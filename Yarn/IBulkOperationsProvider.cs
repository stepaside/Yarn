using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IBulkOperationsProvider
    {
        IEnumerable<T> GetById<T, ID>(IEnumerable<ID> ids) where T : class;

        long Insert<T>(IEnumerable<T> entities) where T : class;

        long Update<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> update) where T : class;

        long Delete<T>(IEnumerable<T> entities) where T : class;

        long Delete<T, ID>(IEnumerable<ID> ids) where T : class;

        long Delete<T>(Expression<Func<T, bool>> criteria) where T : class;

        long Delete<T>(ISpecification<T> criteria) where T : class;
    }
}
