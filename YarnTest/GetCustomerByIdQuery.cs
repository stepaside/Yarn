using System.Linq;
using Yarn;
using Yarn.Test.Models.EF;
using Yarn.Queries;
using Yarn.Specification;
using System;

namespace Yarn.Test
{
    public class CustomerByIdQuery : IQuery<Customer>
    {
        public string Id { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Id);
        }
    }

    public class CustomerByIdQueryHandler : IQueryHandler<CustomerByIdQuery, Customer>
    {
        private readonly IRepository _repository;

        public CustomerByIdQueryHandler(IRepository repository)
        {
            _repository = repository;
        }

        public IQueryResult<Customer> Handle(CustomerByIdQuery request)
        {
            if (!request.IsValid()) throw new ArgumentException("Invalid query specified");

            var query = new QueryBuilder<Customer>().Where(new Specification<Customer>(c => c.CustomerID == request.Id)).Build(_repository);
            return new QueryResult<Customer>(query.FirstOrDefault());
        }
    }
}
