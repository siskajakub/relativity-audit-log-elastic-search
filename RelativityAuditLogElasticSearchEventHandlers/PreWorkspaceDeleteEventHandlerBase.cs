using Relativity.API;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Net;
using System.Runtime.InteropServices;

namespace RelativityAuditLogElasticSearchEventHandlers
{
    [Description("Relativity Audit Log Elastic Search Pre Workspace Delete EventHandler")]
    [Guid("4724d01d-8fdb-4cfc-be4e-57806300b005")]

    /*
     * Pre Workspace Delete EventHandler Class
     * Class's only method Execute() overrides default method and it is executed before workspace with Audit Log Elastic Search application is deleted
     */
    public class PreWorkspaceDeleteEventHandlerBase : kCura.EventHandler.PreWorkspaceDeleteEventHandlerBase
    {
        // Application management table name
        private string tableName = "AuditLogElasticSearch";

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

            // Update line of the application management and set current workspace to disabled
            try
            {
                // Update the application management table
                SqlParameter workspaceIdParam = new SqlParameter("@workspaceId", workspaceId);
                instanceContext.ExecuteNonQuerySQLStatement("UPDATE [eddsdbo].[" + this.tableName + "] SET [Status] = 0, [LastUpdated] = CURRENT_TIMESTAMP WHERE [CaseArtifactID] = @workspaceId", new SqlParameter[] { workspaceIdParam });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Audit Log Elastic Search, Pre Workspace Delete EventHandler application management table update error");

                response.Success = false;
                response.Message = "Pre Workspace Delete EventHandler application management table update error";
                return response;
            }

            // Log end of Pre Uninstall EventHandler
            _logger.LogDebug("Audit Log Elastic Search, Pre Workspace Delete EventHandler successfully finished");

            return response;
        }
    }
}