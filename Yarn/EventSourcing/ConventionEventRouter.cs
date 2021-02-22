using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Yarn.EventSourcing
{
    public class ConventionEventRouter : IEventRouter
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Action<IAggregate, object>>> AllHandlers = new ConcurrentDictionary<Type, Dictionary<Type, Action<IAggregate, object>>>();

        private static Dictionary<Type, Action<IAggregate, object>> ScanAggregate(Type aggregateType)
        {
            var handleMethods = aggregateType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "Handle" && m.GetParameters().Length == 1 && m.ReturnParameter != null && m.ReturnParameter.ParameterType == typeof(void))
                .Select(m => new
                {
                    Method = m,
                    EventType = m.GetParameters().Single().ParameterType
                });
            var handlers = new Dictionary<Type, Action<IAggregate, object>>();
            foreach (var handle in handleMethods)
            {
                var handleMethod = handle.Method;
                handlers.Add(handle.EventType, (a, m) => handleMethod.Invoke(a, new[] { m }));
            }
            return handlers;
        }

        public void Invoke(IAggregate aggregate, object eventData)
        {
            var handlers = AllHandlers.GetOrAdd(aggregate.GetType(), type => ScanAggregate(type));

            if (handlers.TryGetValue(eventData.GetType(), out var handler))
            {
                handler(aggregate, eventData);
            }
        }
    }
}