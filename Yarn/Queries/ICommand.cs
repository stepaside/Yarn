using System.Threading.Tasks;

namespace Yarn.Queries
{
    public interface ICommand
    {
        bool IsValid();
    }

    public interface ICommand<out TResult>
    {
        bool IsValid();
    }
}
