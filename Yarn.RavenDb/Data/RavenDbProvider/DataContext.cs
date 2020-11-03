using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents.Session;


namespace Yarn.Data.RavenDbProvider
{
    public class DataContext : IDataContext<IDocumentSession>
    {
        private static readonly ConcurrentDictionary<string, IDocumentStore> DocumentStores = new ConcurrentDictionary<string, IDocumentStore>();

        private readonly IDocumentSession _session;
        private readonly string _connectionString;

        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
            _session = CreateDocumentStore().OpenSession(); ;
        }

        protected IDocumentStore CreateDocumentStore()
        {
            var documentStore = DocumentStores.GetOrAdd(_connectionString, key =>
            {
                IDocumentStore ds = null;
                
                if (_connectionString != null)
                {
                    ds = new DocumentStore { Urls = new[] { key } };
                    ds.Initialize();
                }

                return ds;
            });
            return documentStore;
        }
                
        public void CreateIndex<T>(string indexName, Expression<Func<IEnumerable<T>, IEnumerable>> map, Expression<Func<IEnumerable<T>, IEnumerable>> reduce = null)
        {
            var builder = new IndexDefinitionBuilder<T>(indexName)
            {
                Map = map,
                Reduce = reduce
            };

            var store = CreateDocumentStore();
            store.Maintenance.Send(new PutIndexesOperation(builder.ToIndexDefinition(store.Conventions)));
        }

        public void SaveChanges()
        {
            Session.SaveChanges();
        }

        public IDocumentSession Session
        {
            get
            {
                return _session;
            }
        }

        public string Source
        {
            get
            {
                return _connectionString;
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
            _session.Dispose();
        }
    }
}
