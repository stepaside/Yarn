using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.IoC
{
    public interface INestedContainerProvider
    {
        IContainer GetNestedContainer();
    }
}
