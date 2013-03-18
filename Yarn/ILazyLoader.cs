using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface ILazyLoader
    {
        IQueryable<TRoot> Include<TRoot, TRelated>(params Expression<Func<TRoot, TRelated>>[] selectors) where TRoot : class where TRelated : class;
    }
}
