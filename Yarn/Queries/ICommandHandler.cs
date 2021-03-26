using System.Threading;
using System.Threading.Tasks;

namespace Yarn.Queries
{
    public interface ICommandHandler<TCommand>
         where TCommand : ICommand
    {
        void Handle(TCommand command);
    }

    public interface ICommandHandlerAsync<TCommand>
        where TCommand : ICommand
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken);
    }

    public interface ICommandHandler<in TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        TResult Handle(TCommand command);
    }

    public interface ICommandHandlerAsync<in TCommand, TResult>
       where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
    }
}
