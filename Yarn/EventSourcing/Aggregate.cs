using System;
using System.Collections.Generic;
using System.Linq;

namespace Yarn.EventSourcing
{
    public abstract class Aggregate : IAggregate
    {
        private readonly List<object> _uncommittedEvents;
        private readonly IEventRouter _router;

        protected Aggregate() 
            : this(null) 
        { }

        protected Aggregate(Guid id)
            : this(id, null)
        { }

        protected Aggregate(IEventRouter router)
            : this(Guid.NewGuid(), router)
        { }

        protected Aggregate(Guid id, IEventRouter router)
        {
            Id = id;
            Version = 0;
            _uncommittedEvents = new List<object>();
            _router = router ?? new ConventionEventRouter();
        }

        public Guid Id { get; }

        public int Version { get; private set; }

        public void Apply(object eventData)
        {
            _router.Invoke(this, eventData);            
        }
        
        public void Add(object eventData)
        {
            _uncommittedEvents.Add(eventData);
            Apply(eventData);            
            Version++;
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        public IEnumerable<object> GetUncommittedEvents()
        {
            return _uncommittedEvents.Select(e => e);
        }
    }
}