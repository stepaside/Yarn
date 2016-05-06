using System.Threading.Tasks;

namespace Yarn
{
    public interface IEntityRepositoryAsync<T, in TKey> : IEntityRepository<T, TKey>
        where T : class
    {
        Task<IQueryResult<T>> GetAllAsync();
        Task<T> GetByIdAsync(TKey id);
        Task<IQueryResult<T>> FindAsync(ISpecification<T> criteria);
    }
}