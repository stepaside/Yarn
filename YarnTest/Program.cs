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

namespace YarnTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ObjectContainer.Current.Register<IRepository>(() => new Repository("Yarn.EF2", false, false), "EF");

            ObjectContainer.Current.Register<IRepository>(() => new Yarn.Data.InMemoryProvider.Repository(), "InMemory");
            
            var repo = ObjectContainer.Current.Resolve<IRepository>("InMemory");
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
            
            var loader = repo.As<ILoadServiceProvider>();
            if (loader != null)
            {
                var eager_customer = loader.Load<Customer>().Include(c => c.Orders).Include(c => c.Orders.Select(o => o.Order_Details)).Find(c => c.CustomerID == "ALFKI");
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
}
