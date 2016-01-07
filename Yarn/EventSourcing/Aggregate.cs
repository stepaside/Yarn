using System;
using System.Collections.Generic;
using System.Linq;

namespace Yarn.EventSourcing
{
    public abstract class Aggregate : IAggregate
    {
        private readonly Guid _id;
        private int _version;
        private readonly List<object> _uncommittedEvents;
        private readonly IEventRouter _router;

        protected Aggregate(IEventRouter router = null)
        {
            _id = Guid.NewGuid();
            _version = 0;
            _uncommittedEvents = new List<object>();
            _router = router ?? new ConventionEventRouter();
        }

        public Guid Id
        {
            get { return _id; }
        }

        public int Version
        {
            get { return _version; }
        }

        public void Apply(object eventData)
        {
            _router.Invoke(this, eventData);
            _version++;
        }
        
        public void Publish(object eventData)
        {
            Apply(eventData);
            _uncommittedEvents.Add(eventData);
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