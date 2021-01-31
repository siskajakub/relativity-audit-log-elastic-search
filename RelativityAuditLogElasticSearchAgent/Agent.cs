using kCura.Agent;

namespace RelativityAuditLogElasticSearchAgent
{
    [kCura.Agent.CustomAttributes.Name("Relativity Audit Log Elastic Search Agent")]
    [System.Runtime.InteropServices.Guid("4724d01d-8fdb-4cfc-be4e-57806300a001")]
    public class Agent : AgentBase
    {
        public override void Execute()
        {
            
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