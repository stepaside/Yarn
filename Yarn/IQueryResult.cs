using System.Collections.Generic;

namespace Yarn
{
    public interface IQueryResult<out T> 
        where T : class
    {
        IEnumerable<T> Items { get; }

        int TotalCount { get; }
    }
}