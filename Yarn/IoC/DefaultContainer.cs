﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Yarn.IoC
{

    public class DefaultContainer : IContainer, INestedContainerProvider
    {
        private readonly Dictionary<(Type, string), Func<object>> _mappings;

        public DefaultContainer()
        {
            _mappings = new Dictionary<(Type, string), Func<object>>();
        }

        private DefaultContainer(IDictionary<(Type, string), Func<object>> mappings)
        {
            _mappings = new Dictionary<(Type, string), Func<object>>(mappings);
        }

        public void Register<TAbstract, TConcrete>(string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            Func<TAbstract> createInstance = Activator.CreateInstance<TConcrete>;
            Register(createInstance, instanceName);
        }

        public void Register<TAbstract, TConcrete>(TConcrete instance, string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract
        {
            Func<TAbstract> createInstance = () => instance;
            Register(createInstance, instanceName);
        }

        public void Register<TAbstract>(Func<TAbstract> createInstanceFactory, string instanceName = null) 
            where TAbstract : class
        {
            if (createInstanceFactory == null)
            {
                throw new ArgumentNullException("createInstanceFactory");
            }

            var key = (Type: typeof(TAbstract), InstanceName: instanceName);

            if (_mappings.ContainsKey(key))
            {
                throw new InvalidOperationException($"The requested mapping already exists - Instance Name: {key.InstanceName ?? "[null]"} ({key.Type.FullName})");
            }

            _mappings.Add(key, createInstanceFactory as Func<object>);
        }

        public bool IsRegistered<TAbstract>(string instanceName = null)
            where TAbstract : class
        {
            var key = (Type: typeof(TAbstract), InstanceName: instanceName);
            return _mappings.ContainsKey(key);
        }

        public TAbstract Resolve<TAbstract>(string instanceName = null) 
            where TAbstract : class
        {
            return (TAbstract)Resolve(typeof(TAbstract), instanceName);
        }

        public IEnumerable<TAbstract> ResolveAll<TAbstract>() where TAbstract : class
        {
            return _mappings.Where(kvp => kvp.Key.Item1 == typeof (TAbstract)).Select(kvp => kvp.Value()).OfType<TAbstract>();
        }

        public void Dispose()
        {
            _mappings.Clear();
        }

        public object Resolve(Type serviceType, string instanceName = null)
        {
            var key = (Type: serviceType, InstanceName: instanceName);
            
            if (_mappings.TryGetValue(key, out var createInstance))
            {
                var instance = createInstance();
                return instance;
            }

            const string errorMessageFormat = "Could not find mapping for type '{0}'";
            throw new InvalidOperationException(string.Format(errorMessageFormat, serviceType.FullName));
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            return _mappings.Where(kvp => kvp.Key.Item1 == serviceType).Select(kvp => kvp.Value());
        }

        public IContainer GetNestedContainer()
        {
            return new DefaultContainer(_mappings);
        }
    }
}
