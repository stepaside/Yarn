namespace Yarn.Queries
{
    public interface ICommand
    {
        void Execute();
    }

    public interface ICommand<T> : ICommand
    {
        T Result { get; }
    }
}
