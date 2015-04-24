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
        protected readonly IRepository Repository;

        protected RepositoryAdapter(IRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            Repository = repository;
        }

        public virtual T GetById<T, ID>(ID id) where T : class
        {
            return Repository.GetById<T, ID>(id);
        }

        public virtual T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Repository.Find(criteria);
        }

        public virtual T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Repository.Find(criteria);
        }

        public virtual IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            return Repository.FindAll(criteria, offset, limit, orderBy);
        }

        public virtual IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            return Repository.FindAll(criteria, offset, limit, orderBy);
        }

        public virtual IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return Repository.Execute<T>(command, parameters);
        }

        public virtual T Add<T>(T entity) where T : class
        {
            return Repository.Add(entity);
        }

        public virtual T Remove<T>(T entity) where T : class
        {
            return Repository.Remove(entity);
        }

        public virtual T Remove<T, ID>(ID id) where T : class
        {
            return Repository.Remove<T, ID>(id);
        }

        public virtual T Update<T>(T entity) where T : class
        {
            return Repository.Update(entity);
        }

        public virtual long Count<T>() where T : class
        {
            return Repository.Count<T>();
        }

        public virtual long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Repository.Count(criteria);
        }

        public virtual long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Repository.Count(criteria);
        }

        public virtual IQueryable<T> All<T>() where T : class
        {
            return Repository.All<T>();
        }

        public virtual void Detach<T>(T entity) where T : class
        {
            Repository.Detach(entity);
        }

        public virtual void Attach<T>(T entity) where T : class
        {
            Repository.Attach(entity);
        }

        public virtual IDataContext DataContext
        {
            get { return Repository.DataContext; }
        }

        public virtual void Dispose()
        {
            Repository.Dispose();
        }

        public virtual ILoadService<T> Load<T>() where T : class
        {
            var provider = Repository as ILoadServiceProvider;
            if (provider != null)
            {
                return provider.Load<T>();
            }
            throw new InvalidOperationException();
        }

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            var provider = Repository as IMetaDataProvider;
            if (provider != null)
            {
                return provider.GetPrimaryKey<T>();
            }
            throw new InvalidOperationException();
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var provider = Repository as IMetaDataProvider;
            if (provider != null)
            {
                return provider.GetPrimaryKeyValue(entity);
            }
            throw new InvalidOperationException();
        }
    }
}
