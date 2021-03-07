using Relativity.API;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Net;
using System.Runtime.InteropServices;

namespace RelativityAuditLogElasticSearchEventHandlers
{
    [Description("Relativity Audit Log Elastic Search Post Workspace Create EventHandler")]
    [Guid("4724d01d-8fdb-4cfc-be4e-57806300b003")]

    /*
     * Post Workspace Create EventHandler Class
     * Class's only method Execute() overrides default method and it is executed after workspace with Audit Log Elastic Search application is crated (e.g. copying from template)
     */
    public class PostWorkspaceCreateEventHandlerBase : kCura.EventHandler.PostWorkspaceCreateEventHandlerBase
    {
        // Application management table name
        private string TableName = "AuditLogElasticSearch";

        public override kCura.EventHandler.Response Execute()
        {
            // Update Security Protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<PostWorkspaceCreateEventHandlerBase>();

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

            // Add line to the application management table for current workspace
            try
            {
                // Insert to the application management table
                SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                instanceContext.ExecuteNonQuerySQLStatement("INSERT INTO [eddsdbo].[" + this.TableName + "] ([CaseArtifactID], [AuditRecordID], [Status], [LastUpdated]) VALUES (@workspaceId, 0, 1, CURRENT_TIMESTAMP)", new SqlParameter[] { workspaceIdParam });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Post Workspace Create EventHandler application management table insert error");

                response.Success = false;
                response.Message = "Post Workspace Create EventHandler application management table insert error";
                return response;
            }

            // Log end of Post Workspace Create EventHandler
            _logger.LogDebug("Audit Log Elastic Search, Post Workspace Create EventHandler successfully finished");

            return response;
        }
    }
}