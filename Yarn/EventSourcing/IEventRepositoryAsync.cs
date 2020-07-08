using System;
using System.Threading.Tasks;

namespace Yarn.EventSourcing
{
    public interface IEventRepositoryAsync : IEventRepository
    {
        Task<T> GetByIdAsync<T>(Guid id) where T : class, IAggregate;
        Task SaveAsync<T>(T aggregate) where T : class, IAggregate;
    }
}