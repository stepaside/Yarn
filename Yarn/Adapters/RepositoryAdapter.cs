using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;

namespace Yarn.Adapters
{
    public abstract class RepositoryAdapter : IRepository, ILoadServiceProvider, IMetaDataProvider
    {
        protected readonly IRepository _repository;

        protected RepositoryAdapter(IRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            _repository = repository;
        }

        public virtual T GetById<T, ID>(ID id) where T : class
        {
            return _repository.GetById<T, ID>(id);
        }

        public virtual T Find<T>(ISpecification<T> criteria) where T : class
        {
            return _repository.Find(criteria);
        }

        public virtual T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return _repository.Find(criteria);
        }

        public virtual IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return _repository.FindAll(criteria, offset, limit, orderBy);
        }

        public virtual IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return _repository.FindAll(criteria, offset, limit, orderBy);
        }

        public virtual IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return _repository.Execute<T>(command, parameters);
        }

        public virtual T Add<T>(T entity) where T : class
        {
            return _repository.Add(entity);
        }

        public virtual T Remove<T>(T entity) where T : class
        {
            return _repository.Remove(entity);
        }

        public virtual T Remove<T, ID>(ID id) where T : class
        {
            return _repository.Remove<T, ID>(id);
        }

        public virtual T Update<T>(T entity) where T : class
        {
            return _repository.Update(entity);
        }

        public virtual long Count<T>() where T : class
        {
            return _repository.Count<T>();
        }

        public virtual long Count<T>(ISpecification<T> criteria) where T : class
        {
            return _repository.Count(criteria);
        }

        public virtual long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return _repository.Count(criteria);
        }

        public virtual IQueryable<T> All<T>() where T : class
        {
            return _repository.All<T>();
        }

        public virtual void Detach<T>(T entity) where T : class
        {
            _repository.Detach(entity);
        }

        public virtual void Attach<T>(T entity) where T : class
        {
            _repository.Attach(entity);
        }

        public virtual IDataContext DataContext
        {
            get { return _repository.DataContext; }
        }

        public virtual void Dispose()
        {
            _repository.Dispose();
        }

        public virtual ILoadService<T> Load<T>() where T : class
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
    }
}
