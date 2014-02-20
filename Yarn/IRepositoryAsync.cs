using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IRepositoryAsync : IRepository
    {
        // Retrieve methods
        Task<T> GetByIdAsync<T, ID>(ID id) where T : class;
        Task<IEnumerable<T>> GetByIdListAsync<T, ID>(IList<ID> ids) where T : class;
        Task<T> FindAsync<T>(ISpecification<T> criteria) where T : class;
        Task<T> Find<T>(Expression<Func<T, bool>> criteria) where T : class;
        Task<IEnumerable<T>> FindAllAsync<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class;
        Task<IEnumerable<T>> FindAllAsync<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class;

        // Execute methods
        Task<IList<T>> ExecuteAsync<T>(string command, ParamList parameters) where T : class;
        
        // Count methods
        Task<long> CountAsync<T>() where T : class;
        Task<long> CountAsync<T>(ISpecification<T> criteria) where T : class;
        Task<long> CountAsync<T>(Expression<Func<T, bool>> criteria) where T : class;

        new IDataContextAsync DataContext { get; }
    }
}
