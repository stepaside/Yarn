using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Yarn.Data.MongoDbProvider
{
    public class MongoTransactionState
    {
        public const string Initial = "initial";
        public const string Pending = "pending";
        public const string Committed = "committed";
        public const string Done = "done";
        public const string Canceling = "canceling";
        public const string Canceled = "canceled";
    }

    public class MongoTransaction<T> 
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("state")]
        public string State { get; set; }
        [BsonElement("source")]
        public string Source { get; set; }
        [BsonElement("destination")]
        public string Destination { get; set; }
        [BsonElement("value")]
        public T Value { get; set; }
    }
}
