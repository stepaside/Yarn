using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Yarn;
using Raven.Client;
using Raven.Client.Linq;

namespace Yarn.Data.RavenDbProvider
{
    public class Repository : IRepository, IMetaDataProvider
    {
        private IDataContext<IDocumentSession> _context;
        private bool _waitForNonStaleResults;
        private string _contextKey;

        public Repository() : this(false, null) { }

        public Repository(bool waitForNonStaleResults = false, string contextKey = null)
        {
            _waitForNonStaleResults = waitForNonStaleResults;
            _contextKey = contextKey;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return _context.Session.Load<T>(id.ToString());
        }

        public IList<T> GetByIdList<T, ID>(params ID[] ids) where T : class
        {
            return _context.Session.Load<T>(ids.Select(i => i.ToString()));
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return _context.Session.Query<T>().Customize(q => CustomizeQuery(q, _waitForNonStaleResults)).Where(criteria).ToList();
        }
        
        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria) where T : class
        {
            return criteria.Apply(this.All<T>());
        }

        public T FindOne<T>(Expression<Func<T, bool>> criteria, bool waitForNonStaleResults = false) where T : class
        {
            var foundList = this.FindAll(criteria);
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
            _context.Session.Delete<T>(entity);
            return entity;
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var entity = GetById<T, ID>(id);
            if (entity != null)
            {
                _context.Session.Delete<T>(entity);
            }
            return entity;
        }

        public T Merge<T>(T entity) where T : class
        {
            _context.Session.Store(entity);
            return entity;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public void Attach<T>(T entity) where T : class
        {
            throw new NotImplementedException();
        }

        public void Detach<T>(T entity) where T : class
        {
            _context.Session.Advanced.Evict<T>(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return _context.Session.Query<T>();
        }

        public long Count<T>() where T : class
        {
            RavenQueryStatistics stats;
            _context.Session.Query<T>().Statistics(out stats);
            return stats.TotalResults;
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            RavenQueryStatistics stats;
            _context.Session.Query<T>().Where(criteria).Statistics(out stats);
            return stats.TotalResults;
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            RavenQueryStatistics stats;
            ((IRavenQueryable<T>)criteria.Apply(_context.Session.Query<T>())).Statistics(out stats);
            return stats.TotalResults;
        }

        public IList<T> Execute<T>(string command, params System.Tuple<string, object>[] parameters) where T : class
        {
            throw new NotSupportedException();
        }

        public IDataContext<IDocumentSession> PrivateContext
        {
            get
            {
                return (IDataContext<IDocumentSession>)DataContext;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = new DataContext(_contextKey);
                }
                return _context;
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
                if (_context != null)
                {
                    _context.Session.Dispose();
                    _context = null;
                }
            }
        }

        private void CustomizeQuery(IDocumentQueryCustomization p, bool waitForNonStaleResults)
        {
            if (waitForNonStaleResults)
            {
                p.WaitForNonStaleResults();
            }
        }

        #region IMetaDataProvider Members

        IEnumerable<string> IMetaDataProvider.GetPrimaryKey<T>()
        {
            return new[] { this.PrivateContext.Session.Advanced.DocumentStore.Conventions.GetIdentityProperty(typeof(T)).Name };
        }

        IDictionary<string, object> IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var key = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();
            var value = this.PrivateContext.Session.Advanced.GetDocumentId(entity);
            return new Dictionary<string, object> { { key, value } };
        }
        
        #endregion
    }
}
