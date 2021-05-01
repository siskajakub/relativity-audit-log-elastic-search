using Relativity.API;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Net;
using System.Runtime.InteropServices;

namespace RelativityAuditLogElasticSearchEventHandlers
{
    [Description("Audit Log Elastic Search Post Install EventHandler")]
    [Guid("4724d01d-8fdb-4cfc-be4e-57806300b002")]

    /*
     * Post Install EventHandler Class
     * Class's only method Execute() overrides default method and it is executed after Audit Log Elastic Search application is installed to the workspace
     */
    public class PostInstallEventHandler : kCura.EventHandler.PostInstallEventHandler
    {
        // Application management table name
        private string TableName = "AuditLogElasticSearch";

        public override kCura.EventHandler.Response Execute()
        {
            // Update Security Protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<PostInstallEventHandler>();

            // Init general response
            kCura.EventHandler.Response response = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = ""
            };

            // Get current Workspace ID
            int workspaceId = this.Helper.GetActiveCaseID();
            _logger.LogDebug("Audit Log Elastic Search, current Workspace ID: {workspaceId}", workspaceId.ToString());

            // Get database context of the instance
            IDBContext instanceContext = Helper.GetDBContext(-1);

            // Existing application management table name
            string tableExisting = "";

            try
            {
                // Get application management table
                tableExisting = instanceContext.ExecuteSqlStatementAsScalar("SELECT ISNULL((SELECT '" + this.TableName + "' FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_SCHEMA] = 'eddsdbo' AND [TABLE_NAME] = '" + this.TableName + "'), '')").ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Post Install EventHandler application management table error");

                response.Success = false;
                response.Message = "Post Install EventHandler management application table error";
                return response;
            }

            // If application management table not present, create it
            if (tableExisting != this.TableName)
            {
                instanceContext.BeginTransaction();
                try
                {
                    // Create application management table
                    instanceContext.ExecuteNonQuerySQLStatement("CREATE TABLE [eddsdbo].[" + this.TableName + "] ([CaseArtifactID] [int] NOT NULL, [AuditRecordID] [bigint] NOT NULL, [Status] [bit] NOT NULL, [LastUpdated] [datetime] NOT NULL, [AgentArtifactID] [int] NULL)");
                    instanceContext.CommitTransaction();
                }
                catch (Exception e)
                {
                    instanceContext.RollbackTransaction();

                    _logger.LogError(e, "Audit Log Elastic Search, Post Install EventHandler application management table creation error");

                    response.Success = false;
                    response.Message = "Post Install EventHandler application management table creation error";
                    return response;
                }
            }

            // Add line to the application management table for current workspace
            try
            {
                // Insert to the application management table
                SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                instanceContext.ExecuteNonQuerySQLStatement("INSERT INTO [eddsdbo].[" + this.TableName + "] ([CaseArtifactID], [AuditRecordID], [Status], [LastUpdated]) VALUES (@workspaceId, 0, 1, CURRENT_TIMESTAMP)", new SqlParameter[] { workspaceIdParam });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Post Install EventHandler application management table insert error");

                response.Success = false;
                response.Message = "Post Install EventHandler application management table insert error";
                return response;
            }

            // Log end of Post Install EventHandler
            _logger.LogDebug("Audit Log Elastic Search, Post Install EventHandler successfully finished");

            return response;
        }
    }
}