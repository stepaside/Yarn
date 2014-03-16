using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class FileDataContext : SQLiteDataContext
    {
        public FileDataContext() : this(null, null) { }

        public FileDataContext(string nameOrConnectionString = null) : this(null, nameOrConnectionString) { }

        public FileDataContext(string prefix = null, string nameOrConnectionString = null) :
            base(SQLiteConfiguration.Standard.UsingFile(GetFileName(prefix)).ShowSql(), prefix, nameOrConnectionString)
        { }
        
        private static string GetFileName(string prefix)
        {
            return (prefix ?? "NHibernate.SqliteClient") + ".db";
        }
    }
}
