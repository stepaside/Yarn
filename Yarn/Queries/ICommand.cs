using System.Threading.Tasks;

namespace Yarn.Queries
{
    public interface ICommand
    {
        void Execute(IRepository repository);
    }

    public interface ICommandAsync : ICommand
    {
        Task ExecuteAsync(IRepositoryAsync repository);
    }

    public interface ICommand<T> : ICommand
    {
        T Result { get; }
    }

    public interface ICommandAsync<T> : ICommand<T>, ICommandAsync
    {
    }
}
