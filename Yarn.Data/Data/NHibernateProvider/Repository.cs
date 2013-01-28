using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Yarn;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;

namespace Yarn.Data.NHibernateProvider
{
    public class Repository : IRepository
    {
        private IDataContext<ISession> _context;
        protected readonly string _contextKey;

        public Repository() : this(null) { }

        public Repository(string contextKey = null)
        {
            _contextKey = contextKey;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return this.GetById<T, ID>(id, null);
        }

        public T GetById<T, ID>(ID id, LockMode lockMode) where T : class
        {
            var session = this.PrivateContext.Session;
            if (lockMode != null)
            {
                return session.Get<T>(id, lockMode);
            }
            else
            {
                return session.Get<T>(id);
            }
        }

        public T LoadById<T, ID>(ID id) where T : class
        {
            return this.LoadById<T, ID>(id, null);
        }

        public T LoadById<T, ID>(ID id, LockMode lockMode) where T : class
        {
            var session = this.PrivateContext.Session;
            if (lockMode != null)
            {
                return session.Load<T>(id, lockMode);
            }
            else
            {
                return session.Load<T>(id);
            }
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.All<T>().Where(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.All<T>().Where(criteria);
        }
        
        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria) where T : class
        {
            return criteria.Apply(this.All<T>());
        }

        public IList<T> Execute<T>(string command, params System.Tuple<string, object>[] parameters) where T : class
        {
            var text = new StringBuilder();
            text.AppendFormat("exec {0}", command);
            if (parameters.Length > 0)
            {
                text.Append(" ");
                foreach (var tuple in parameters)
                {
                    text.AppendFormat(":{0}", tuple.Item1);
                }
            }

            var query = this.PrivateContext.Session.CreateSQLQuery(text.ToString());
            foreach (var parameter in parameters)
            {
                query.SetParameter(parameter.Item1, parameter.Item2);
            }
            query.SetResultTransformer(Transformers.AliasToBean<T>());
            return query.List<T>();
        }

        public T Add<T>(T entity) where T : class
        {
            this.PrivateContext.Session.SaveOrUpdate(entity);
            return entity;
        }

        public T Remove<T>(T entity) where T : class
        {
            this.PrivateContext.Session.Delete(entity);
            return entity;
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var entity = GetById<T, ID>(id);
            this.PrivateContext.Session.Delete(entity);
            return entity;
            //var result = this.PrivateContext.Session.Delete<T, ID>(id);
            //return result;
        }

        public T Merge<T>(T entity) where T : class
        {
            this.PrivateContext.Session.Update(entity);
            return entity;
        }

        public void SaveChanges()
        {
            this.DataContext.SaveChanges();
        }

        public void Attach<T>(T entity) where T : class
        {
            this.PrivateContext.Session.Merge(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            this.PrivateContext.Session.Evict(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return this.PrivateContext.Session.Query<T>();
        }

        public long Count<T>() where T : class
        {
            return this.All<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        private IDataContext<ISession> PrivateContext
        {
            get
            {
                return (IDataContext<ISession>)this.DataContext;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = ObjectFactory.Resolve<IDataContext<ISession>>(_contextKey);
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
    }
}
