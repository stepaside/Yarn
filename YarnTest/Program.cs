using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarn;
using Yarn.Data.EntityFrameworkProvider;
using YarnTest.Models.EF;

namespace YarnTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ObjectFactory.Bind<IRepository, Repository>(new Repository("Yarn.EF2"), "EF");
            
            var repo = ObjectFactory.Resolve<IRepository>("EF");
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
            
            var customers = repo.Execute<Customer>("EXEC spDTO_Customer_Retrieve @CustomerID", new ParamList { { "CustomerID", "ALFKI" } });
        }
    }
}
