using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Adapters
{
    public class FailoverRepostiory : RepositoryAdapter
    {
        private readonly Action<Exception> _logger;
        private IRepository _current;
        private IRepository _other;
        
        public FailoverRepostiory(IRepository repository, IRepository otherRepository, Action<Exception> logger = null)
            : base(repository)
        {
            if (otherRepository == null)
            {
                throw new ArgumentNullException("otherRepository");
            }
            _other = otherRepository;
            _current = _repository;
            _logger = logger;
        }

        private void Failover()
        {
            var temp = _current;
            _current = _other;
            _other = _current;
        }

        public override T GetById<T, ID>(ID id)
        {
            try
            {
                return _current.GetById<T, ID>(id);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.GetById<T, ID>(id);
            }
        }

        public override T Find<T>(ISpecification<T> criteria)
        {
            try
            {
                return _current.Find(criteria);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.Find(criteria);
            }
        }

        public override T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria)
        {
            try
            {
                return _current.Find(criteria);
            }
            catch (Exception ex)
            {

                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.Find(criteria);
            }
        }

        public override IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            try
            {
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
        }

        public override IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, System.Linq.Expressions.Expression<Func<T, object>> orderBy = null)
        {
            try
            {
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
        }

        public override IList<T> Execute<T>(string command, ParamList parameters)
        {
            try
            {
                return _current.Execute<T>(command, parameters);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.Execute<T>(command, parameters);
            }
        }

        public override T Add<T>(T entity)
        {
            var result = _current.Add(entity);
            _other.Add(entity);
            return result;
        }

        public override T Remove<T>(T entity)
        {
            var result = _current.Remove(entity);
            _other.Remove(entity);
            return result;
        }

        public override T Remove<T, ID>(ID id)
        {
            var result = _current.Remove<T, ID>(id);
            _other.Remove<T, ID>(id);
            return result;
        }

        public override T Update<T>(T entity)
        {
            var result = _current.Update(entity);
            _other.Update(entity);
            return result;
        }
        
        public override IQueryable<T> All<T>()
        {
            try
            {
                return _current.All<T>();
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                Failover();
                return _current.All<T>();
            }
        }

        public override void Detach<T>(T entity)
        {
            _current.Detach(entity);
            _other.Detach(entity);
        }

        public override void Attach<T>(T entity)
        {
            _current.Attach(entity);
            _other.Attach(entity);
        }

        public IDataContext DataContext
        {
            get { return _current.DataContext; }
        }

        public void Dispose()
        {
            _current.Dispose();
            _other.Dispose();
        }

        public override ILoadService<T> Load<T>()
        {
            var provider = _current as ILoadServiceProvider;
            var otherProvider = _other as ILoadServiceProvider;
            if (provider != null || otherProvider != null)
            {
                return new LoadService<T>((provider ?? otherProvider).Load<T>(), provider != null ? otherProvider : null, _logger);
            }
            throw new InvalidOperationException();
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly ILoadService<T> _service;
            private readonly ILoadService<T> _failoverService;
            private readonly Action<Exception> _logger;

            public LoadService(ILoadService<T> service, ILoadServiceProvider failoverProvider, Action<Exception> logger)
            {
                _service = service;
                _failoverService = failoverProvider != null ? failoverProvider.Load<T>() : null;
                _logger = logger;
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                _service.Include(path);
                if (_failoverService != null)
                {
                    _failoverService.Include(path);
                }
                return this;
            }

            public T Update(T entity)
            {
                try
                {
                    return _service.Update(entity);
                }
                finally
                {
                    if (_failoverService != null)
                    {
                        _failoverService.Update(entity);
                    }
                }
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                try
                {
                    return _service.Find(criteria);
                }
                catch (Exception ex)
                {
                    if (_failoverService == null) throw;
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    return _failoverService.Find(criteria);
                }
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                try
                {
                    return _service.FindAll(criteria, offset, limit, orderBy);
                }
                catch (Exception ex)
                {
                    if (_failoverService == null) throw;
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    return _failoverService.FindAll(criteria, offset, limit, orderBy);
                }
            }

            public T Find(ISpecification<T> criteria)
            {
                try
                {
                    return _service.Find(criteria);
                }
                catch (Exception ex)
                {
                    if (_failoverService == null) throw;
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    return _failoverService.Find(criteria);
                }
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                try
                {
                    return _service.FindAll(criteria, offset, limit, orderBy);
                }
                catch (Exception ex)
                {
                    if (_failoverService == null) throw;
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    return _failoverService.FindAll(criteria, offset, limit, orderBy);
                }
            }

            public IQueryable<T> All()
            {
                try
                {
                    return _service.All();
                }
                catch (Exception ex)
                {
                     if (_failoverService == null) throw;
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    return _failoverService.All();
                }
            }

            public void Dispose()
            {
                _service.Dispose();
                if (_failoverService != null)
                {
                    _failoverService.Dispose();
                }
            }
        }
    }
}
