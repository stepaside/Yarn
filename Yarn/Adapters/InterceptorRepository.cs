using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;

namespace Yarn.Adapters
{
    public class InterceptorRepository : RepositoryAdapter
    {
        private readonly Func<InterceptorContext, IDisposable> _interceptorFactory;

        public InterceptorRepository(IRepository repository, Func<InterceptorContext, IDisposable> interceptorFactory)
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
            return Intercept(() => base.GetById<T, ID>(id), "GetById", new object[] { id });
        }
        
        public override T Find<T>(ISpecification<T> criteria)
        {
            return Intercept(() => base.Find(criteria), "Find", new object[] { criteria });
        }

        public override T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria)
        {
            return Intercept(() => base.Find(criteria), "Find", new object[] { criteria });
        }

        public override IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            return Intercept(() => base.FindAll(criteria, offset, limit, orderBy), "FindAll", new object[] { criteria, offset, limit, orderBy });
        }

        public override IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            return Intercept(() => base.FindAll(criteria, offset, limit, orderBy), "FindAll", new object[] { criteria, offset, limit, orderBy });
        }

        public override IList<T> Execute<T>(string command, ParamList parameters)
        {
            return Intercept(() => base.Execute<T>(command, parameters), "Execute", new object[] { command, parameters });
        }

        public override T Add<T>(T entity)
        {
            return Intercept(() => base.Add(entity), "Add", new object[] { entity });
        }

        public override T Remove<T>(T entity)
        {
            return Intercept(() => base.Remove(entity), "Remove", new object[] { entity });
        }

        public override T Remove<T, ID>(ID id)
        {
            return Intercept(() => base.Remove<T, ID>(id), "Remove", new object[] { id });
        }

        public override T Update<T>(T entity)
        {
            return Intercept(() => base.Update(entity), "Update", new object[] { entity });
        }

        public override IQueryable<T> All<T>()
        {
            return Intercept(() => base.All<T>(), "All", new object[] { });
        }

        public override void Detach<T>(T entity)
        {
            Action action = () => base.Detach(entity);
            InterceptNoResult<T>(action, "Detach", new object[] { entity });
        }

        public override void Attach<T>(T entity)
        {
            Action action = () => base.Attach(entity);
            InterceptNoResult<T>(action, "Attach", new object[] { entity });
        }

        public override ILoadService<T> Load<T>()
        {
            return new LoadService<T>(this, base.Load<T>(), _interceptorFactory);
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly InterceptorRepository _repository;
            private readonly ILoadService<T> _service;
            private readonly Func<InterceptorContext, IDisposable> _interceptorFactory;

            public LoadService(InterceptorRepository repository, ILoadService<T> service, Func<InterceptorContext, IDisposable> interceptorFactory)
            {
                _repository = repository;
                _service = service;
                _interceptorFactory = interceptorFactory;
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                _service.Include(path);
                return this;
            }

            public T Update(T entity)
            {
                return _repository.Intercept(() => _service.Update(entity), "Update", new object[] { entity });
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _repository.Intercept(() => _service.Find(criteria), "Find", new object[] { criteria });
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                return _repository.Intercept(() => _service.FindAll(criteria), "FindAll", new object[] { criteria, offset, limit, orderBy });
            }

            public T Find(ISpecification<T> criteria)
            {
                return _repository.Intercept(() => _service.Find(criteria), "Find", new object[] { criteria });
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                return _repository.Intercept(() => _service.FindAll(criteria), "FindAll", new object[] { criteria, offset, limit, orderBy });
            }

            public IQueryable<T> All()
            {
                return _repository.Intercept(() => _service.All(), "All", new object[] { });
            }

            public void Dispose()
            {
                _service.Dispose();
            }
        }

        private T Intercept<T>(Func<T> func, string method, object[] arguments)
        {
            var result = default(T);
            var ctx = new InterceptorContext { Action = () => result = func(), Method = method, Arguments = arguments };
            using (_interceptorFactory(ctx))
            {
                if (ctx.Exception != null)
                {
                    ExceptionDispatchInfo.Capture(ctx.Exception).Throw();
                }
                return result;
            }
        }

        private void InterceptNoResult<T>(Action action, string method, object[] arguments)
        {
            var ctx = new InterceptorContext { Action = action, Method = method, Arguments = arguments };
            using (_interceptorFactory(ctx))
            {
                if (ctx.Exception != null)
                {
                    ExceptionDispatchInfo.Capture(ctx.Exception).Throw();
                }
            }
        }
    }
}
