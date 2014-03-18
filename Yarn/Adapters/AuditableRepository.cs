using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;

namespace Yarn.Adapters
{
    public class AuditableRepository : IRepository, ILoadServiceProvider, IMetaDataProvider
    {
         private IRepository _repository;
        private IPrincipal _principal;

        public AuditableRepository(IRepository repository, IPrincipal principal)
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
            return _repository.GetById<T, ID>(id);
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            return _repository.GetByIdList<T, ID>(ids);
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return _repository.Find<T>(criteria);
        }

        public T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return _repository.Find<T>(criteria);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class
        {
            return _repository.FindAll<T>(criteria, offset, limit);
        }

        public IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class
        {
            return _repository.FindAll<T>(criteria, offset, limit);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return _repository.Execute<T>(command, parameters);
        }

        public T Add<T>(T entity) where T : class
        {
            if (entity is IAuditable)
            {
                ((IAuditable)entity).CreateDate = DateTime.UtcNow;
                if (_principal != null && _principal.Identity != null)
                {
                    ((IAuditable)entity).CreatedBy = _principal.Identity.Name;
                }
                
                ((IAuditable)entity).Cascade((root, item) =>
                {
                    item.CreateDate = root.CreateDate;
                    item.CreatedBy = root.CreatedBy;
                });
            }
            return _repository.Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            return _repository.Remove<T>(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            return _repository.Remove<T, ID>(id);
        }

        public T Update<T>(T entity) where T : class
        {
            if (entity is IAuditable)
            {
                ((IAuditable)entity).UpdateDate = DateTime.UtcNow;
                if (_principal != null && _principal.Identity != null)
                {
                    ((IAuditable)entity).UpdatedBy = _principal.Identity.Name;
                }

                ((IAuditable)entity).Cascade((root, item) =>
                {
                    item.UpdateDate = root.UpdateDate;
                    item.UpdatedBy = root.UpdatedBy;
                });
            }
            return _repository.Update(entity);
        }

        public long Count<T>() where T : class
        {
            return _repository.Count<T>();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return _repository.Count<T>(criteria);
        }

        public long Count<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return _repository.Count<T>(criteria);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return _repository.All<T>();
        }

        public void Detach<T>(T entity) where T : class
        {
            _repository.Detach<T>(entity);
        }

        public void Attach<T>(T entity) where T : class
        {
            _repository.Attach<T>(entity);
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
    }
}
