using System;
using System.Linq.Expressions;

namespace Yarn
{
    public interface IEntityLoadService<T> : IDisposable
        where T : class
    {
        IEntityLoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class;
    }
}