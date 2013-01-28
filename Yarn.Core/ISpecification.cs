using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exsage.Core
{
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T item);

        IQueryable<T> Apply(IQueryable<T> query);
    }
}
