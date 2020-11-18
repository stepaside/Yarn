using System.Linq;
using Yarn;
using Yarn.Test.Models.EF;
using Yarn.Queries;
using Yarn.Specification;

namespace Yarn.Test
{
    public class GetCustomerByIdQuery : IQuery<Customer>
    {
        public GetCustomerByIdQuery(string id)
        {
            CustomerId = id;
        }

        public string CustomerId { get; set; }

        public IQueryResult<Customer> Execute(IRepository repository)
        {
            var query = new QueryBuilder<Customer>().Where(new Specification<Customer>(c => c.CustomerID == CustomerId)).Build(repository);
            return new QueryResult<Customer>(query.FirstOrDefault());
        }
    }
}
