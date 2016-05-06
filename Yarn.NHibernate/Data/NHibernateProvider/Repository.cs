using System.Reflection;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Linq;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Util;
using Yarn;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;
using System.Collections;
using Expression = System.Linq.Expressions.Expression;

namespace Yarn.Data.NHibernateProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider
    {
        private IDataContext<ISession> _context;
        protected readonly string DataContextInstanceName;

        public Repository() : this(null) { }

        public Repository(string dataContextInstanceName = null)
        {
            DataContextInstanceName = dataContextInstanceName;
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            return GetById<T, TKey>(id, null);
        }

        public T GetById<T, TKey>(TKey id, LockMode lockMode) where T : class
        {
            var session = Session;
            if (lockMode != null)
            {
                return session.Get<T>(id, lockMode);
            }
            return session.Get<T>(id);
        }

        public IEnumerable<T> GetById<T, TKey>(IList<TKey> ids) where T : class
        {
            var session = Session;
            var criteria = session.CreateCriteria<T>();
            var idsRestriction = Restrictions.Disjunction();
            ids.ForEach(id => idsRestriction.Add(Restrictions.IdEq(id)));
            criteria.Add(idsRestriction);
            return criteria.Future<T>();
        }

        public T LoadById<T, TKey>(TKey id) where T : class
        {
            return LoadById<T, TKey>(id, null);
        }

        public T LoadById<T, TKey>(TKey id, LockMode lockMode) where T : class
        {
            var session = Session;
            if (lockMode != null)
            {
                return session.Load<T>(id, lockMode);
            }
            return session.Load<T>(id);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return All<T>().Where(criteria).FirstOrDefault();
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
            var query = criteria.Apply(this.All<T>());
            return this.Page(query, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            var query = Session.CreateSQLQuery(command);
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
            Session.SaveOrUpdate(entity);
            return entity;
        }

        public T Remove<T>(T entity) where T : class
        {
            Session.Delete(entity);
            return entity;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            var entity = GetById<T, TKey>(id);
            Session.Delete(entity);
            return entity;
            //var result = this.Session.Delete<T, ID>(id);
            //return result;
        }

        public T Update<T>(T entity) where T : class
        {
            Session.Merge(entity);
            return entity;
        }

        public void Attach<T>(T entity) where T : class
        {
            var session = Session;
            var persister = session.GetSessionImplementation().GetEntityPersister(NHibernateProxyHelper.GuessClass(entity).FullName, entity);
            var fields = persister.GetPropertyValues(entity, Session.ActiveEntityMode);
            var id = persister.GetIdentifier(entity, session.ActiveEntityMode);
            var entry = session.GetSessionImplementation().PersistenceContext.AddEntry(entity, Status.Loaded, fields, null, id, null, LockMode.None, true, persister, true, false);
        }

        public void Detach<T>(T entity) where T : class
        {
            Session.Evict(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return Session.Query<T>();
        }

        public long Count<T>() where T : class
        {
            return All<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return All<T>().LongCount(criteria);
        }

        protected ISession Session
        {
            get
            {
                return ((IDataContext<ISession>)DataContext).Session;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = ObjectContainer.Current.Resolve<IDataContext<ISession>>(DataContextInstanceName);
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
            readonly IQueryable<T> _query;
            readonly IRepository _repository;
            readonly MethodInfo _fetchMany = typeof(EagerFetchingExtensionMethods).GetMethod("FetchMany");
            readonly MethodInfo _thenFetchMany = typeof(EagerFetchingExtensionMethods).GetMethod("ThenFetchMany");
            readonly MethodInfo _thenFetch = typeof(EagerFetchingExtensionMethods).GetMethod("ThenFetch");

            public LoadService(IRepository repository)
            {
                _repository = repository;
                _query = repository.All<T>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path)
                where TProperty : class
            {
                var properties = path.Body.ToString().Split('.').Where(p => !p.StartsWith("Select")).Select(p => p.TrimEnd(')')).ToArray();

                if (properties.Length == 2)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(typeof(TProperty)))
                    {
                        ((IQueryable<T>)_fetchMany.Invoke(null, new object[] { _query, path })).ToFuture();
                    }
                    else
                    {
                        _query.Fetch(path).ToFuture();
                    }
                }
                else if (properties.Length > 2)
                {
                    var current = typeof(T);
                    IQueryable<T> query = null;
                    for (var i = 1; i < properties.Length; i++)
                    {
                        if (i == 1)
                        {
                            if (typeof(IEnumerable).IsAssignableFrom(typeof(TProperty)))
                            {
                                query = (IQueryable<T>)_fetchMany.Invoke(null, new object[] { _query, path });
                                current = typeof(TProperty).GetGenericArguments()[0];
                            }
                            else
                            {
                                query = _query.Fetch(path);
                                current = typeof(TProperty);
                            }
                        }
                        else
                        {
                            var property = current.GetProperty(properties[i]);
                            if (property == null)
                            {
                                break;
                            }

                            var propertyType = property.PropertyType;
                            var parameter = Expression.Parameter(current);
                            var body = Expression.Convert(Expression.PropertyOrField(parameter, properties[i]), propertyType);
                            var selector = Expression.Lambda(body, parameter);

                            if (typeof(IEnumerable).IsAssignableFrom(propertyType))
                            {
                                query = (IQueryable<T>)_thenFetchMany.Invoke(null, new object[] { query, selector });
                                current = propertyType.GetGenericArguments()[0];
                            }
                            else
                            {
                                query = (IQueryable<T>)_thenFetch.Invoke(null, new object[] { query, selector });
                                current = propertyType;
                            }
                        }
                    }
                    query.ToFuture();
                }
                return this;
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _query.FirstOrDefault(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                var query = _query.Where(criteria);
                return _repository.Page(query, offset, limit, orderBy);
            }

            public T Find(ISpecification<T> criteria)
            {
                return Find(((Specification<T>)criteria).Predicate);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
            }

            public IQueryable<T> All()
            {
                return _query;
            }

            public T Update(T entity)
            {
                var loadedEntity = Find(_repository.As<IMetaDataProvider>().BuildPrimaryKeyExpression(entity));
                return loadedEntity != null ? _repository.Update(entity) : null;
            }

            public void Dispose()
            {
                
            }
        }

        #endregion
    }
}
