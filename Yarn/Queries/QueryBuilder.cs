using System;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Queries
{
    public class QueryBuilder<T>
        where T : class
    {
        private Expression<Func<T, object>> _include;
        private ISpecification<T> _query;
        private Sorting<T> _orderBy;
        private int _pageNumber;
        private int _pageSize;

        public QueryBuilder<T> Include(Expression<Func<T, object>> include)
        {
            _include = include;
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
            if (_include != null)
            {
                var loadService = repository.As<ILoadService<T>>().Include(_include);
                query = loadService.All();
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
