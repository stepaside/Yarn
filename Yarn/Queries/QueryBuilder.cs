using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Queries
{
    public class QueryBuilder<T>
        where T : class
    {
        private List<Expression<Func<T, object>>> _includes;
        private ISpecification<T> _query;
        private Sorting<T> _orderBy;
        private int _pageNumber;
        private int _pageSize;

        public QueryBuilder<T> Include(params Expression<Func<T, object>>[] includes)
        {
            _includes = includes.Where(i => i != null).ToList();
            return this;
        }

        public QueryBuilder<T> Include(Expression<Func<T, object>> include)
        {
            if (include == null) throw new ArgumentNullException(nameof(include));

            if (_includes == null)
            {
                _includes = new List<Expression<Func<T, object>>>();
            }

            _includes.Add(include);
            return this;
        }

        public QueryBuilder<T> Page(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) throw new ArgumentException($"{nameof(pageNumber)} must be greater than 0");

            if (pageSize < 1) throw new ArgumentException($"{nameof(pageSize)} must be greater than 0");

            _pageNumber = pageNumber;
            _pageSize = pageSize;

            return this;
        }

        public QueryBuilder<T> Sort(Sorting<T> orderBy)
        {
            _orderBy = orderBy;
            return this;
        }

        public QueryBuilder<T> Where(ISpecification<T> query)
        {
            _query = query;
            return this;
        }

        public IQueryable<T> Build(IRepository repository)
        {
            IQueryable<T> query;
            if (_includes != null)
            {
                ILoadService<T> loadService = null;
                foreach (var include in _includes)
                {
                    if (loadService == null)
                    {
                        loadService = repository.As<ILoadService<T>>().Include(include);
                    }
                    else
                    {
                        loadService = loadService.Include(include);
                    }
                }

                if (loadService != null)
                {
                    query = loadService.All();
                }
                else
                {
                    query = repository.All<T>();
                }
            }
            else
            {
                query = repository.All<T>();
            }

            if (_query != null && _pageNumber != 0 && _pageSize != 0)
            {
                var paging = new PagedSpecification<T>((Specification<T>)_query).Page(_pageNumber, _pageSize);
                query = paging.Apply(query);
            }
            else if (_query != null)
            {
                query = _query.Apply(query);
            }
            else if (_pageNumber != 0 && _pageSize != 0)
            {
                var paging = new PagedSpecification<T>(Specification<T>.AlwaysTrue).Page(_pageNumber, _pageSize);
                query = paging.Apply(query);
            }

            if (_orderBy != null)
            {
                query = _orderBy.Apply(query);
            }

            return query;
        }
    }
}
