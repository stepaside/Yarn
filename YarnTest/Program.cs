﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarn;
using Yarn.Cache;
using Yarn.Data.EntityFrameworkProvider;
using YarnTest.Models.EF;

namespace YarnTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ObjectContainer.Register<IRepository, Repository>(new Repository("Yarn.EF2"), "EF");
            
            var repo = ObjectContainer.Resolve<IRepository>("EF");
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

            var cachedRepo = repo.UseCache<LocalCache>();
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
