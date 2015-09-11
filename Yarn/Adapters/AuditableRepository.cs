using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using Yarn.Extensions;

namespace Yarn.Adapters
{
    public class AuditableRepository : RepositoryAdapter
    {
        private readonly Func<string> _getOwnerIdentity;

        public AuditableRepository(IRepository repository, IPrincipal principal) 
            : this(repository, () => principal.Identity.Name)
        {
            if (principal == null)
                throw new ArgumentNullException("principal");
        }

        public AuditableRepository(IRepository repository, Func<string> getOwnerIdentity)
            :base(repository)
        {
            if (getOwnerIdentity == null)
                throw new ArgumentNullException("getOwnerIdentity");
            _getOwnerIdentity = getOwnerIdentity;
        }

        public override T Add<T>(T entity)
        {
            var auditable = entity as IAuditable;
            if (auditable == null)
            {
                return Repository.Add(entity);
            }

            auditable.AuditId = Guid.NewGuid();
            auditable.CreateDate = DateTime.UtcNow;
            auditable.CreatedBy = _getOwnerIdentity();
                
            auditable.Cascade((root, item) =>
            {
                if (item.CreateDate != DateTime.MinValue && item.AuditId.HasValue) return;
                item.CreateDate = root.CreateDate;
                item.CreatedBy = root.CreatedBy;
                item.AuditId = root.AuditId;
            });
            return Repository.Add(entity);
        }

        public override T Update<T>(T entity)
        {
            BeforeUpdate(entity);
            return Repository.Update(entity);
        }

        private void BeforeUpdate<T>(T entity, IReadOnlyCollection<string> paths = null) where T : class
        {
            var auditable = entity as IAuditable;
            if (auditable == null) return;

            auditable.AuditId = Guid.NewGuid();
            auditable.UpdateDate = DateTime.UtcNow;
            auditable.UpdatedBy = _getOwnerIdentity();

            auditable.Cascade((root, item) =>
            {
                if (item.CreateDate == DateTime.MinValue)
                {
                    item.CreateDate = DateTime.UtcNow;
                    item.CreatedBy = root.CreatedBy;
                }
                else
                {
                    item.UpdateDate = root.UpdateDate;
                    item.UpdatedBy = root.UpdatedBy;
                }
                item.AuditId = root.AuditId;
            }, paths);
        }

        public override ILoadService<T> Load<T>()
        {
            var provider = Repository as ILoadServiceProvider;
            if (provider != null)
            {
                return new LoadService<T>(this, provider.Load<T>());
            }
            throw new InvalidOperationException();
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly AuditableRepository _repository;
            private readonly ILoadService<T> _service;
            private readonly List<string> _paths;
            
            public LoadService(AuditableRepository repository, ILoadService<T> service)
            {
                _repository = repository;
                _service = service;
                _paths = new List<string>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                _service.Include(path);

                var properties = string.Join(".", path.Body.ToString().Split('.').Where(p => !p.StartsWith("Select")).Select(p => p.TrimEnd(')')).Skip(1));
                _paths.Add(properties);

                return this;
            }

            public T Update(T entity)
            {
                _repository.BeforeUpdate(entity, _paths);
                return _service.Update(entity);
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _service.Find(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                return _service.FindAll(criteria, offset, limit, orderBy);
            }

            public T Find(ISpecification<T> criteria)
            {
                return _service.Find(criteria);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                return _service.FindAll(criteria, offset, limit, orderBy);
            }

            public IQueryable<T> All()
            {
                return _service.All();
            }

            public void Dispose()
            {
                _service.Dispose();
            }
        }
    }
}
