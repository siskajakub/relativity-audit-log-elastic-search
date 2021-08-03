using kCura.Agent;
using kCura.Agent.CustomAttributes;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace RelativityAuditLogElasticSearchAgent
{
    [Name("Audit Log Elastic Search Agent")]
    [Guid("4724d01d-8fdb-4cfc-be4e-57806300a001")]
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
            int agentArtifactId = this.AgentID;
            _logger.LogDebug("Audit Log Elastic Search, current Agent ID: {agentArtifactId}", agentArtifactId.ToString());

            // Display initial message
            this.RaiseMessageNoLogging("Getting Instance Settings.", 10);

            // Get ES URI Instance Settings
            List<Uri> elasticUris = new List<Uri>();
            try
            {
                string[] uris = this.Helper.GetInstanceSettingBundle().GetString("Relativity.AuditLogElasticSearch", "ElasticSearchUris").Split(';');
                foreach (string uri in uris)
                {
                    if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    {
                        elasticUris.Add(new Uri(uri));
                    }
                    else
                    {
                        _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchUri), single URI error ({uri})", agentArtifactId.ToString(), uri);
                        this.RaiseMessageNoLogging(string.Format("Instance Settings error (ElasticSearchUri), single URI error ({0}).", uri), 1);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchUri)", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Instance Settings error (ElasticSearchUri).", 1);
                return;
            }

            // Get ES authentication API Key Instance Settings
            string[] elasticApiKey = new string[] { "", "" };
            try
            {
                string apiKey = this.Helper.GetInstanceSettingBundle().GetString("Relativity.AuditLogElasticSearch", "ElasticSearchApiKey");
                if (apiKey.Length > 0)
                {
                    if (apiKey.Split(':').Length == 2)
                    {
                        elasticApiKey = apiKey.Split(':');
                    }
                    else
                    {
                        _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchApiKey), API Key format error ({apiKey})", agentArtifactId.ToString(), apiKey);
                        this.RaiseMessageNoLogging(string.Format("Instance Settings error (ElasticSearchApiKey), API Key format error ({0}).", apiKey), 1);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchApiKey)", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Instance Settings error (ElasticSearchApiKey).", 1);
                return;
            }

            // Get ES index prefix Instance Settings (must by lowercase)
            string elasticIndexPrefix = "";
            try
            {
                elasticIndexPrefix = this.Helper.GetInstanceSettingBundle().GetString("Relativity.AuditLogElasticSearch", "ElasticSearchIndexPrefix").ToLower();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchIndexPrefix)", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Instance Settings error (ElasticSearchIndexPrefix).", 1);
                return;
            }

            // Get ES index number of replicas Instance Settings
            int elasticIndexReplicas = 1;
            try
            {
                elasticIndexReplicas = this.Helper.GetInstanceSettingBundle().GetInt("Relativity.AuditLogElasticSearch", "ElasticSearchIndexReplicas").Value;
                if (elasticIndexReplicas < 0)
                {
                    elasticIndexReplicas = 1;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchIndexReplicas)", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Instance Settings error (ElasticSearchIndexReplicas).", 1);
                return;
            }

            // Get ES index number of shards Instance Settings
            int elasticIndexShards = 1;
            try
            {
                elasticIndexShards = this.Helper.GetInstanceSettingBundle().GetInt("Relativity.AuditLogElasticSearch", "ElasticSearchIndexShards").Value;
                if (elasticIndexShards < 0)
                {
                    elasticIndexShards = 1;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchIndexShards)", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Instance Settings error (ElasticSearchIndexShards).", 1);
                return;
            }

            // Get ES synchronization threshold for one agent run
            int elasticSyncSize = 1000;
            try
            {
                elasticSyncSize = this.Helper.GetInstanceSettingBundle().GetInt("Relativity.AuditLogElasticSearch", "ElasticSearchSyncSize").Value;
                if (elasticSyncSize < 1000)
                {
                    elasticSyncSize = 1000;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), Instance Settings error (ElasticSearchSyncSize)", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Instance Settings error (ElasticSearchSyncSize).", 1);
                return;
            }

            // Get database context of the instance
            IDBContext instanceContext = Helper.GetDBContext(-1);

            // Check if management table exists
            try
            {
                int exists = instanceContext.ExecuteSqlStatementAsScalar<int>("IF OBJECT_ID('[eddsdbo].[" + this.tableName + "]', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0");
                _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), application management table " + (exists == 1 ? "exists" : "does not exist"), agentArtifactId.ToString());
                if (exists != 1)
                {
                    _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), application management table does not exist", agentArtifactId.ToString());
                    this.RaiseMessageNoLogging("Application management table does not exist.", 1);
                    return;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), application management table existence check error", agentArtifactId.ToString());
            }

            _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), selecting Workspace", agentArtifactId.ToString());
            this.RaiseMessageNoLogging("Selecting Workspace.", 10);

            // Check what needs to be done
            int workspaceId = -1;
            long auditRecordId = -1;
            int status = -1;
            instanceContext.BeginTransaction();
            try
            {
                // Get workspace that was synchronized latest
                DataTable dataTable = instanceContext.ExecuteSqlStatementAsDataTable(@"
                    SELECT TOP(1)
                        [CaseArtifactID],
                        [AuditRecordID],
                        [Status]
                    FROM [eddsdbo].[" + this.tableName + @"]
                    WHERE [AgentArtifactID] IS NULL
                    ORDER BY
                        [Status] ASC,
                        [LastUpdated] ASC
                ");

                // If there is no workspace check if table is empty and if it is, delete it
                _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), Workspace selection row count: {count}", agentArtifactId.ToString(), dataTable.Rows.Count.ToString());
                if (dataTable.Rows.Count == 0)
                {
                    int count = instanceContext.ExecuteSqlStatementAsScalar<int>("SELECT COUNT(*) FROM [eddsdbo].[" + this.tableName + "]");
                    _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), application management table row count: {count}", agentArtifactId.ToString(), count.ToString());
                    // If there are no rows in the application management table better to drop it
                    if (count == 0)
                    {
                        instanceContext.ExecuteNonQuerySQLStatement("DROP TABLE [eddsdbo].[" + this.tableName + "]");
                        _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), application management table was deleted", agentArtifactId.ToString());
                    }
                }
                // Else we have workspace to work with
                else
                {
                    DataRow dataRow = dataTable.Rows[0];
                    workspaceId = Convert.ToInt32(dataRow["CaseArtifactID"]);
                    auditRecordId = Convert.ToInt64(dataRow["AuditRecordID"]);
                    status = Convert.ToInt32(dataRow["Status"]);

                    // Update the application management table with Agent ID lock
                    SqlParameter agentArtifactIdParam = new SqlParameter("@agentArtifactId", agentArtifactId);
                    SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                    instanceContext.ExecuteNonQuerySQLStatement("UPDATE [eddsdbo].[" + this.tableName + "] SET [AgentArtifactID] = @agentArtifactId WHERE [CaseArtifactID] = @workspaceId", new SqlParameter[] { agentArtifactIdParam, workspaceIdParam });
                }
                instanceContext.CommitTransaction();
            }
            catch (Exception e)
            {
                instanceContext.RollbackTransaction();
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), application management table querying error", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Application management table querying error.", 1);
                return;
            }

            // If we have Workspace ID we have to do something
            if (workspaceId > 0)
            {
                // Construct ES index name
                string elasticIndexName = elasticIndexPrefix + workspaceId.ToString();

                // Construct connector to ES cluster
                Nest.ElasticClient elasticClient = null;
                try
                {
                    Elasticsearch.Net.StaticConnectionPool pool = new Elasticsearch.Net.StaticConnectionPool(elasticUris, true);
                    elasticClient = new Nest.ElasticClient(new Nest.ConnectionSettings(pool).DefaultIndex(elasticIndexName).ApiKeyAuthentication(elasticApiKey[0], elasticApiKey[1]).EnableHttpCompression());
                }
                catch (Exception e)
                {
                    this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                    _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}) Elastic Search connection call error ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                    this.RaiseMessageNoLogging(string.Format("Elastic Search connection call error ({0}, {1}).", string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName), 1);
                    return;
                }

                // Check ES cluster connection
                Nest.PingResponse pingResponse = elasticClient.Ping();
                if (pingResponse.IsValid)
                {
                    _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), Ping succeeded ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                }
                else
                {
                    this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                    _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), Ping failed, check cluster health and connection settings ({elasticUris}, {indexName}, {elasticError})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName, pingResponse.DebugInformation);
                    this.RaiseMessageNoLogging(string.Format("Elastic Search ping failed, check cluster health and connection settings ({0}, {1}, {2}).", string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName, pingResponse.DebugInformation), 1);
                    return;
                }

                switch (status)
                {
                    // If the status is 0 we will be deleting ES index
                    case 0:
                        _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), deleting ES index ({indexName})", agentArtifactId.ToString(), elasticIndexName);
                        this.RaiseMessageNoLogging(string.Format("Deleting ES index ({0}).", elasticIndexName), 10);

                        // Delete ES index
                        try
                        {
                            Nest.DeleteIndexResponse deleteIndexResponse = elasticClient.Indices.Delete(elasticIndexName);
                            if (deleteIndexResponse.Acknowledged)
                            {
                                _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), Elastic Search index deleted ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                            }
                            else
                            {
                                this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), Elastic Search index deletion error ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                                this.RaiseMessageNoLogging(string.Format("Elastic Search index deletion error ({0}, {1}).", string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName), 1);
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                            _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}) Elastic Search deletion call error ({indexName})", agentArtifactId.ToString(), elasticIndexName);
                            this.RaiseMessageNoLogging(string.Format("Elastic Search deletion call error ({0}).", elasticIndexName), 1);
                            return;
                        }

                        // Delete related row from the application management table
                        try
                        {
                            SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                            instanceContext.ExecuteNonQuerySQLStatement("DELETE FROM [eddsdbo].[" + this.tableName + "] WHERE [Status] = 0 AND [CaseArtifactID] = @workspaceId", new SqlParameter[] { workspaceIdParam });
                        }
                        catch (Exception e)
                        {
                            this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                            _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), application management table delete error", agentArtifactId.ToString());
                            this.RaiseMessageNoLogging("Application management table delete error.", 1);
                            return;
                        }
                        break;

                    // If the status is 1 we will be synchronizing Audit Log with ES index
                    case 1:
                        _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), synchronizing Audit Log of Workspace ({workspaceId}) to ES index ({indexName})", agentArtifactId.ToString(), workspaceId.ToString(), elasticIndexName);
                        this.RaiseMessageNoLogging(string.Format("Synchronizing Audit Log of Workspace ({0}) to ES index ({1})", workspaceId.ToString(), elasticIndexName), 10);

                        // If there is no records synchronized yet, we have to create ES index first
                        if (auditRecordId == 0)
                        {
                            // Create ES index
                            try {
                                Nest.CreateIndexResponse createIndexResponse = elasticClient.Indices.Create(elasticIndexName, c => c.Settings(s => s.NumberOfShards(elasticIndexShards).NumberOfReplicas(elasticIndexReplicas)).Map<AuditRecord>(m => m.AutoMap()));
                                if (createIndexResponse.Acknowledged)
                                {
                                    _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), Elastic Search index created ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                                }
                                else
                                {
                                    this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                    _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), Elastic Search index creation error ({elasticUris}, {indexName}, {serverError})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName, createIndexResponse.ServerError.ToString());
                                    this.RaiseMessageNoLogging(string.Format("Elastic Search index creation error ({0}, {1}).", string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName), 1);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}) Elastic Search index creation call error ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                                this.RaiseMessageNoLogging(string.Format("Elastic Search index creation call error ({0}, {1}).", string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName), 1);
                                return;
                            }
                        }

                        // Get database context of the given workspace
                        IDBContext workspaceContext = Helper.GetDBContext(workspaceId);

                        // Synchronize until threshold is reached
                        int syncCount = 0;
                        while (syncCount < elasticSyncSize)
                        {
                            try
                            {
                                // Get Audit Log to synchronize
                                SqlParameter auditRecordIdParam = new SqlParameter("@auditRecordId", auditRecordId);
                                DataTable dataTable = workspaceContext.ExecuteSqlStatementAsDataTable(@"
                                    SELECT TOP (1000)
	                                    [AuditRecord].[ID],
	                                    [AuditRecord].[TimeStamp],
	                                    [AuditRecord].[ArtifactID],
	                                    [AuditRecord].[Action] AS [ActionID],
	                                    [AuditAction].[Action],
	                                    [AuditRecord].[UserID],
	                                    [AuditUser].[FullName] AS [User],
	                                    [AuditRecord].[ExecutionTime],
	                                    [AuditRecord].[Details],
	                                    [AuditRecord].[RequestOrigination],
	                                    [AuditRecord].[RecordOrigination]
                                    FROM [EDDSDBO].[AuditRecord] WITH (NOLOCK)
                                    JOIN [EDDSDBO].[AuditUser] WITH (NOLOCK) ON [AuditRecord].[UserID] = [AuditUser].[UserID]
                                    JOIN [EDDSDBO].[AuditAction] WITH (NOLOCK) ON [AuditRecord].[Action] = [AuditAction].[AuditActionID]
                                    WHERE [AuditRecord].[ID] > @auditRecordId
                                    ORDER BY [AuditRecord].[ID] ASC
                                ", new SqlParameter[] { auditRecordIdParam });

                                // If there is nothing to synchronize end
                                _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), Audit Log row count to synchronize: {count}", agentArtifactId.ToString(), dataTable.Rows.Count.ToString());
                                if (dataTable.Rows.Count == 0)
                                {
                                    // Log end of Agent execution
                                    this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                    _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), completed, nothing to synchronize", agentArtifactId.ToString());
                                    this.RaiseMessageNoLogging("Completed.", 10);
                                    return;
                                }
                                // Else synchronize workspace Audit Log with ES index
                                else
                                {
                                    // Synchronizing workspace Audit Log with ES index
                                    List<AuditRecord> auditRecords = new List<AuditRecord>();
                                    long newAuditRecordId = auditRecordId;
                                    for (int i = 0; i < dataTable.Rows.Count; i++)
                                    {
                                        // Read Audit Log data
                                        AuditRecord auditRecord = new AuditRecord();
                                        DataRow dataRow = dataTable.Rows[i];
                                        auditRecord.AuditRecordId = Convert.ToInt64(dataRow["ID"]);
                                        auditRecord.TimeStamp = Convert.ToDateTime(dataRow["TimeStamp"]);
                                        auditRecord.ArtifactId = Convert.ToInt32(dataRow["ArtifactID"]);
                                        auditRecord.ActionId = Convert.ToInt32(dataRow["ActionID"]);
                                        auditRecord.Action = Convert.ToString(dataRow["Action"]);
                                        auditRecord.UserId = Convert.ToInt32(dataRow["UserID"]);
                                        auditRecord.User = Convert.ToString(dataRow["User"]);
                                        auditRecord.ExecutionTime = dataRow["ExecutionTime"] is DBNull ? default : Convert.ToInt32(dataRow["ExecutionTime"]);
                                        auditRecord.Details = dataRow["Details"] is DBNull ? default : Convert.ToString(dataRow["Details"]);
                                        auditRecord.RequestOrigination = dataRow["RequestOrigination"] is DBNull ? default : Convert.ToString(dataRow["RequestOrigination"]);
                                        auditRecord.RecordOrigination = dataRow["RecordOrigination"] is DBNull ? default : Convert.ToString(dataRow["RecordOrigination"]);
                                        auditRecords.Add(auditRecord);

                                        // Record last Audit Log ID
                                        if (newAuditRecordId < auditRecord.AuditRecordId)
                                        {
                                            newAuditRecordId = auditRecord.AuditRecordId;
                                        }

                                        // Index data in threshold is reached or we are at the last row
                                        if (auditRecords.Count >= 500 || i + 1 >= dataTable.Rows.Count)
                                        {
                                            try
                                            {
                                                Nest.BulkResponse bulkResponse = elasticClient.Bulk(b => b.Index(elasticIndexName).IndexMany(auditRecords, (descriptor, s) => descriptor.Id(s.AuditRecordId.ToString())));
                                                if (!bulkResponse.Errors)
                                                {
                                                    auditRecords.Clear();
                                                    _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), documents synchronized to Elastic Serach index ({indexName})", agentArtifactId.ToString(), elasticIndexName);
                                                }
                                                else
                                                {
                                                    this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                                    foreach (Nest.BulkResponseItemBase itemWithError in bulkResponse.ItemsWithErrors)
                                                    {
                                                        _logger.LogError("Audit Log Elastic Search, Agent ({agentArtifactId}), Elastic Serach bulk index error to index {indexName} ({elasticUris}) on document {docIs}:{docError}", agentArtifactId.ToString(), elasticIndexName, string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), itemWithError.Id, itemWithError.Error.ToString());
                                                    }
                                                    this.RaiseMessageNoLogging(string.Format("Elastic Serach bulk index error to index {0} ({1}).", elasticIndexName, string.Join(";", elasticUris.Select(x => x.ToString()).ToArray())), 1);
                                                    return;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}) Elastic Search bulk index call error ({elasticUris}, {indexName})", agentArtifactId.ToString(), string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName);
                                                this.RaiseMessageNoLogging(string.Format("Elastic Search bulk index call error ({0}, {1}).", string.Join(";", elasticUris.Select(x => x.ToString()).ToArray()), elasticIndexName), 1);
                                                return;
                                            }
                                        }
                                    }

                                    // After successful indexing assign new Audit Log ID
                                    auditRecordId = newAuditRecordId;
                                }
                            }
                            catch (Exception e)
                            {
                                this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), audit log table querying error", agentArtifactId.ToString());
                                this.RaiseMessageNoLogging("Audit log table querying error.", 1);
                                return;
                            }

                            syncCount += 1000;
                        }

                        // When done synchronizing update the application management table and release Agent ID lock
                        this.releaseAgentLock(agentArtifactId, auditRecordId, workspaceId);
                        break;
                }
            }

            // Log end of Agent execution
            _logger.LogDebug("Audit Log Elastic Search, Agent ({agentArtifactId}), completed", agentArtifactId.ToString());
            this.RaiseMessageNoLogging("Completed.", 10);
        }

        public override string Name
        {
            get
            {
                return "Audit Log Elastic Search Agent";
            }
        }

        private bool releaseAgentLock(int agentArtifactId, long auditRecordId, int workspaceId)
        {
            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<Agent>();

            // Get database context of the instance
            IDBContext instanceContext = Helper.GetDBContext(-1);

            try
            {
                SqlParameter auditRecordIdParam = new SqlParameter("@auditRecordId", auditRecordId);
                SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                instanceContext.ExecuteNonQuerySQLStatement("UPDATE [eddsdbo].[" + this.tableName + "] SET [AuditRecordID] = @auditRecordId, [LastUpdated] = CURRENT_TIMESTAMP, [AgentArtifactID] = NULL WHERE [CaseArtifactID] = @workspaceId", new SqlParameter[] { auditRecordIdParam, workspaceIdParam });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Agent ({agentArtifactId}), application management table update error", agentArtifactId.ToString());
                this.RaiseMessageNoLogging("Application management table update error.", 1);
                return false;
            }

            return true;
        }
    }
}