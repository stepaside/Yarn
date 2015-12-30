using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IService<out T> : IDisposable
       where T : class
    {
        // LINQ methods
        IQueryable<T> All();
    }

    public interface ISimpleService<T, in ID> : IService<T>
       where T : class
    {
        T GetById(ID id);

        bool Save(T entity);
        void Remove(T entity);
        T Remove(ID id);
    }
}
