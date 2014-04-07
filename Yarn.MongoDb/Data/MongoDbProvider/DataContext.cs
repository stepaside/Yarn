using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Yarn;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Builders;

namespace Yarn.Data.MongoDbProvider
{
    public class DataContext : IDataContext<MongoDatabase>
    {
        private readonly string _prefix;
        private MongoDatabase _database;
        private MongoUrl _url;

        public DataContext() : this(null) { }

        public DataContext(string prefix = null)
        {
            _prefix = prefix;
        }

        protected MongoDatabase GetMongoDatabase(string prefix)
        {
            _url = new MongoUrl(ConfigurationManager.AppSettings.Get(prefix));
            var dbName = _url.DatabaseName;
            if (string.IsNullOrEmpty(dbName))
            {
                dbName = ConfigurationManager.AppSettings.Get(prefix + ".Database");
            }
            var client = new MongoClient(_url);
            var server = client.GetServer();
            return server.GetDatabase(dbName);
        }

        protected virtual string DefaultPrefix
        {
            get
            {
                return "MongoDB.Default";
            }
        }

        protected MongoDatabase GetDefaultMongoDatabase()
        {
            return GetMongoDatabase(DefaultPrefix);
        }

        public void SaveChanges()
        {
            // No transaction support
        }

        public void CreateIndex<T>(Tuple<string, bool>[] names, bool background = true, TimeSpan ttl = new TimeSpan(), bool unique = false, bool dropDups = false, bool sparse = false)
        {
            var ascending = new List<string>();
            var descending = new List<string>();
            foreach (var n in names)
            {
                if (n.Item2)
                {
                    ascending.Add(n.Item1);
                }
                else
                {
                    descending.Add(n.Item1);
                }
            }

            var builder = new IndexKeysBuilder();
            if (ascending.Count > 0)
            {
                builder = builder.Ascending(ascending.ToArray());
            }
            if (descending.Count > 0)
            {
                builder = builder.Descending(descending.ToArray());
            }

            _database.GetCollection<T>(typeof(T).Name).EnsureIndex(builder, 
                                                                    IndexOptions.SetBackground(background)
                                                                                .SetTimeToLive(ttl)
                                                                                .SetUnique(unique)
                                                                                .SetDropDups(dropDups)
                                                                                .SetSparse(sparse));
        }

        public MongoDatabase Session
        {
            get
            {
                if (_database == null)
                {
                    _database = _prefix == null ? GetDefaultMongoDatabase() : GetMongoDatabase(_prefix);
                    _database.Server.Connect();
                }
                return _database;
            }
        }

        public string Source
        {
            get
            {
                return _url.Url;
            }
        }

        public void Dispose()
        {
           
        }
    }
}
