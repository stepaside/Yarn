using System;
using Nemo;
using Nemo.Attributes;

namespace Yarn.EventSourcing.NemoProvider
{
    [Table("Events")]
    public class EventData
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public string AggregateType { get; set; }
        public Guid AggregateId { get; set; }
        public int Version { get; set; }
        public string Event { get; set; }
        public string Metadata { get; set; }
    }
}
