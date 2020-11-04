using Yarn;
using Yarn.Test.Models.EF;

namespace Yarn.Test
{
    public interface ICustomerRepository : IEntityRepository<Customer, string>
    {
        IQueryResult<Order> GetOrders(string id);
    }
}
