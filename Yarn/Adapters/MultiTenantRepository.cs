using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Adapters
{
    public class MultiTenantRepository : IRepository, ILoadServiceProvider, IMetaDataProvider
    {
        private IRepository _repository;
        private ITenant _owner;

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
            if (result is ITenant)
            {
                if (((ITenant)result).TenantId != _owner.TenantId)
                {
                    return null;
                }
            }
            return result;
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return _repository.GetByIdList<T, ID>(ids).Where(e => ((ITenant)e).TenantId == _owner.TenantId);
            }
            else
            {
                return _repository.GetByIdList<T, ID>(ids);
            }
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return this.Find<T>(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                return _repository.All<T>().Where(e => ((ITenant)e).TenantId == _owner.TenantId).FirstOrDefault(criteria);
            }
            else
            {
                return _repository.Find<T>(criteria);
            }
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return this.FindAll<T>(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                var query = _repository.All<T>().Where(e => ((ITenant)e).TenantId == _owner.TenantId);
                return this.Page<T>(query, offset, limit, orderBy);
            }
            else
            {
                return _repository.FindAll<T>(criteria, offset, limit, orderBy);
            }
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                return _repository.Execute<T>(command, parameters).Where(e => ((ITenant)e).TenantId == _owner.TenantId).ToArray();
            }
            else
            {
                return _repository.Execute<T>(command, parameters);
            }
        }

        public T Add<T>(T entity) where T : class
        {
            if (entity is ITenant)
            {
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    return _repository.Add(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                return _repository.Add(entity);
            }
        }

        public T Remove<T>(T entity) where T : class
        {
            if (entity is ITenant)
            {
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    return _repository.Remove<T>(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                return _repository.Remove<T>(entity);
            }
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                var entity = _repository.GetById<T, ID>(id);
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    return _repository.Remove<T>(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                return _repository.Remove<T, ID>(id);
            }
        }

        public T Update<T>(T entity) where T : class
        {
            if (entity is ITenant)
            {
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    return _repository.Update(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                return _repository.Update(entity);
            }
        }

        public long Count<T>() where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                return _repository.All<T>().LongCount(e => ((ITenant)e).TenantId == _owner.TenantId);
            }
            else
            {
                return _repository.Count<T>();
            }
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public IQueryable<T> All<T>() where T : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(T)))
            {
                return _repository.All<T>().Where(e => ((ITenant)e).TenantId == _owner.TenantId);
            }
            else
            {
                return _repository.All<T>();
            }
        }

        public void Detach<T>(T entity) where T : class
        {
            if (entity is ITenant)
            {
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    _repository.Detach<T>(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                _repository.Detach<T>(entity);
            }
        }

        public void Attach<T>(T entity) where T : class
        {
            if (entity is ITenant)
            {
                if (((ITenant)entity).TenantId == _owner.TenantId)
                {
                    _repository.Attach<T>(entity);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                _repository.Attach<T>(entity);
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
            if (_repository is ILoadServiceProvider)
            {
                return ((ILoadServiceProvider)_repository).Load<T>();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            if (_repository is IMetaDataProvider)
            {
                return ((IMetaDataProvider)_repository).GetPrimaryKey<T>();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            if (_repository is IMetaDataProvider)
            {
                return ((IMetaDataProvider)_repository).GetPrimaryKeyValue<T>(entity);
            }
            else
            {
                throw new InvalidOperationException();
            }
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
