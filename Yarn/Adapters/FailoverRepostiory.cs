using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Adapters
{
    public enum FailoverStrategy
    {
        ReplicationOnly, Failover, Failback
    }

    public class FailoverRepostiory : RepositoryAdapter
    {
        private readonly Action<Exception> _logger;
        private IRepository _current;
        private IRepository _other;
        private readonly FailoverStrategy _strategy;
        private bool _failedOver;
        private readonly bool _allowInconsistentReplication;

        public FailoverRepostiory(IRepository repository, IRepository otherRepository, Action<Exception> logger = null, FailoverStrategy strategy = FailoverStrategy.ReplicationOnly, bool allowInconsistentReplication = false)
            : base(repository)
        {
            if (otherRepository == null)
            {
                throw new ArgumentNullException("otherRepository");
            }
            _other = otherRepository;
            _current = Repository;
            _logger = logger;
            _strategy = strategy;
            _failedOver = false;
            _allowInconsistentReplication = allowInconsistentReplication;
        }

        private void Failover()
        {
            var temp = _current;
            _current = _other;
            _other = _current;
            _failedOver = !_failedOver;
        }

        private void Failback()
        {
            if (_failedOver)
            {
                Failover();
            }
        }

        public override T GetById<T, ID>(ID id)
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.GetById<T, ID>(id);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
                Failover();
                return _current.GetById<T, ID>(id);
            }
        }

        public override T Find<T>(ISpecification<T> criteria)
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.Find(criteria);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
                Failover();
                return _current.Find(criteria);
            }
        }

        public override T Find<T>(Expression<Func<T, bool>> criteria)
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.Find(criteria);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
                Failover();
                return _current.Find(criteria);
            }
        }

        public override IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
                Failover();
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
        }

        public override IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
                Failover();
                return _current.FindAll(criteria, offset, limit, orderBy);
            }
        }

        public override IList<T> Execute<T>(string command, ParamList parameters)
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.Execute<T>(command, parameters);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
                Failover();
                return _current.Execute<T>(command, parameters);
            }
        }

        public override T Add<T>(T entity)
        {
            var result = _current.Add(entity);
            if (!_allowInconsistentReplication)
            {
                _other.Add(entity);
            }
            else
            {
                try
                {
                    _other.Add(entity);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                }
            }
            return result;
        }

        public override T Remove<T>(T entity)
        {
            var result = _current.Remove(entity);
            if (!_allowInconsistentReplication)
            {
                _other.Remove(entity);
            }
            else
            {
                try
                {
                    _other.Remove(entity);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                }
            }
            return result;
        }

        public override T Remove<T, ID>(ID id)
        {
            var result = _current.Remove<T, ID>(id);
            if (!_allowInconsistentReplication)
            {
                _other.Remove<T, ID>(id);
            }
            else
            {
                try
                {
                    _other.Remove<T, ID>(id);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                }
            }
            return result;
        }

        public override T Update<T>(T entity)
        {
            var result = _current.Update(entity);
            if (!_allowInconsistentReplication)
            {
                _other.Update(entity);
            }
            else
            {
                try
                {
                    _other.Update(entity);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                }
            }
            return result;
        }

        public override IQueryable<T> All<T>()
        {
            try
            {
                if (_strategy == FailoverStrategy.Failback)
                {
                    Failback();
                }
                return _current.All<T>();
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger(ex);
                }
                if (_strategy == FailoverStrategy.ReplicationOnly) throw;
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

        public override IDataContext DataContext
        {
            get { return new FailoverDataContext(_current.DataContext, _other.DataContext); }
        }

        public override void Dispose()
        {
            DataContext.Dispose();
        }

        public override ILoadService<T> Load<T>()
        {
            var provider = _current as ILoadServiceProvider;
            var otherProvider = _other as ILoadServiceProvider;
            if (provider != null || otherProvider != null)
            {
                return new LoadService<T>((provider ?? otherProvider).Load<T>(), provider != null ? otherProvider : null, _logger, _strategy, _allowInconsistentReplication);
            }
            throw new InvalidOperationException();
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly ILoadService<T> _service;
            private readonly ILoadService<T> _failoverService;
            private readonly Action<Exception> _logger;
            private readonly FailoverStrategy _strategy;
            private readonly bool _allowInconsistentReplication;

            public LoadService(ILoadService<T> service, ILoadServiceProvider failoverProvider, Action<Exception> logger, FailoverStrategy strategy, bool allowInconsistentReplication)
            {
                _service = service;
                _failoverService = failoverProvider != null ? failoverProvider.Load<T>() : null;
                _logger = logger;
                _strategy = strategy;
                _allowInconsistentReplication = allowInconsistentReplication;
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
                var result = _service.Update(entity);
                if (_failoverService == null) return result;
                if (!_allowInconsistentReplication)
                {
                    _failoverService.Update(entity);
                }
                else
                {
                    try
                    {
                        _failoverService.Update(entity);
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger(ex);
                        }
                    }
                }
                return result;
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                try
                {
                    return _service.Find(criteria);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    if (_failoverService == null || _strategy == FailoverStrategy.ReplicationOnly) throw;
                    return _failoverService.Find(criteria);
                }
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                try
                {
                    return _service.FindAll(criteria, offset, limit, orderBy);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    if (_failoverService == null || _strategy == FailoverStrategy.ReplicationOnly) throw;
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
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    if (_failoverService == null || _strategy == FailoverStrategy.ReplicationOnly) throw;
                    return _failoverService.Find(criteria);
                }
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                try
                {
                    return _service.FindAll(criteria, offset, limit, orderBy);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    if (_failoverService == null || _strategy == FailoverStrategy.ReplicationOnly) throw;
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
                    if (_logger != null)
                    {
                        _logger(ex);
                    }
                    if (_failoverService == null || _strategy == FailoverStrategy.ReplicationOnly) throw;
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

        private class FailoverDataContext : IDataContext
        {
            private readonly IDataContext _current;
            private readonly IDataContext _other;

            public FailoverDataContext(IDataContext current, IDataContext other)
            {
                _current = current;
                _other = other;
            }

            public void SaveChanges()
            {
                _current.SaveChanges();
                _other.SaveChanges();
            }

            public string Source
            {
                get { return _current.Source; }
            }

            public void Dispose()
            {
                _current.Dispose();
                _other.Dispose();
            }
        }
    }
}
