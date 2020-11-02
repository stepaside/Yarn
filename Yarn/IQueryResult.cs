using System.Collections.Generic;

namespace Yarn
{
    public interface IQueryResult<T> 
    {
        IEnumerable<T> Items { get; }

        long TotalCount { get; }
    }
}