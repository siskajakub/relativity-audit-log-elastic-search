using kCura.Agent;
using Relativity.API;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;

namespace RelativityAuditLogElasticSearchAgent
{
    [kCura.Agent.CustomAttributes.Name("Relativity Audit Log Elastic Search Agent")]
    [System.Runtime.InteropServices.Guid("4724d01d-8fdb-4cfc-be4e-57806300a001")]
    public class Agent : AgentBase
    {
        // Application management table name
        private string tableName = "AuditLogElasticSearch";

        public override void Execute()
        {
            // Update Security Protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<Agent>();

            // Get current Agent ID
            int agentId = this.AgentID;
            _logger.LogDebug("Audit Log Elastic Search, current Agent ID: {agentId}", agentId.ToString());

            // Get database context of the instance
            IDBContext instanceContext = Helper.GetDBContext(-1);

            this.RaiseMessageNoLogging("Selecting Workspace", 10);
            _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), selecting Workspace", agentId.ToString());

            // Check what needs to be done
            int workspaceId = -1;
            int status = -1;
            instanceContext.BeginTransaction();
            try
            {
                // Get workspace that was synchronized latest
                DataTable dataTable = instanceContext.ExecuteSqlStatementAsDataTable("SELECT TOP(1) [CaseArtifactID], [Status] FROM [eddsdbo].[" + this.tableName + "] WHERE [AgentArtifactID] IS NULL ORDER BY [Status] ASC, [LastUpdated] ASC");

                // If there is no workspace check if table is empty and if it is, delete it
                _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), Workspace selection row count: {count}", agentId.ToString(), dataTable.Rows.Count.ToString());
                if (dataTable.Rows.Count == 0)
                {
                    int count = instanceContext.ExecuteSqlStatementAsScalar<int>("SELECT COUNT(*) FROM [eddsdbo].[Relativity]");
                    _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), application management table row count: {count}", agentId.ToString(), count.ToString());
                    if (count == 0)
                    {
                        instanceContext.ExecuteNonQuerySQLStatement("DROP TABLE [eddsdbo].[" + this.tableName + "]");
                        _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), application management table was deleted", agentId.ToString());
                    }
                }
                // Else we have workspace to work with
                else
                {
                    DataRow dataRow = dataTable.Rows[0];
                    workspaceId = Convert.ToInt32(dataRow["CaseArtifactID"]);
                    status = Convert.ToInt32(dataRow["Status"]);

                    // Update the application management table with Agent ID lock
                    SqlParameter agentIdParam = new SqlParameter("@agentId", agentId);
                    SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                    instanceContext.ExecuteNonQuerySQLStatement("UPDATE [eddsdbo].[" + this.tableName + "] SET [AgentArtifactID] = @agentId WHERE [CaseArtifactID] = @workspaceId", new SqlParameter[] { agentIdParam, workspaceIdParam });
                }
                instanceContext.CommitTransaction();
            }
            catch (Exception e)
            {
                instanceContext.RollbackTransaction();
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentId}), application management table action querying error", agentId.ToString());
                return;
            }

            // If we have Workspace ID we have to do something
            if (workspaceId > 0)
            {
                // Get certain instance settings
                string indexPrefix = "";
                try
                {
                    indexPrefix = this.Helper.GetInstanceSettingBundle().GetString("Relativity.AuditLogElasticSearch", "ElasticSearchIndexPrefix");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentId}), Instance Settings error", agentId.ToString());
                    return;
                }

                string indexName = indexPrefix + workspaceId.ToString();
                switch (status)
                {
                    // If the status is 0 we will be deleting ES index
                    case 0:
                        this.RaiseMessageNoLogging(string.Format("Deleting ES index ({0})", indexName), 10);
                        _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), deleting ES index ({indexName})", agentId.ToString(), indexName);
                        // ToDo
                        break;

                    // If the status is 1 we will be synchronizing ES index
                    case 1:
                        this.RaiseMessageNoLogging(string.Format("Synchronizing Audit Log of Workspace ({0}) to ES index ({1})", workspaceId.ToString(), indexName), 10);
                        _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), synchronizing Audit Log of Workspace ({workspaceId}) to ES index ({indexName})", agentId.ToString(), workspaceId.ToString(), indexName);
                        // ToDo
                        break;
                }

                // When done update the application management table and release Agent ID lock
                SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                instanceContext.ExecuteNonQuerySQLStatement("UPDATE [eddsdbo].[" + this.tableName + "] SET [AgentArtifactID] = NULL, [LastUpdated] = CURRENT_TIMESTAMP WHERE [CaseArtifactID] = @workspaceId", new SqlParameter[] { workspaceIdParam });

                // Log end of Agent execution
                this.RaiseMessageNoLogging("Completed.", 10);
                _logger.LogDebug("Audit Log Elastic Search, Agent ({agentId}), completed", agentId.ToString());
            }
        }

        public override string Name
        {
            get
            {
                return "Audit Log Elastic Search Agent";
            }
        }
    }
}