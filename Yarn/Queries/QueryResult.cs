using System.Collections.Generic;
using System.Linq;

namespace Yarn.Queries
{
    public class QueryResult<T> : IQueryResult<T>
        where T : class
    {
        public QueryResult(IEnumerable<T> items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }

        public IEnumerable<T> Items { get; private set; }

        public int TotalCount { get; private set; }
    }
}