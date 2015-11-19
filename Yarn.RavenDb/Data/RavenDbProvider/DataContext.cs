using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Shard;

namespace Yarn.Data.RavenDbProvider
{
    public class DataContext : IDataContext<IDocumentSession>
    {
        private static readonly ConcurrentDictionary<string, IDocumentStore> DocumentStores = new ConcurrentDictionary<string, IDocumentStore>();

        private readonly string _key;
        private IDocumentSession _session;
        private readonly IShardAccessStrategy _accessStrategy;
        private readonly IShardResolutionStrategy _resolutionStrategy;
        private readonly IReadOnlyDictionary<string, string> _shards;
        private readonly string _connectionString;

        public DataContext(string connectionString)
        {
            _key = connectionString;
            _session = (IDocumentSession)DataContextCache.Current.Get(_key);
        }

        public DataContext(string prefix = null, IShardAccessStrategy accessStrategy = null, IShardResolutionStrategy resolutionStrategy = null)
        {
            if (prefix == null)
            {
                prefix = DefaultPrefix;
            }

            var shardCountValue = ConfigurationManager.AppSettings.Get(prefix + ".ShardCount");
            int shardCount;

            if (int.TryParse(shardCountValue, out shardCount))
            {
                var shards = new Dictionary<string, string>();
                for (var i = 0; i < shardCount; i++)
                {
                    var url = ConfigurationManager.AppSettings.Get(prefix + ".Shard." + i + ".Url");
                    var id = ConfigurationManager.AppSettings.Get(prefix + ".Shard." + i + ".Identifier");
                    shards.Add(id, url);
                }

                _shards = new ReadOnlyDictionary<string, string>(shards);

                _key = string.Join("|", shards.Keys.OrderBy(s => s));

                _accessStrategy = accessStrategy ?? new ParallelShardAccessStrategy();
                _resolutionStrategy = resolutionStrategy;
            }
            else
            {
                var url = ConfigurationManager.AppSettings.Get(prefix);

                if (!string.IsNullOrEmpty(url))
                {
                    _connectionString = url;
                    _key = url;
                }
            }

            if (_key != null)
            {
                _session = (IDocumentSession)DataContextCache.Current.Get(_key);
            }
        }

        public DataContext(IDictionary<string, string> shards, IShardAccessStrategy accessStrategy = null, IShardResolutionStrategy resolutionStrategy = null)
        {
            _key = string.Join("|", shards.Keys.OrderBy(s => s));
            _accessStrategy = accessStrategy ?? new ParallelShardAccessStrategy();
            _resolutionStrategy = resolutionStrategy;
            _session = (IDocumentSession)DataContextCache.Current.Get(_key);
        }

        protected IDocumentStore CreateDocumentStore()
        {
            var documentStore = DocumentStores.GetOrAdd(_key, key =>
            {
                IDocumentStore ds = null;
                
                if (_shards.Count > 0)
                {
                    var shards = _shards.ToDictionary(s => s.Key, s => (IDocumentStore)new DocumentStore { Identifier = s.Key, Url = s.Value });

                    var shardStrategy = new ShardStrategy(shards);

                    if (_accessStrategy != null)
                    {
                        shardStrategy.ShardAccessStrategy = _accessStrategy;
                    }

                    if (_resolutionStrategy != null)
                    {
                        shardStrategy.ShardResolutionStrategy = _resolutionStrategy;
                    }

                    ds = new ShardedDocumentStore(shardStrategy);
                    ds.Initialize();
                }
                else if (_connectionString != null)
                {
                    ds = new DocumentStore { Url = _connectionString };
                    ds.Initialize();
                }

                return ds;
            });
            return documentStore;
        }

        protected virtual string DefaultPrefix
        {
            get
            {
                return "RavenDB.Default";
            }
        }
        
        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map, Expression<Func<IEnumerable<T>, IEnumerable>> reduce, IDictionary<Expression<Func<T, object>>, SortOptions> sortOptions)
        {
            CreateDocumentStore().DatabaseCommands.PutIndex(indexName, new IndexDefinitionBuilder<T>
            {
                Map = map,
                Reduce = reduce,
                SortOptions = sortOptions
            });
        }

        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map, IDictionary<Expression<Func<T, object>>, SortOptions> sortOptions)
        {
            CreateDocumentStore().DatabaseCommands.PutIndex(indexName, new IndexDefinitionBuilder<T>
            {
                Map = map,
                SortOptions = sortOptions
            });
        }

        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map)
        {
            CreateDocumentStore().DatabaseCommands.PutIndex(indexName, new IndexDefinitionBuilder<T>
            {
                Map = map
            });
        }

        public void SaveChanges()
        {
            Session.SaveChanges();
        }

        public IDocumentSession Session
        {
            get
            {
                if (_session != null) return _session;
                _session = CreateDocumentStore().OpenSession();
                DataContextCache.Current.Set(_key, _session);
                return _session;
            }
        }

        public string Source
        {
            get
            {
                return Session.Advanced.DocumentStore.Url;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_session == null) return;
            DataContextCache.Current.Cleanup(_key);
            _session.Dispose();
            _session = null;
        }
    }
}
