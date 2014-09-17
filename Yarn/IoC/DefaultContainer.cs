using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.IoC
{
    public class DefaultContainer : IContainer
    {
        private readonly Dictionary<Tuple<Type, string>, Func<object>> _mappings;

        public DefaultContainer()
        {
            _mappings = new Dictionary<Tuple<Type, string>, Func<object>>();
        }

        public void Register<TAbstract, TConcrete>(string instanceName = null)
            where TAbstract : class
            where TConcrete : class, TAbstract, new()
        {
            Func<TAbstract> createInstance = () => new TConcrete();
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

            var key = Tuple.Create(typeof(TAbstract), instanceName);

            if (_mappings.ContainsKey(key))
            {
                const string errorMessageFormat = "The requested mapping already exists - Instance Name: {0} ({1})";
                throw new InvalidOperationException(string.Format(errorMessageFormat, key.Item2 ?? "[null]", key.Item1.FullName));
            }

            _mappings.Add(key, createInstanceFactory as Func<object>);
        }

        public bool IsRegistered<TAbstract>(string instanceName = null)
            where TAbstract : class
        {
            var key = Tuple.Create(typeof(TAbstract), instanceName);
            return _mappings.ContainsKey(key);
        }

        public TAbstract Resolve<TAbstract>(string instanceName = null) 
            where TAbstract : class
        {
            var key = Tuple.Create(typeof(TAbstract), instanceName);
            Func<object> createInstance;

            if (_mappings.TryGetValue(key, out createInstance))
            {
                var instance = createInstance();
                return (TAbstract)instance;
            }

            const string errorMessageFormat = "Could not find mapping for type '{0}'";
            throw new InvalidOperationException(string.Format(errorMessageFormat, typeof(TAbstract).FullName));
        }

        public void Dispose()
        {
            _mappings.Clear();
        }
    }
}
