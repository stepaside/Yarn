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
    public class SoftDeleteRepository : IRepository, ILoadServiceProvider, IMetaDataProvider
    {
        private readonly IRepository _repository;
        private readonly IPrincipal _principal;

        public SoftDeleteRepository(IRepository repository, IPrincipal principal)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }

            _repository = repository;
            _principal = principal;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            var entity = _repository.GetById<T, ID>(id);
            if (entity is ISoftDelete && ((ISoftDelete)entity).IsDeleted)
            {
                return null;
            }
            return entity;
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? _repository.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)).FirstOrDefault(criteria) : _repository.Find<T>(criteria);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return FindAll(((Specification<T>)criteria).Predicate, offset, limit);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
                var query = _repository.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)).Where(criteria);
                return this.Page(query, offset, limit, orderBy);
            }
            return _repository.FindAll(criteria, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? _repository.Execute<T>(command, parameters).Where(CastRemoverVisitor<ISoftDelete>.Convert(filter).Compile()).ToArray() : _repository.Execute<T>(command, parameters);
        }

        public T Add<T>(T entity) where T : class
        {
            return _repository.Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            var deleted = entity as ISoftDelete;
            if (deleted == null)
            {
                return _repository.Remove(entity);
            }
            deleted.IsDeleted = true;
            deleted.UpdateDate = DateTime.UtcNow;
            if (_principal != null)
            {
                deleted.UpdatedBy = _principal.Identity.Name;
            }
            return _repository.Update(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return _repository.Remove<T, ID>(id);
            }
            var entity = _repository.GetById<T, ID>(id);
            ((ISoftDelete)entity).IsDeleted = true;
            ((ISoftDelete)entity).UpdateDate = DateTime.UtcNow;
            if (_principal != null)
            {
                ((ISoftDelete)entity).UpdatedBy = _principal.Identity.Name;
            }
            return _repository.Update(entity);
        }

        public T Update<T>(T entity) where T : class
        {
            return _repository.Update(entity);
        }

        public long Count<T>() where T : class
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? _repository.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)).LongCount() : _repository.Count<T>();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).LongCount();
        }

        public IQueryable<T> All<T>() where T : class
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? _repository.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)) : _repository.All<T>();
        }

        public void Detach<T>(T entity) where T : class
        {
            _repository.Detach(entity);
        }

        public void Attach<T>(T entity) where T : class
        {
            _repository.Attach(entity);
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
                return provider.GetPrimaryKeyValue<T>(entity);
            }
            throw new InvalidOperationException();
        }
    }
}
