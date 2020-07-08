using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Yarn.IoC
{
    public class ServiceCollectionContainer : IContainer
    {
        private readonly IServiceCollection _services;
        private readonly Lazy<IServiceProvider> _provider;

        public ServiceCollectionContainer(IServiceCollection services)
        {
            _services = services;
            _provider = new Lazy<IServiceProvider>(() => _services.BuildServiceProvider());
        }

        public void Dispose()
        {
        }

        public bool IsRegistered<TAbstract>(string instanceName = null) where TAbstract : class
        {
            if (!string.IsNullOrEmpty(instanceName)) throw new NotSupportedException();

            return Resolve<TAbstract>(instanceName) != null;
        }

        public void Register<TAbstract>(Func<TAbstract> createInstanceFactory, string instanceName = null) where TAbstract : class
        {
            if (!string.IsNullOrEmpty(instanceName)) throw new NotSupportedException();

            _services.AddTransient(provider => createInstanceFactory());
        }

        public TAbstract Resolve<TAbstract>(string instanceName = null) where TAbstract : class
        {
            if (!string.IsNullOrEmpty(instanceName)) throw new NotSupportedException();

            return _provider.Value.GetService<TAbstract>();
        }

        public object Resolve(Type serviceType, string instanceName = null)
        {
            if (!string.IsNullOrEmpty(instanceName)) throw new NotSupportedException();

            return _provider.Value.GetService(serviceType);
        }

        public IEnumerable<TAbstract> ResolveAll<TAbstract>() where TAbstract : class
        {
            return _provider.Value.GetServices<TAbstract>();
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            return _provider.Value.GetServices(serviceType);
        }

        void IContainer.Register<TAbstract, TConcrete>(string instanceName)
        {
            if (!string.IsNullOrEmpty(instanceName)) throw new NotSupportedException();

            _services.AddTransient<TAbstract, TConcrete>();
        }

        void IContainer.Register<TAbstract, TConcrete>(TConcrete instance, string instanceName)
        {
            if (!string.IsNullOrEmpty(instanceName)) throw new NotSupportedException();

            _services.AddTransient<TAbstract, TConcrete>(p => instance);
        }
    }
}
