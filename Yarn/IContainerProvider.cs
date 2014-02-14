using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IContainerProvider
    {
        void Register<I, T>(object key = null) 
            where I : class 
            where T : class, I, new();

        void Register<I, T>(T instance, object key = null)
            where I : class
            where T : class, I;

        T Resolve<T>(object key = null)
           where T : class;
    }
}
