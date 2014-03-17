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
using YarnTest.Models.EF;

namespace YarnTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ObjectContainer.Current.Register<IRepository, Repository>(new Repository("Yarn.EF2"), "EF");
            
            var repo = ObjectContainer.Current.Resolve<IRepository>("EF");
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
                var session = ((IDataContext<DbContext>)dctx).Session;
            }

            var customer = repo.GetById<Customer, string>("ALFKI");

            var eager_customer = repo.As<ILoadServiceProvider>().Load<Customer>().Include(c => c.Orders).Include(c => c.Orders.Select(o => o.Order_Details)).Find(c => c.CustomerID == "ALFKI");

            var customers = repo.Execute<Customer>("EXEC spDTO_Customer_Retrieve @CustomerID", new ParamList { { "CustomerID", "ALFKI" } });

            var cachedRepo = repo.WithCache<LocalCache>();
            var customer1 = cachedRepo.GetById<Customer, string>("ALFKI");
            var customer2 = cachedRepo.GetById<Customer, string>("ALFKI");

            if (object.ReferenceEquals(customer1, customer2))
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
