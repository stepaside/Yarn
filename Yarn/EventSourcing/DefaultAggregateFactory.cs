using System;
using System.Collections.Generic;

namespace Yarn.EventSourcing
{
    public class DefaultAggregateFactory : IAggregateFactory
    {
        public virtual T Create<T>(IEnumerable<object> events) where T : class, IAggregate
        {
            var item = Activator.CreateInstance<T>();

            var aggregate = item as Aggregate;
            if (aggregate == null)
            {
                throw new InvalidOperationException(string.Format("Can't use default aggregate factory for type {0}", typeof(T).FullName));
            }

            foreach (var e in events)
            {
                aggregate.Apply(e);
            }

            return item;
        }
    }
}