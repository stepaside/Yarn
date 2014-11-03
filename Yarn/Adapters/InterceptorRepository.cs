using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Yarn.Adapters
{
    public class InterceptorRepository : RepositoryAdapter
    {
        private readonly Func<IDisposable> _interceptorFactory;

        public InterceptorRepository(IRepository repository, Func<IDisposable> interceptorFactory)
            : base(repository)
        {
            if (_interceptorFactory == null)
            {
                throw new ArgumentNullException("interceptorFactory");
            }
            _interceptorFactory = interceptorFactory;
        }

        public override T GetById<T, ID>(ID id)
        {
            using (_interceptorFactory())
            {
                return base.GetById<T, ID>(id);
            }
        }
        
        public override T Find<T>(ISpecification<T> criteria)
        {
            using (_interceptorFactory())
            {
                return base.Find(criteria);
            }
        }

        public override T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria)
        {
            using (_interceptorFactory())
            {
                return base.Find(criteria);
            }
        }

        public override IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            using (_interceptorFactory())
            {
                return base.FindAll(criteria, offset, limit, orderBy);
            }
        }

        public override IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            using (_interceptorFactory())
            {
                return base.FindAll(criteria, offset, limit, orderBy);
            }
        }

        public override IList<T> Execute<T>(string command, ParamList parameters)
        {
            using (_interceptorFactory())
            {
                return base.Execute<T>(command, parameters);
            }
        }

        public override T Add<T>(T entity)
        {
            using (_interceptorFactory())
            {
                return base.Add(entity);
            }
        }

        public override T Remove<T>(T entity)
        {
            using (_interceptorFactory())
            {
                return base.Remove(entity);
            }
        }

        public override T Remove<T, ID>(ID id)
        {
            using (_interceptorFactory())
            {
                return base.Remove<T, ID>(id);
            }
        }

        public override T Update<T>(T entity)
        {
            using (_interceptorFactory())
            {
                return base.Update(entity);
            }
        }

        public override IQueryable<T> All<T>()
        {
            using (_interceptorFactory())
            {
                return base.All<T>();
            }
        }

        public override void Detach<T>(T entity)
        {
            using (_interceptorFactory())
            {
                base.Detach(entity);
            }
        }

        public override void Attach<T>(T entity)
        {
            using (_interceptorFactory())
            {
                base.Attach(entity);
            }
        }

        public override ILoadService<T> Load<T>()
        {
            using (_interceptorFactory())
            {
                return new LoadService<T>(base.Load<T>(), _interceptorFactory);
            }
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly ILoadService<T> _service;
            private readonly Func<IDisposable> _interceptorFactory;

            public LoadService(ILoadService<T> service, Func<IDisposable> interceptorFactory)
            {
                _service = service;
                _interceptorFactory = interceptorFactory;
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                using (_interceptorFactory())
                {
                    _service.Include(path);
                    return this;
                }
            }

            public T Update(T entity)
            {
                using (_interceptorFactory())
                {
                    return _service.Update(entity);
                }
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                using (_interceptorFactory())
                {
                    return _service.Find(criteria);
                }
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                using (_interceptorFactory())
                {
                    return _service.FindAll(criteria, offset, limit, orderBy);
                }
            }

            public T Find(ISpecification<T> criteria)
            {
                using (_interceptorFactory())
                {
                    return _service.Find(criteria);
                }
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                using (_interceptorFactory())
                {
                    return _service.FindAll(criteria, offset, limit, orderBy);
                }
            }

            public IQueryable<T> All()
            {
                using (_interceptorFactory())
                {
                    return _service.All();
                }
            }

            public void Dispose()
            {
                using (_interceptorFactory())
                {
                    _service.Dispose();
                }
            }
        }
    }
}
