using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Ioc;

namespace Yarn
{
    public static class ObjectContainer
    {
        private static Lazy<IContainerProvider> _container = new Lazy<IContainerProvider>(() => new DefaultContainerProvider(), true);

        public static void Initialize(Func<IContainerProvider> containerFactory)
        {
            _container = new Lazy<IContainerProvider>(containerFactory, true);
        }

        public static IContainerProvider Current
        {
            get
            {
                return _container.Value;
            }
        }
    }
}
