using System;
using System.Collections.Generic;
using System.Linq;
using Lamar;
using Lamar.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace Yarn.IoC.StructureMap
{
    public class LamarContainer : IContainerAdapter<Container>
    {
        public LamarContainer(IServiceCollection services)
        {
            Container = new Container(services);
       }

        public bool IsRegistered<TAbstract>(string instanceName = null) where TAbstract : class
        {
            return instanceName == null ? Container.TryGetInstance<TAbstract>() != null : Container.TryGetInstance<TAbstract>(instanceName) != null;
        }

        public void Register<TAbstract>(Func<TAbstract> createInstanceFactory, string instanceName = null) where TAbstract : class
        {
            var registry = new ServiceRegistry();
            var item = registry.For<TAbstract>().Use(p => createInstanceFactory());
            if (instanceName != null)
            {
                item = item.Named(instanceName);
            }
            Container.Configure(registry);
        }

        public void Register<TAbstract, TConcrete>(TConcrete instance, string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            var registry = new ServiceRegistry();
            var item = registry.For<TAbstract>().Use(instance);
            if (instanceName != null)
            {
                item = item.Named(instanceName);
            }
            Container.Configure(registry);
        }

        public void Register<TAbstract, TConcrete>(string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            var registry = new ServiceRegistry();
            var item = registry.For<TAbstract>().Use<TConcrete>();
            if (instanceName != null)
            {
                item = item.Named(instanceName);
            }
            Container.Configure(registry);
        }

        public TAbstract Resolve<TAbstract>(string instanceName = null) where TAbstract : class
        {
            return instanceName == null ? Container.GetInstance<TAbstract>() : Container.GetInstance<TAbstract>(instanceName);
        }

        public IEnumerable<TAbstract> ResolveAll<TAbstract>() where TAbstract : class
        {
            return Container.GetAllInstances<TAbstract>();
        }

        public void Dispose()
        {
            Container.Dispose();
        }

        public object Resolve(Type serviceType, string instanceName = null)
        {
            return instanceName == null ? Container.GetInstance(serviceType) : Container.GetInstance(serviceType, instanceName);
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            return Container.GetAllInstances(serviceType).Cast<object>();
        }

        public Container Container { get; }
    }
}
