using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface ILoadService<T> : IService<T>
        where T : class
    {
        ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class;

        // Retrieve methods
        T Find(Expression<Func<T, bool>> criteria);
        IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0);
        T Find(ISpecification<T> criteria);
        IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0);
    }
}
