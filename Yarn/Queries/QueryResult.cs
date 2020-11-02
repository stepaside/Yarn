using System.Collections.Generic;
using System.Linq;

namespace Yarn.Queries
{
    public class QueryResult<T> : IQueryResult<T>
        where T : class
    {
        public QueryResult(IEnumerable<T> items, long totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }

        public IEnumerable<T> Items { get; private set; }

        public long TotalCount { get; private set; }
    }
}