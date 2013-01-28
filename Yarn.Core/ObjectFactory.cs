using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Ioc;

namespace Exsage.Core
{
    public static class ObjectFactory
    {
        private static IocContainer _container = new IocContainer(() => new ContainerLifetime());

        public static void Bind<I, T>(object key = null)
            where I : class
            where T : class, I, new()
        {
            _container.RegisterInstance<I>(new T(), key);
            _container.Compile();
        }

        public static void Bind<I, T>(T instance, object key = null)
            where I : class
            where T : class, I
        {
            _container.RegisterInstance<I>(instance, key);
            _container.Compile();
        }

        public static T Resolve<T>(object key = null)
            where T : class
        {
            if (key == null)
            {
                return _container.Resolve<T>();
            }
            else
            {
                return _container.Resolve<T>(key);
            }
        }
    }
}
