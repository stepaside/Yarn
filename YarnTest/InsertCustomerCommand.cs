using Yarn;
using Yarn.Test.Models.EF;
using Yarn.Queries;
using System;

namespace YarnTest
{
    public class InsertCustomerCommand : ICommand
    {
        public InsertCustomerCommand(Customer customer)
        {
            Customer = customer;
        }

        public Customer Customer { get; }

        public bool IsValid()
        {
            return Customer != null && !string.IsNullOrEmpty(Customer.CustomerID);
        }
    }

    public class InsertCustomerCommandHandler : ICommandHandler<InsertCustomerCommand>
    {
        private readonly IRepository _repository;

        public InsertCustomerCommandHandler(IRepository repository)
        {
            _repository = repository;
        }

        public void Handle(InsertCustomerCommand command)
        {
            if (!command.IsValid()) throw new ArgumentException("Invalid command specified");

            _repository.Add(command.Customer);
            _repository.DataContext.SaveChanges();
        }
    }
}
