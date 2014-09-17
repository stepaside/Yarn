using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn.IoC;

namespace Yarn
{
    public static class ObjectContainer
    {
        private static Lazy<IContainer> _container = new Lazy<IContainer>(() => new DefaultContainer(), true);

        public static void Initialize(Func<IContainer> containerFactory)
        {
            _container = new Lazy<IContainer>(containerFactory, true);
        }

        public static IContainer Current
        {
            get
            {
                return _container.Value;
            }
        }
    }
}
