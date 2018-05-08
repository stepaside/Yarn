namespace Yarn
{
    public interface IEntityKeySelector<in T, out TKey>
        where T : class
    {
        TKey GetKey(T entity);
    }
}