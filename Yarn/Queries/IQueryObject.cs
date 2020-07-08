namespace Yarn.Queries
{
    public interface IQueryObject<T>
    {
        ISpecification<T> Query();
    }
}
