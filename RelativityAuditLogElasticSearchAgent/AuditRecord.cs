using Nest;
using System;

namespace RelativityAuditLogElasticSearchAgent
{
    /*
     * Class representing audit record
     */
    [ElasticsearchType(RelationName = "audit_record")]
    public class AuditRecord
    {
        [Number(NumberType.Long ,Name = "audit_record_id" ,Index = true, Coerce = true, DocValues = true)]
        public long AuditRecordId { get; set; }
        
        [Date(Name = "timestamp", Index = true, DocValues = true)]
        public DateTime TimeStamp { get; set; }

        [Number(NumberType.Integer, Name = "artifact_id", Index = true, Coerce = true, DocValues = true)]
        public int ArtifactId { get; set; }

        [Number(NumberType.Integer, Name = "action_id", Index = true, Coerce = true, DocValues = true)]
        public int ActionId { get; set; }

        [Keyword(Name = "action", Index = true, DocValues = true)]
        public string Action { get; set; }

        [Number(NumberType.Integer, Name = "user_id", Index = true, Coerce = true, DocValues = true)]
        public int UserId { get; set; }
        
        [Keyword(Name = "user", Index = true, DocValues = true)]
        public string User { get; set; }

        [Number(NumberType.Integer, Name = "execution_time", Index = true, Coerce = true, DocValues = true)]
        public int ExecutionTime { get; set; }

        [Text(Name = "details", Index = true)]
        public string Details { get; set; }

        [Text(Name = "request_origination", Index = true)]
        public string RequestOrigination { get; set; }

        [Text(Name = "record_origination", Index = true)]
        public string RecordOrigination { get; set; }
    }
}
