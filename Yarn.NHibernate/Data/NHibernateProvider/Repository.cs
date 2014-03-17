using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Yarn;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using NHibernate.Engine;
using Yarn.Reflection;
using NHibernate.Criterion;
using Yarn.Specification;

namespace Yarn.Data.NHibernateProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider
    {
        private IDataContext<ISession> _context;
        protected readonly string _dataContextInstanceName;

        public Repository() : this(null) { }

        public Repository(string dataContextInstanceName = null)
        {
            _dataContextInstanceName = dataContextInstanceName;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return this.GetById<T, ID>(id, null);
        }

        public T GetById<T, ID>(ID id, LockMode lockMode) where T : class
        {
            var session = this.Session;
            if (lockMode != null)
            {
                return session.Get<T>(id, lockMode);
            }
            else
            {
                return session.Get<T>(id);
            }
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            var session = this.Session;
            var criteria = session.CreateCriteria<T>();
            var idsRestriction = Restrictions.Disjunction();
            ids.ForEach(id => idsRestriction.Add(Restrictions.IdEq(id)));
            criteria.Add(idsRestriction);
            return criteria.Future<T>();
        }

        public T LoadById<T, ID>(ID id) where T : class
        {
            return this.LoadById<T, ID>(id, null);
        }

        public T LoadById<T, ID>(ID id, LockMode lockMode) where T : class
        {
            var session = this.Session;
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

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class
        {
            var results = this.All<T>().Where(criteria);
            if (offset >= 0 && limit > 0)
            {
                results = results.Skip(offset).Take(limit);
            }
            return results;
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class
        {
            var results = criteria.Apply(this.All<T>());
            if (offset >= 0 && limit > 0)
            {
                results = results.Skip(offset).Take(limit);
            }
            return results;
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            var query = this.Session.CreateSQLQuery(command);
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    query.SetParameter(parameter.Key, parameter.Value);
                }
            }
            query.SetResultTransformer(Transformers.AliasToBean<T>());
            return query.List<T>();
        }

        public T Add<T>(T entity) where T : class
        {
            this.Session.SaveOrUpdate(entity);
            return entity;
        }

        public T Remove<T>(T entity) where T : class
        {
            this.Session.Delete(entity);
            return entity;
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var entity = GetById<T, ID>(id);
            this.Session.Delete(entity);
            return entity;
            //var result = this.Session.Delete<T, ID>(id);
            //return result;
        }

        public T Update<T>(T entity) where T : class
        {
            this.Session.Update(entity);
            return entity;
        }

        public void Attach<T>(T entity) where T : class
        {
            this.Session.Merge(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            this.Session.Evict(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return this.Session.Query<T>();
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

        protected ISession Session
        {
            get
            {
                return ((IDataContext<ISession>)this.DataContext).Session;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = ObjectContainer.Current.Resolve<IDataContext<ISession>>(_dataContextInstanceName);
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
                    _context.Dispose();
                    _context = null;
                }
            }
        }

        #region IMetaDataProvider Members

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return new[] { this.Session.SessionFactory.GetClassMetadata(typeof(T)).IdentifierPropertyName };
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var key = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();
            return new [] { PropertyAccessor.Get(entity, key) };
        }

        #endregion

        #region LoadServiceProvider Members

        ILoadService<T> ILoadServiceProvider.Load<T>()
        {
            return new LoadService<T>(this);
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            IQueryable<T> _query;
            IRepository _repository;

            public LoadService(IRepository repository)
            {
                _repository = repository;
                _query = repository.All<T>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path)
                where TProperty : class
            {
                _query = _query.Fetch(path);
                return this;
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _query.FirstOrDefault(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0)
            {
                var query = _query.Where(criteria);
                if (offset > 0)
                {
                    query.Skip(offset);
                }
                if (limit > 0)
                {
                    query.Take(limit);
                }
                return query;
            }

            public T Find(ISpecification<T> criteria)
            {
                return Find(((Specification<T>)criteria).Predicate);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0)
            {
                return FindAll(((Specification<T>)criteria).Predicate, offset, limit);
            }

            public IQueryable<T> All()
            {
                return _query;
            }

            public void Dispose()
            {
                
            }
        }

        #endregion
    }
}
