using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IRelationNavigator
    {
        IFetchPath<T> Relations<T>() where T : class;
    }
}
