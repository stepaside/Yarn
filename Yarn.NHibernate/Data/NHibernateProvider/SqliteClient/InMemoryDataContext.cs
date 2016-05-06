using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class InMemoryDataContext : SqLiteDataContext
    {
        public InMemoryDataContext() : this(null, null, null, null) { }

        public InMemoryDataContext(string assemblyNameOrLocation = null) : this(null, null, assemblyNameOrLocation, null) { }

        public InMemoryDataContext(Assembly configurationAssembly = null) : this(null, null, null, configurationAssembly) { }

        public InMemoryDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(null, nameOrConnectionString, assemblyNameOrLocation, null) { }

        public InMemoryDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(null, nameOrConnectionString, null, configurationAssembly) { }

        public InMemoryDataContext(string prefix = null, string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null) :
            base(SQLiteConfiguration.Standard.InMemory().ShowSql(), prefix, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }
}
