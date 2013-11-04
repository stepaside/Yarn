using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class InMemoryDataContext : SQLiteDataContext
    {
        public InMemoryDataContext() : this(null) { }

        public InMemoryDataContext(string contextKey = null) :
            base(SQLiteConfiguration.Standard.InMemory().ShowSql(), contextKey)
        { }
    }
}
