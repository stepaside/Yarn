using System.Collections;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarn;
using Yarn.Cache;
using Yarn.Data.EntityFrameworkProvider;
using Yarn.Extensions;
using Yarn.Reflection;
using YarnTest.Models.EF;
using Yarn.Queries;
using Yarn.Specification;

namespace YarnTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ObjectContainer.Current.Register<IRepository>(() => new Repository(false, false, nameOrConnectionString: "Yarn.EF2.Connection"), "EF");

            ObjectContainer.Current.Register<IRepository>(() => new Repository(false, false, dbContextType: typeof(NorthwindEntities)), "EF2");

            ObjectContainer.Current.Register<IRepository>(() => new Yarn.Data.InMemoryProvider.Repository(), "InMemory");

            var repo = ObjectContainer.Current.Resolve<IRepository>("EF2");
            if (repo == null)
            {
                Console.WriteLine("RepoNull");
            }
            var dctx = repo.DataContext;
            if (dctx == null)
            {
                Console.WriteLine("UoWNull");
            }
            else
            {
                var context = dctx as IDataContext<DbContext>;
                if (context != null)
                {
                    var session = context.Session;
                    var tableName = session.GetTableName<Customer>();
                }
                //repo.As<IBulkOperationsProvider>().Update<Customer>(c => c.CustomerID.Length > 12, c => new Customer { ContactName = c.ContactName + " 2" });
                //repo.As<IBulkOperationsProvider>().Delete<Customer>(c => c.CustomerID.Length > 12, c => c.CustomerID.StartsWith("AL"));
            }

            //var customer = repo.GetById<Customer, string>("ALFKI");

            Customer eagerCustomer = null;
            var loader = repo.As<ILoadServiceProvider>();
            if (loader != null)
            {
                eagerCustomer = loader.Load<Customer>().Include(c => c.Orders).Include(c => c.Orders.Select(o => o.Order_Details)).Find(c => c.CustomerID == "ALFKI");
            }

            var customersFromLondon = repo.FindAll<Customer>(c => c.City == "London", offset: 1).ToList();

            var customers = repo.Execute<Customer>("EXEC spDTO_Customer_Retrieve @CustomerID", new ParamList { { "CustomerID", "ALFKI" } });

            var cachedRepo = repo.WithCache<LocalCache>();
            var id = "ALFKI";
            var customer1 = cachedRepo.Find<Customer>(c => c.CustomerID == id);
            var customer2 = cachedRepo.Find<Customer>(c => c.CustomerID == id);

            if (ReferenceEquals(customer1, customer2))
            {
                Console.WriteLine("From Cache");
            }
            else
            {
                Console.WriteLine("Not From Cache");
            }
        }
    }

    public interface ICustomerRepository : IEntityRepository<Customer, string>
    {
        IQueryResult<Order> GetOrders(string id);
    }

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
