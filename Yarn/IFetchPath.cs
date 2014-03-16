using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IFetchPath<T>
        where T : class
    {
        IFetchPath<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class;
        IQueryable<T> Compile();
    }
}
