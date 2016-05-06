using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IEntityRepository<T, in TKey>
       where T : class
    {
        IQueryResult<T> GetAll();
        T GetById(TKey id);
        IQueryResult<T> Find(ISpecification<T> criteria);

        bool Save(T entity);
        void Remove(T entity);
        T Remove(TKey id);
    }
}
