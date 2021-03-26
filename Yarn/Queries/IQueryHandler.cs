using System.Threading;
using System.Threading.Tasks;

namespace Yarn.Queries
{
    public interface IQueryHandler<in TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        IQueryResult<TResult> Handle(TQuery request);
    }

    public interface IQueryHandlerAsync<in TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        Task<IQueryResult<TResult>> HandleAsync(TQuery request, CancellationToken cancellationToken);
    }
}
