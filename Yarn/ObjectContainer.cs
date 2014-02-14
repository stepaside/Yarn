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

        public static void Register<I, T>(object key = null)
            where I : class
            where T : class, I, new()
        {
            _container.Value.Register<I, T>(key);
        }

        public static void Register<I, T>(T instance, object key = null)
            where I : class
            where T : class, I
        {
            _container.Value.Register<I, T>(instance, key);
        }

        public static T Resolve<T>(object key = null)
            where T : class
        {
            return _container.Value.Resolve<T>(key);
        }
    }
}
