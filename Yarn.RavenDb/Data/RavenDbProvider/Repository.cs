using Raven.Client;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Yarn;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Data.RavenDbProvider
{
    public class Repository : IRepository, IMetaDataProvider, IBulkOperationsProvider
    {
        private IDataContext<IDocumentSession> _context;
        private readonly Action<IDocumentQueryCustomization> _queryCustomization;
        private readonly string _connectionString;

        public Repository(string connectionString = null, Action<IDocumentQueryCustomization> queryCustomization = null)
        {
            _connectionString = connectionString;
            _queryCustomization = queryCustomization;
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            return _context.Session.Load<T>(id.ToString());
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = All<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = criteria.Apply(All<T>());
            return this.Page(query, offset, limit, orderBy);
        }

        public T FindOne<T>(Expression<Func<T, bool>> criteria, bool waitForNonStaleResults = false) where T : class
        {
            var foundList = FindAll(criteria);
            try
            {
                return foundList.SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The query returned more than one result. Please refine your query.");
            }
        }

        public T Add<T>(T entity) where T : class
        {
            _context.Session.Store(entity);
            return entity;
        }

        public T Remove<T>(T entity) where T : class
        {
            _context.Session.Delete(entity);
            return entity;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            var entity = GetById<T, TKey>(id);
            if (entity != null)
            {
                _context.Session.Delete(entity);
            }
            return entity;
        }

        public T Update<T>(T entity) where T : class
        {
            _context.Session.Store(entity);
            return entity;
        }

        public void Attach<T>(T entity) where T : class
        {
            throw new NotImplementedException();
        }

        public void Detach<T>(T entity) where T : class
        {
            _context.Session.Advanced.Evict(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            var query = _context.Session.Query<T>();
            return _queryCustomization != null ? query.Customize(_queryCustomization) : query;
        }

        public long Count<T>() where T : class
        {
            QueryStatistics stats;
            _context.Session.Query<T>().Statistics(out stats);
            return stats.TotalResults;
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return _context.Session.Query<T>().Where(criteria).LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return criteria.Apply(_context.Session.Query<T>()).LongCount();
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            // Execute is used to query RavenDB index
            var indexQuery = _context.Session.Query<T>(command);
            if (parameters != null && parameters.Count > 0)
            {
                // De-duplicate parameters and oraganize them into a dictionary
                var args = (Dictionary<string, object>)parameters;

                object criteria, offset, limit;

                if (args.TryGetValue("criteria", out criteria))
                {
                    if (criteria is Expression<Func<T, bool>>)
                    {
                        indexQuery = indexQuery.Where((Expression<Func<T, bool>>)criteria);
                    }
                    else if (criteria is Specification<T>)
                    {
                        indexQuery = indexQuery.Where(((Specification<T>)criteria).Predicate);
                    }
                }

                if (args.TryGetValue("offset", out offset) && offset is int && ((int)offset > 0))
                {
                    indexQuery = indexQuery.Skip((int)offset);
                }

                if (args.TryGetValue("limit", out limit) && limit is int && ((int)limit > 0))
                {
                    indexQuery = indexQuery.Take((int)limit);
                }
            }

            return indexQuery.ToArray();
        }

        protected IDocumentSession DocumentSession
        {
            get
            {
                return ((IDataContext<IDocumentSession>)DataContext).Session;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                return _context ?? (_context = new DataContext(_connectionString));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _context == null) return;
            _context.Dispose();
            _context = null;
        }

        #region IMetaDataProvider Members

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return new[] { DocumentSession.Advanced.DocumentStore.Conventions.GetIdentityProperty(typeof(T)).Name };
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            return new object[] { DocumentSession.Advanced.GetDocumentId(entity) };
        }
        
        #endregion

        public IEnumerable<T> GetById<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            return _context.Session.Load<T>(ids.Select(i => i.ToString())).Values;
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            var count = 0L;
            using (var bulkInsert = _context.Session.Advanced.DocumentStore.BulkInsert())
            {
                foreach (var entity in entities)
                {
                    bulkInsert.Store(entity);
                    count++;
                }
            }
            return count;
        }

        public long Update<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> update) where T : class
        {
            throw new NotImplementedException();
        }

        public long Update<T>(params BulkUpdateOperation<T>[] bulkOperations) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(IEnumerable<T> entities) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(params Expression<Func<T, bool>>[] criteria) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            throw new NotImplementedException();
        }
    }
}
