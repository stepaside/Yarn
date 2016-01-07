using System;
using System.Collections.Generic;

namespace Yarn.EventSourcing
{
    public interface IAggregate
    {
        Guid Id { get; }
        int Version { get; }
        IEnumerable<object> GetUncommittedEvents();
        void ClearUncommittedEvents();
    }
}