namespace Yarn.Queries
{
    public interface ICommand
    {
        void Execute(IRepository repository);
    }

    public interface ICommand<T> : ICommand
    {
        T Result { get; }
    }
}
