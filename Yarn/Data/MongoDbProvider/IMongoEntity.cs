using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Yarn.Data.MongoDbProvider
{
    public class IMongoEntity
    {
        [BsonId]
        ObjectId Id { get; set; }
        ObjectId[] PendingTransactions { get; set; }
    }
}
