using System;
using System.Threading.Tasks;

namespace Yarn.EventSourcing
{
    public interface IEventRepository : IDisposable
    {
        T GetById<T>(Guid id) where T : class, IAggregate;
        void Save<T>(T aggregate) where T : class, IAggregate;
    }
}