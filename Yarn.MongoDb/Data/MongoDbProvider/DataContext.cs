using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Yarn;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Yarn.Data.MongoDbProvider
{
    internal class DataContext : IDataContext<IMongoDatabase>
    {
        private IMongoDatabase _database;
        private MongoUrl _url;
        private readonly string _connectionString;

        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected IMongoDatabase GetMongoDatabase(string connectionString)
        {
            _url = new MongoUrl(connectionString);
            var dbName = _url.DatabaseName;
            var client = new MongoClient(_url);
            return client.GetDatabase(dbName);
        }
        
        protected IMongoDatabase GetDefaultMongoDatabase()
        {
            return GetMongoDatabase(_connectionString);
        }

        public void SaveChanges()
        {
            // No transaction support
        }

        public void CreateIndex<T>(Expression<Func<T, object>> field, bool ascending = true, bool background = true, TimeSpan? ttl = null, bool? unique = null, bool? sparse = null)
        {
            var index = ascending ? Builders<T>.IndexKeys.Ascending(field) : Builders<T>.IndexKeys.Descending(field);
            _database.GetCollection<T>(typeof(T).Name).Indexes.CreateOne(new CreateIndexModel<T>(index, new CreateIndexOptions { Background = background, ExpireAfter = ttl, Unique = unique, Sparse = sparse }));
        }

        public IMongoDatabase Session
        {
            get
            {
                if (_database == null)
                {
                    _database = GetMongoDatabase(_connectionString);
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
