using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IService<T> : IDisposable
       where T : class
    {
        // Retrieve methods
        T GetById<ID>(ID id);
        IEnumerable<T> GetByIdList<ID>(IList<ID> ids);
        T Find(ISpecification<T> criteria);
        IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0);

        // DML methods
        T Add(T entity);
        T Remove(T entity);

        // LINQ methods
        IQueryable<T> All();
    }
}
