using Yarn;
using Yarn.Test.Models.EF;
using Yarn.Queries;
using Yarn.Specification;
using Yarn.Extensions;

namespace Yarn.Test
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IRepository _repo;

        public CustomerRepository(IRepository repo)
        {
            _repo = repo;
        }

        public IQueryResult<Customer> Find(ISpecification<Customer> criteria)
        {
            var customer = _repo.Find(criteria);
            return new QueryResult<Customer>(customer != null ? new[] { _repo.Find(criteria) } : new Customer[] { }, customer != null ? 1 : 0 );
        }

        public IQueryResult<Customer> GetAll()
        {
            return new QueryResult<Customer>(_repo.FindAll(new Specification<Customer>(c => true)), _repo.Count<Customer>());
        }

        public Customer GetById(string id)
        {
            return _repo.GetById<Customer, string>(id);
        }

        public IQueryResult<Order> GetOrders(string id)
        {
            return new QueryResult<Order>(_repo.FindAll<Order>(o => o.CustomerID == id), _repo.Count<Order>(o => o.CustomerID == id));
        }

        public void Remove(Customer entity)
        {
            _repo.Remove(entity);
        }

        public Customer Remove(string id)
        {
            return _repo.Remove<Customer, string>(id);
        }

        public bool Save(Customer entity)
        {
            return _repo.As<ILoadServiceProvider>().Load<Customer>().Update(entity) != null;
        }
    }
}
