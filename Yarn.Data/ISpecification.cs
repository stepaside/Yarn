using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T item);

        IQueryable<T> Apply(IQueryable<T> query);
    }
}
