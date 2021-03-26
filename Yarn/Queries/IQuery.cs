using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Queries
{
    public interface IQuery<out TResult>
    {
        bool IsValid();
    }
}
