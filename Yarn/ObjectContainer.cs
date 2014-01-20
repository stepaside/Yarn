using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Ioc;

namespace Yarn
{
    public static class ObjectContainer
    {
        private static IocContainer _container = new IocContainer(() => new ContainerLifetime());

        public static void Register<I, T>(object key = null)
            where I : class
            where T : class, I, new()
        {
            _container.RegisterInstance<I>(new T(), key);
            _container.Compile();
        }

        public static void Register<I, T>(T instance, object key = null)
            where I : class
            where T : class, I
        {
            _container.RegisterInstance<I>(instance, key);
            _container.Compile();
        }

        public static T Resolve<T>(object key = null)
            where T : class
        {
            T instance;
            if (key == null)
            {
                if (!_container.TryResolve<T>(out instance))
                {
                    instance = null;
                }
            }
            else
            {
                if (!_container.TryResolve<T>(key, out instance))
                {
                    instance = null;
                }
            }
            return instance;
        }
    }
}
