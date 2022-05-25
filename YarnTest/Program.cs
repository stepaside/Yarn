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
using Yarn.Data.EntityFrameworkCoreProvider;
using Yarn.Data.EntityFrameworkProvider;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Test.Models.EF;
using Yarn.Test;
using System.Linq.Expressions;
using Nemo.Configuration;
using Nemo.Attributes.Converters;
using Nemo.Extensions;

namespace Yarn.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var nemoConfig = ConfigurationFactory.CloneCurrentConfiguration();
            nemoConfig.SetDefaultCacheRepresentation(Nemo.CacheRepresentation.None).SetAutoTypeCoercion(true);

            var repoNemo = new Yarn.Data.NemoProvider.Repository(new Yarn.Data.NemoProvider.RepositoryOptions { UseStoredProcedures = false, Configuration = nemoConfig }, new Yarn.Data.NemoProvider.DataContextOptions { ConnectionName = "Yarn.EF2.Connection" });

            var repodEf =  new Yarn.Data.EntityFrameworkProvider.Repository(new Yarn.Data.EntityFrameworkProvider.DataContextOptions { LazyLoadingEnabled = false, ProxyCreationEnabled = false, NameOrConnectionString = "Yarn.EF2.Connection", ConfigurationAssembly = typeof(Program).Assembly });

            var repodEf2 = new Yarn.Data.EntityFrameworkProvider.Repository(new Yarn.Data.EntityFrameworkProvider.DataContextOptions { LazyLoadingEnabled = false, ProxyCreationEnabled = false, DbContextType = typeof(NorthwindEntities) });

            var repoEfCore = new Yarn.Data.EntityFrameworkCoreProvider.Repository(new Yarn.Data.EntityFrameworkCoreProvider.DataContext(new NorthwindEntitiesCore()));

            var repoInMemory = new Yarn.Data.InMemoryProvider.Repository();

            var repo = repoNemo;
            //var retrieved = repo.GetById<Customer, string>("ALFKI");
            //var inserted = new Order { CustomerID = "ALFKI", ShipName = "Ship 1" };
            //repo.Add(inserted);

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
                //var context = dctx as IDataContext<Microsoft.EntityFrameworkCore.DbContext>;
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

            var query = new CustomerByIdQueryHandler(repo);
            var result = query.Handle(new CustomerByIdQuery { Id = "ANATR" });

            var customerRepo = new CustomerRepository(repo);
            var customer = customerRepo.GetById("ANTON");
        }
    }

    public class CustomerMap : Nemo.Configuration.Mapping.EntityMap<Customer>
    {
        public CustomerMap()
        {
            TableName = "Customers";
            Property(c => c.CustomerID).PrimaryKey();
            // Need a better way to auto-map nullable fields
            //Property(c => c.Address).WithTransform<DBNullableStringConverter>();
            //Property(c => c.City).WithTransform<DBNullableStringConverter>();
            //Property(c => c.ContactName).WithTransform<DBNullableStringConverter>();
            //Property(c => c.ContactTitle).WithTransform<DBNullableStringConverter>();
            //Property(c => c.Country).WithTransform<DBNullableStringConverter>();
            //Property(c => c.Fax).WithTransform<DBNullableStringConverter>();
            //Property(c => c.PostalCode).WithTransform<DBNullableStringConverter>();           
            //Property(c => c.Region).WithTransform<DBNullableStringConverter>();
        }
    }

    public class OrderMap : Nemo.Configuration.Mapping.EntityMap<Order>
    {
        public OrderMap()
        {
            TableName = "Orders";
            Property(o => o.OrderID).PrimaryKey().Generated();
            Property(o => o.CustomerID).References<Customer>();
            //Property(o => o.ShipRegion).WithTransform<DBNullableStringConverter>();
        }
    }

    public class OrderDetailMap : Nemo.Configuration.Mapping.EntityMap<Order_Detail>
    {
        public OrderDetailMap()
        {
            TableName = "Order Details";
            Property(o => o.OrderID).PrimaryKey();
            Property(o => o.ProductID).PrimaryKey();
            Property(o => o.OrderID).References<Order>();
        }
    }
}
