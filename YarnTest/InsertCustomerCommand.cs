using Yarn;
using Yarn.Test.Models.EF;
using Yarn.Queries;

namespace YarnTest
{
    public class InsertCustomerCommand : ICommand
    {
        private readonly Customer _customer;

        public InsertCustomerCommand(Customer customer)
        {
            _customer = customer;
        }

        public void Execute(IRepository repository)
        {
            repository.Add(_customer);
            repository.DataContext.SaveChanges();
        }
    }
}
