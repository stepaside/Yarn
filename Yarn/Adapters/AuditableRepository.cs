using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;

namespace Yarn.Adapters
{
    public class AuditableRepository : IRepository, ILoadServiceProvider, IMetaDataProvider
    {
        private readonly IRepository _repository;
        private readonly IPrincipal _principal;

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
            return _repository.Find(criteria);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return _repository.Find(criteria);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return _repository.FindAll(criteria, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return _repository.FindAll(criteria, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return _repository.Execute<T>(command, parameters);
        }

        public T Add<T>(T entity) where T : class
        {
            var auditable = entity as IAuditable;
            if (auditable != null)
            {
                auditable.AuditId = Guid.NewGuid();
                auditable.CreateDate = DateTime.UtcNow;
                if (_principal != null && _principal.Identity != null)
                {
                    auditable.CreatedBy = _principal.Identity.Name;
                }
                
                auditable.Cascade((root, item) =>
                {
                    if (item.CreateDate == DateTime.MinValue || !item.AuditId.HasValue)
                    {
                        item.CreateDate = root.CreateDate;
                        item.CreatedBy = root.CreatedBy;
                        item.AuditId = root.AuditId;
                    }
                });
            }
            return _repository.Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            return _repository.Remove(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            return _repository.Remove<T, ID>(id);
        }

        public T Update<T>(T entity) where T : class
        {
            var auditable = entity as IAuditable;
            if (auditable != null)
            {
                auditable.AuditId = Guid.NewGuid();
                auditable.UpdateDate = DateTime.UtcNow;
                if (_principal != null && _principal.Identity != null)
                {
                    auditable.UpdatedBy = _principal.Identity.Name;
                }

                auditable.Cascade((root, item) =>
                {
                    if (item.CreateDate == DateTime.MinValue || !item.AuditId.HasValue)
                    {
                        if (item.CreateDate == DateTime.MinValue)
                        {
                            item.CreateDate = DateTime.UtcNow;
                        }
                        else
                        {
                            auditable.UpdateDate = root.UpdateDate;
                        }
                        item.CreatedBy = root.CreatedBy;
                        item.AuditId = root.AuditId;
                    }
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
            return _repository.Count(criteria);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return _repository.Count(criteria);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return _repository.All<T>();
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
                return new LoadService<T>(this, provider.Load<T>());
            }
            throw new InvalidOperationException();
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly AuditableRepository _repository;
            private readonly ILoadService<T> _service;
            
            public LoadService(AuditableRepository repository, ILoadService<T> service)
            {
                _repository = repository;
                _service = service;
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                _service.Include(path);
                return this;
            }

            public T Update(T entity)
            {
                var auditable = entity as IAuditable;
                if (auditable != null)
                {
                    auditable.AuditId = Guid.NewGuid();
                    auditable.UpdateDate = DateTime.UtcNow;
                    if (_repository._principal != null && _repository._principal.Identity != null)
                    {
                        auditable.UpdatedBy = _repository._principal.Identity.Name;
                    }

                    auditable.Cascade((root, item) =>
                    {
                        if (item.CreateDate == DateTime.MinValue || !item.AuditId.HasValue)
                        {
                            if (item.CreateDate == DateTime.MinValue)
                            {
                                item.CreateDate = DateTime.UtcNow;
                            }
                            else
                            {
                                auditable.UpdateDate = root.UpdateDate;
                            }
                            item.CreatedBy = root.CreatedBy;
                            item.AuditId = root.AuditId;
                        }
                    });
                }
                return _service.Update(entity);
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _service.Find(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                return _service.FindAll(criteria, offset, limit, orderBy);
            }

            public T Find(ISpecification<T> criteria)
            {
                return _service.Find(criteria);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
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
