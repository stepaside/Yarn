using System;
using StructureMap;

namespace Yarn.IoC.StructureMap
{
    public class StructureMapContainer : IContainerAdapter<Container>
    {
        private readonly Container _container;

        public StructureMapContainer()
        {
            _container = new Container();
            _container.Name = "YarnContainer-" + _container.Name;
        }

        public bool IsRegistered<TAbstract>(string instanceName = null) where TAbstract : class
        {
            return instanceName == null ? _container.TryGetInstance<TAbstract>() != null : _container.GetInstance<TAbstract>(instanceName) != null;
        }

        public void Register<TAbstract>(Func<TAbstract> createInstanceFactory, string instanceName = null) where TAbstract : class
        {
            if (instanceName == null)
            {
                _container.Configure(c => c.For<TAbstract>().Use(() => createInstanceFactory()));
            }
            else
            {
                _container.Configure(c => c.For<TAbstract>().Use(() => createInstanceFactory()).Named(instanceName));
            }
        }

        public void Register<TAbstract, TConcrete>(TConcrete instance, string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            if (instanceName == null)
            {
                _container.Configure(c => c.For<TAbstract>().Use(instance));
            }
            else
            {
                _container.Configure(c => c.For<TAbstract>().Use(instance).Named(instanceName));
            }
        }

        public void Register<TAbstract, TConcrete>(string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            if (instanceName == null)
            {
                _container.Configure(c => c.For<TAbstract>().Use<TConcrete>());
            }
            else
            {
                _container.Configure(c => c.For<TAbstract>().Use<TConcrete>().Named(instanceName));
            }
        }

        public TAbstract Resolve<TAbstract>(string instanceName = null) where TAbstract : class
        {
            return instanceName == null ? _container.GetInstance<TAbstract>() : _container.GetInstance<TAbstract>(instanceName);
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public Container Container
        {
            get { return _container; }
        }
    }
}
