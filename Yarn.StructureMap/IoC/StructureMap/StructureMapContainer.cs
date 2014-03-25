using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.IoC.StructureMap
{
    public class StructureMapContainer : Yarn.Ioc.IContainer
    {
        public bool IsRegistered<TAbstract>(string instanceName = null) where TAbstract : class
        {
            if (instanceName == null)
            {
                return ObjectFactory.Container.TryGetInstance<TAbstract>() != null;
            }
            else
            {
                return ObjectFactory.Container.GetInstance<TAbstract>(instanceName) != null;
            }
        }

        public void Register<TAbstract>(Func<TAbstract> createInstanceFactory, string instanceName = null) where TAbstract : class
        {
            if (instanceName == null)
            {
                ObjectFactory.Container.Configure(c => c.For<TAbstract>().Use(createInstanceFactory));
            }
            else
            {
                ObjectFactory.Container.Configure(c => c.For<TAbstract>().Use(createInstanceFactory).Named(instanceName));
            }
        }

        public void Register<TAbstract, TConcrete>(TConcrete instance, string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            if (instanceName == null)
            {
                ObjectFactory.Container.Configure(c => c.For<TAbstract>().Use(instance));
            }
            else
            {
                ObjectFactory.Container.Configure(c => c.For<TAbstract>().Use(instance).Named(instanceName));
            }
        }

        public void Register<TAbstract, TConcrete>(string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract, new()
        {
            if (instanceName == null)
            {
                ObjectFactory.Container.Configure(c => c.For<TAbstract>().Use<TConcrete>());
            }
            else
            {
                ObjectFactory.Container.Configure(c => c.For<TAbstract>().Use<TConcrete>().Named(instanceName));
            }
        }

        public TAbstract Resolve<TAbstract>(string instanceName = null) where TAbstract : class
        {
            if (instanceName == null)
            {
                return ObjectFactory.Container.GetInstance<TAbstract>();
            }
            else
            {
                return ObjectFactory.Container.GetInstance<TAbstract>(instanceName);
            }
        }
    }
}
