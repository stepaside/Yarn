using System;
using System.Linq;
using System.Threading.Tasks;

namespace Yarn.Queries
{
    public static class QueryExtensions
    {
        public static IQueryResult<TResult> Execute<T, TResult>(this IQuery<T> query, IRepository repository, Func<T, TResult> translate) where TResult : class
        {
            var result = query.Execute(repository);
            return new QueryResult<TResult>(result.Items.Select(translate), result.TotalCount);
        }

        public static async Task<IQueryResult<TResult>> ExecuteAsync<T, TResult>(this IQueryAsync<T> query, IRepositoryAsync repository, Func<T, TResult> translate) where TResult : class
        {
            var result = await query.ExecuteAsync(repository).ConfigureAwait(false);
            return new QueryResult<TResult>(result.Items.Select(translate), result.TotalCount);
        }
    }
}
