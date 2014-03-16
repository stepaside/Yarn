using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq.Expressions;
using Yarn;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Shard;

namespace Yarn.Data.RavenDbProvider
{
    public class DataContext : IDataContext<IDocumentSession>
    {
        private static ConcurrentDictionary<string, IDocumentStore> _documentStores = new ConcurrentDictionary<string, IDocumentStore>();
        private string _prefix = null;
        private IDocumentSession _session = DocumentSessionCache.CurrentSession;

        public DataContext(string prefix = null)
        {
            _prefix = prefix;
        }

        protected IDocumentStore CreateDocumentStore(string prefix)
        {
            var documentStore = _documentStores.GetOrAdd(prefix, key =>
            {
                IDocumentStore ds = null;
                var shardCountValue = ConfigurationManager.AppSettings.Get(key + ".ShardCount");
                var shardCount = 0;

                if (int.TryParse(shardCountValue, out shardCount))
                {
                    var shards = new Dictionary<string, IDocumentStore>();
                    for (int i = 0; i < shardCount; i++)
                    {
                        var url = ConfigurationManager.AppSettings.Get(key + ".Shard." + i + ".Url");
                        var id = ConfigurationManager.AppSettings.Get(key + ".Shard." + i + ".Identifier");
                        shards.Add(id, new DocumentStore() { Identifier = id, Url = url });
                    }

                    var accessStrategyTypeName = ConfigurationManager.AppSettings.Get(key + ".Shard.AccessStrategy");
                    var resolutionStrategyTypeName = ConfigurationManager.AppSettings.Get(key + ".Shard.ResolutionStrategy");
                    var selectionStrategyTypeName = ConfigurationManager.AppSettings.Get(key + ".Shard.SelectionStrategy");

                    IShardAccessStrategy accessStrategy;
                    IShardResolutionStrategy resolutionStrategy;

                    if (!string.IsNullOrEmpty(accessStrategyTypeName) && Type.GetType(accessStrategyTypeName) != null)
                    {
                        accessStrategy = (IShardAccessStrategy)Activator.CreateInstance(Type.GetType(accessStrategyTypeName));
                    }
                    else
                    {
                        accessStrategy = new ParallelShardAccessStrategy();
                    }

                    var shardStrategy = new ShardStrategy(shards)
                    {
                        ShardAccessStrategy = accessStrategy
                    };

                    if (!string.IsNullOrEmpty(resolutionStrategyTypeName) && Type.GetType(resolutionStrategyTypeName) != null)
                    {
                        resolutionStrategy = (IShardResolutionStrategy)Activator.CreateInstance(Type.GetType(resolutionStrategyTypeName));
                    }
                    else
                    {
                        resolutionStrategy = new DefaultShardResolutionStrategy(shards.Keys, shardStrategy);
                    }

                    ds = new ShardedDocumentStore(shardStrategy);
                    ds.Initialize();
                }
                else
                {
                    var url = ConfigurationManager.AppSettings.Get(key);

                    if (!string.IsNullOrEmpty(url))
                    {
                        ds = new DocumentStore() { Url = url };
                        ds.Initialize();
                    }
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

        protected IDocumentStore GetDefaultDocumentStore()
        {
            return CreateDocumentStore(DefaultPrefix);
        }

        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map, Expression<Func<IEnumerable<T>, IEnumerable>> reduce, IDictionary<Expression<Func<T, object>>, SortOptions> sortOptions)
        {
            GetDefaultDocumentStore().DatabaseCommands.PutIndex(indexName, new IndexDefinitionBuilder<T>
            {
                Map = map,
                Reduce = reduce,
                SortOptions = sortOptions
            });
        }

        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map, IDictionary<Expression<Func<T, object>>, SortOptions> sortOptions)
        {
            GetDefaultDocumentStore().DatabaseCommands.PutIndex(indexName, new IndexDefinitionBuilder<T>
            {
                Map = map,
                SortOptions = sortOptions
            });
        }

        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map)
        {
            GetDefaultDocumentStore().DatabaseCommands.PutIndex(indexName, new IndexDefinitionBuilder<T>
            {
                Map = map
            });
        }

        public void SaveChanges()
        {
            this.Session.SaveChanges();
        }

        public IDocumentSession Session
        {
            get
            {
                if (_session == null)
                {
                    _session = _prefix == null ? GetDefaultDocumentStore().OpenSession() : CreateDocumentStore(_prefix).OpenSession();
                    DocumentSessionCache.CurrentSession = _session;
                }
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

        public IDataContextCache DataContextCache
        {
            get
            {
                return DocumentSessionCache.Instance;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_session != null)
                {
                    DocumentSessionCache.Instance.Cleanup();
                    _session.Dispose();
                    _session = null;
                }
            }
        }
    }
}
