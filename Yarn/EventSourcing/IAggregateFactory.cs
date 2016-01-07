using System.Collections.Generic;

namespace Yarn.EventSourcing
{
    public interface IAggregateFactory
    {
        T Create<T>(IEnumerable<object> events) where T : class, IAggregate;
    }
}