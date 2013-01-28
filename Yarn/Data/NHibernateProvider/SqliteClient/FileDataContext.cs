using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class FileDataContext : SQLiteDataContext
    {
        public FileDataContext() : this(null) { }
        
        public FileDataContext(string contextKey = null) :
            base(SQLiteConfiguration.Standard.UsingFile(GetFileName(contextKey)).ShowSql(), contextKey)
        { }

        private static string GetFileName(string contextKey)
        {
            return (contextKey ?? "NHibernate.SqliteClient") + ".db";
        }
    }
}
