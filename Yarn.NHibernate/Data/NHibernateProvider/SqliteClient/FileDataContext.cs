using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class FileDataContext : SQLiteDataContext
    {
        public FileDataContext() : this(null, null, null, null) { }

        public FileDataContext(string assemblyNameOrLocation = null) : this(null, null, assemblyNameOrLocation, null) { }

        public FileDataContext(Assembly configurationAssembly = null) : this(null, null, null, configurationAssembly) { }

        public FileDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(null, nameOrConnectionString, assemblyNameOrLocation, null) { }

        public FileDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(null, nameOrConnectionString, null, configurationAssembly) { }

        public FileDataContext(string prefix = null, string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null) :
            base(SQLiteConfiguration.Standard.UsingFile(GetFileName(prefix)).ShowSql(), prefix, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
        
        private static string GetFileName(string prefix)
        {
            return (prefix ?? "NHibernate.SqliteClient") + ".db";
        }
    }
}
