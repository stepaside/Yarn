using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Queries
{
    public interface IQuery<T>
    {
        IQueryResult<T> Execute(IRepository repository);
    }
}
