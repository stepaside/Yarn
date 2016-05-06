using System.Collections.Generic;

namespace Yarn.Queries
{
    public class QueryResult<T> : IQueryResult<T>
        where T : class
    {
        private readonly IEnumerable<T> _items;
        private readonly int _totalCount;

        public QueryResult(IEnumerable<T> items, int totalCount)
        {
            _items = items;
            _totalCount = totalCount;
        }

        public IEnumerable<T> Items { get { return _items; } }

        public int TotalCount { get { return _totalCount; } }
    }
}