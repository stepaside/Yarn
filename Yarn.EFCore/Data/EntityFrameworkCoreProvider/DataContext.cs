using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    public class DataContext : DataContext<DbContext>
    {
        public DataContext(DbContext dbContext)
            : base(dbContext)
        { }
    }

    public class DataContext<T> : IDataContext<T>
        where T : DbContext
    {
        private readonly T _dbContext;

        public DataContext(T dbContext)
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            _dbContext = dbContext;
        }        

        public void SaveChanges()
        {
            if (!_dbContext.ChangeTracker.AutoDetectChangesEnabled)
            {
                _dbContext.ChangeTracker.DetectChanges();
            }
            _dbContext.SaveChanges();
        }

        public virtual T Session
        {
            get { return _dbContext; }
        }

        public string Source
        {
            get
            {
                return _dbContext.Database.GetDbConnection()?.ConnectionString;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _dbContext.Dispose();
        }
    }
}
