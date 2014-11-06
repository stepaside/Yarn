using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
            Func<ID, T> f = base.GetById<T, ID>;
            return Intercept(() => f(id), f.Method, new object[] { id });
        }
        
        public override T Find<T>(ISpecification<T> criteria)
        {
            Func<ISpecification<T>, T> f = base.Find;
            return Intercept(() => f(criteria), f.Method, new object[] { criteria });
        }

        public override T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria)
        {
            Func<Expression<Func<T, bool>>, T> f = base.Find;
            return Intercept(() => base.Find(criteria), f.Method, new object[] { criteria });
        }

        public override IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            Func<ISpecification<T>, int, int, Expression<Func<T, object>>, IEnumerable<T>> f = base.FindAll;
            return Intercept(() => f(criteria, offset, limit, orderBy), f.Method, new object[] { criteria, offset, limit, orderBy });
        }

        public override IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            Func<Expression<Func<T, bool>>, int, int, Expression<Func<T, object>>, IEnumerable<T>> f = base.FindAll;
            return Intercept(() => f(criteria, offset, limit, orderBy), f.Method, new object[] { criteria, offset, limit, orderBy });
        }

        public override IList<T> Execute<T>(string command, ParamList parameters)
        {
            Func<string, ParamList,IList<T>> f = base.Execute<T>;
            return Intercept(() => f(command, parameters), f.Method, new object[] { command, parameters });
        }

        public override T Add<T>(T entity)
        {
            Func<T, T> f = base.Add;
            return Intercept(() => f(entity), f.Method, new object[] { entity });
        }

        public override T Remove<T>(T entity)
        {
            Func<T, T> f = base.Remove;
            return Intercept(() => f(entity), f.Method, new object[] { entity });
        }

        public override T Remove<T, ID>(ID id)
        {
            Func<ID, T> f = base.Remove<T, ID>;
            return Intercept(() => f(id), f.Method, new object[] { id });
        }

        public override T Update<T>(T entity)
        {
            Func<T, T> f = base.Update;
            return Intercept(() => f(entity), f.Method, new object[] { entity });
        }

        public override IQueryable<T> All<T>()
        {
            Func<IQueryable<T>> f = base.All<T>;
            return Intercept(f, f.Method, new object[] { });
        }

        public override void Detach<T>(T entity)
        {
            Action<T> f = base.Detach;
            InterceptNoResult(() => f(entity), f.Method, new object[] { entity });
        }

        public override void Attach<T>(T entity)
        {
            Action<T> f= base.Attach;
            InterceptNoResult(() => f(entity), f.Method, new object[] { entity });
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
                Func<T, T> f = _service.Update;
                return _repository.Intercept(() => f(entity), f.Method, new object[] { entity });
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                Func<Expression<Func<T, bool>>, T> f = _service.Find;
                return _repository.Intercept(() => f(criteria), f.Method, new object[] { criteria });
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                Func<Expression<Func<T, bool>>, int, int, Expression<Func<T, object>>, IEnumerable<T>> f = _service.FindAll;
                return _repository.Intercept(() => f(criteria, offset, limit, orderBy), f.Method, new object[] { criteria, offset, limit, orderBy });
            }

            public T Find(ISpecification<T> criteria)
            {
                Func<ISpecification<T>, T> f = _service.Find;
                return _repository.Intercept(() => f(criteria), f.Method, new object[] { criteria });
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                Func<ISpecification<T>, int, int, Expression<Func<T, object>>, IEnumerable<T>> f = _service.FindAll;
                return _repository.Intercept(() => f(criteria, offset, limit, orderBy), f.Method, new object[] { criteria, offset, limit, orderBy });
            }

            public IQueryable<T> All()
            {
                Func<IQueryable<T>> f = _service.All;
                return _repository.Intercept(f, f.Method, new object[] { });
            }

            public void Dispose()
            {
                _service.Dispose();
            }
        }

        private T Intercept<T>(Func<T> func, MethodBase method, object[] arguments)
        {
            var ctx = new InterceptorContext(() => (object)func()) { Method = method, Arguments = arguments, ReturnType = typeof(T) };
            using (_interceptorFactory(ctx))
            {
                if (ctx.Exception != null)
                {
                    ExceptionDispatchInfo.Capture(ctx.Exception).Throw();
                }

                if (!ctx.Canceled)
                {
                    return (T)ctx.ReturnValue;
                }
            }
            return default(T);
        }

        private void InterceptNoResult(Action action, MethodBase method, object[] arguments)
        {
            var ctx = new InterceptorContext(action) { Method = method, Arguments = arguments };
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
