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
        private string _contextKey = null;
        private MongoDatabase _database = null;
        private MongoUrl _url = null;

        public DataContext() : this(null) { }

        public DataContext(string contextKey = null)
        {
            _contextKey = contextKey;
        }

        protected MongoDatabase GetMongoDatabase(string storeKey)
        {
            _url = new MongoUrl(ConfigurationManager.AppSettings.Get(storeKey));
            var dbName = _url.DatabaseName;
            if (string.IsNullOrEmpty(dbName))
            {
                dbName = ConfigurationManager.AppSettings.Get(storeKey + ".Database");
            }
            var client = new MongoClient(_url);
            var server = client.GetServer();
            return server.GetDatabase(dbName);
        }

        protected virtual string DefaultStoreKey
        {
            get
            {
                return "MongoDB.Default";
            }
        }

        protected MongoDatabase GetDefaultMongoDatabase()
        {
            return GetMongoDatabase(DefaultStoreKey);
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
                    _database = _contextKey == null ? GetDefaultMongoDatabase() : GetMongoDatabase(_contextKey);
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

        public IDataContextCache DataContextCache
        {
            get
            {
                return null;
            }
        }
    }
}
