using System;
using System.Linq;

namespace Yarn.Queries
{
    public static class QueryExtensions
    {
        public static IQueryResult<TResult> Execute<T, TResult>(this IQuery<T> query, IRepository repository, Func<T, TResult> translate) where TResult : class
        {
            var result = query.Execute(repository);
            return new QueryResult<TResult>(result.Items.Select(translate), result.TotalCount);
        }
    }
}
