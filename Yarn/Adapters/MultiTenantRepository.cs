using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Linq.Expressions;
using Yarn.Specification;

namespace Yarn.Adapters
{
    public class MultiTenantRepository : IRepository, ILoadServiceProvider, IMetaDataProvider
    {
        private readonly IRepository _repository;
        private readonly ITenant _owner;

        public MultiTenantRepository(IRepository repository, ITenant owner)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            _repository = repository;
            _owner = owner;
        }
        
        public T GetById<T, ID>(ID id) where T : class
        {
            var result = _repository.GetById<T, ID>(id);
            var tenant = result as ITenant;
            if (tenant == null)
            {
                return result;
            }
            return tenant.TenantId != _owner.TenantId ? null : result;
        }
        
        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            Expression<Func<T, bool>> filter = e => ((ITenant)e).TenantId == _owner.TenantId;

            return typeof(ITenant).IsAssignableFrom(typeof(T)) ? _repository.All<T>().Where(CastRemoverVisitor<ITenant>.Convert(filter)).FirstOrDefault(criteria) : _repository.Find(criteria);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            if (!typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                return _repository.FindAll(criteria, offset, limit, orderBy);
            }

            Expression<Func<T, bool>> filter = e => ((ITenant)e).TenantId == _owner.TenantId;
            var spec = ((Specification<T>)criteria).And(CastRemoverVisitor<ITenant>.Convert(filter));
            var query = _repository.All<T>().Where(spec.Predicate);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            if (!typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                return _repository.FindAll(criteria, offset, limit, orderBy);
            }

            Expression<Func<T, bool>> filter = e => ((ITenant)e).TenantId == _owner.TenantId;
            var spec = new Specification<T>(CastRemoverVisitor<ITenant>.Convert(filter)).And(criteria); ;
            var query = _repository.All<T>().Where(spec.Predicate);
            return this.Page(query, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            Expression<Func<T, bool>> filter = e => ((ITenant)e).TenantId == _owner.TenantId;
            return typeof(ITenant).IsAssignableFrom(typeof(T)) ? _repository.Execute<T>(command, parameters).Where(CastRemoverVisitor<ITenant>.Convert(filter).Compile()).ToArray() : _repository.Execute<T>(command, parameters);
        }

        public T Add<T>(T entity) where T : class
        {
            var tenant = entity as ITenant;
            if (tenant == null)
            {
                return _repository.Add(entity);
            }

            if (tenant.TenantId == _owner.TenantId)
            {
                return _repository.Add(entity);
            }
            throw new InvalidOperationException();
        }

        public T Remove<T>(T entity) where T : class
        {
            var tenant = entity as ITenant;
            if (tenant == null)
            {
                return _repository.Remove(entity);
            }

            if (tenant.TenantId == _owner.TenantId)
            {
                return _repository.Remove(entity);
            }
            throw new InvalidOperationException();
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                var entity = _repository.GetById<T, ID>(id);
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    return _repository.Remove(entity);
                }
                throw new InvalidOperationException();
            }
            return _repository.Remove<T, ID>(id);
        }

        public T Update<T>(T entity) where T : class
        {
            var tenant = entity as ITenant;
            if (tenant == null)
            {
                return _repository.Update(entity);
            }

            if (tenant.TenantId == _owner.TenantId)
            {
                return _repository.Update(entity);
            }
            throw new InvalidOperationException();
        }

        public long Count<T>() where T : class
        {
            Expression<Func<T, bool>> filter = e => ((ITenant)e).TenantId == _owner.TenantId;
            return typeof(ITenant).IsAssignableFrom(typeof(T)) ? _repository.All<T>().LongCount(CastRemoverVisitor<ITenant>.Convert(filter)) : _repository.Count<T>();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).AsQueryable().LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).AsQueryable().LongCount();
        }

        public IQueryable<T> All<T>() where T : class
        {
            Expression<Func<T, bool>> filter = e => ((ITenant)e).TenantId == _owner.TenantId;
            return typeof(ITenant).IsAssignableFrom(typeof(T)) ? _repository.All<T>().Where(CastRemoverVisitor<ITenant>.Convert(filter)) : _repository.All<T>();
        }

        public void Detach<T>(T entity) where T : class
        {
            var tenant = entity as ITenant;
            if (tenant != null)
            {
                if (tenant.TenantId == _owner.TenantId)
                {
                    _repository.Detach(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                _repository.Detach(entity);
            }
        }

        public void Attach<T>(T entity) where T : class
        {
            var tenant = entity as ITenant;
            if (tenant != null)
            {
                if (tenant.TenantId == _owner.TenantId)
                {
                    _repository.Attach(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                _repository.Attach(entity);
            }
        }

        public IDataContext DataContext
        {
            get { return _repository.DataContext; }
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

        ILoadService<T> ILoadServiceProvider.Load<T>()
        {
            var provider = _repository as ILoadServiceProvider;
            if (provider != null)
            {
                return provider.Load<T>();
            }
            throw new InvalidOperationException();
        }

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            var provider = _repository as IMetaDataProvider;
            if (provider != null)
            {
                return provider.GetPrimaryKey<T>();
            }
            throw new InvalidOperationException();
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var provider = _repository as IMetaDataProvider;
            if (provider != null)
            {
                return provider.GetPrimaryKeyValue(entity);
            }
            throw new InvalidOperationException();
        }

        public long TenantId
        {
            get { return _owner.TenantId; }
        }

        public long OwnerId
        {
            get { return _owner.OwnerId; }
        }
    }
}
