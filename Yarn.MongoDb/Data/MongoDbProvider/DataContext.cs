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
    public class DataContext : IDataContext<IMongoDatabase>
    {
        private readonly string _prefix;
        private IMongoDatabase _database;
        private MongoUrl _url;
        private readonly string _connectionString;

        public DataContext() : this(null) { }

        public DataContext(string prefix = null, string connectionString = null)
        {
            _prefix = prefix;
            _connectionString = connectionString;
        }

        protected IMongoDatabase GetMongoDatabase(string prefix , string connectionString)
        {
            _url = new MongoUrl(connectionString ?? ConfigurationManager.AppSettings.Get(prefix));
            var dbName = _url.DatabaseName;
            if (string.IsNullOrEmpty(dbName))
            {
                dbName = ConfigurationManager.AppSettings.Get(prefix + ".Database");
            }
            var client = new MongoClient(_url);
            return client.GetDatabase(dbName);
        }

        protected virtual string DefaultPrefix
        {
            get
            {
                return "MongoDB.Default";
            }
        }

        protected IMongoDatabase GetDefaultMongoDatabase()
        {
            return GetMongoDatabase(DefaultPrefix , _connectionString);
        }

        public void SaveChanges()
        {
            // No transaction support
        }

        public void CreateIndex<T>(Expression<Func<T, object>> field, bool ascending = true, bool background = true, TimeSpan? ttl = null, bool? unique = null, bool? sparse = null)
        {
            var index = ascending ? Builders<T>.IndexKeys.Ascending(field) : Builders<T>.IndexKeys.Descending(field);
            _database.GetCollection<T>(typeof(T).Name).Indexes.CreateOne(index, new CreateIndexOptions { Background = background, ExpireAfter = ttl, Unique = unique, Sparse = sparse });
        }

        public IMongoDatabase Session
        {
            get
            {
                if (_database == null)
                {
                    _database = _prefix == null ? GetDefaultMongoDatabase() : GetMongoDatabase(_prefix , _connectionString);
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
