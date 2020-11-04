using System.Collections.Generic;
using System.Linq;

namespace Yarn.Queries
{
    public class QueryResult<T> : IQueryResult<T>
        where T : class
    {
        public QueryResult(T item)
        {
            Items = new[] { item };
            TotalCount = 1;
        }

        public QueryResult(IEnumerable<T> items)
        {
            Items = items;
            TotalCount = -1;
        }

        public QueryResult(IEnumerable<T> items, long totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }

        public T Item
        {
            get
            {
                return Items?.FirstOrDefault();
            }
        }

        public IEnumerable<T> Items { get; }

        public long TotalCount { get; }
    }
}