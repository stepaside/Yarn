using System;
using System.Collections.Generic;

namespace Yarn.Data.MongoDbProvider
{
    public class RepositoryOptions
    {
        public string ConnectionString { get; set; }
        public IDictionary<Type, string> Collections { get; } = new Dictionary<Type, string>();
    }
}
