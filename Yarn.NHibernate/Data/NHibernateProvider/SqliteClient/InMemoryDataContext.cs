using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class InMemoryDataContext : SQLiteDataContext
    {
        public InMemoryDataContext() : this(null, null) { }

        public InMemoryDataContext(string nameOrConnectionString = null) : this(null, nameOrConnectionString) { }

        public InMemoryDataContext(string prefix = null, string nameOrConnectionString = null) :
            base(SQLiteConfiguration.Standard.InMemory().ShowSql(), prefix, nameOrConnectionString)
        { }
    }
}
