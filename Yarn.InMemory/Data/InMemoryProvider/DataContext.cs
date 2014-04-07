using NDatabase;
using NDatabase.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Data.InMemoryProvider
{
    public class DataContext : IDataContext<IOdb>
    {
        private const string DataContextKey = "NDatabase.DataContext";
        private IOdb _context = (IOdb)DataContextCache.Current.Get(DataContextKey);

        public IOdb Session
        {
            get
            {
                if (_context == null)
                {
                    _context = OdbFactory.OpenInMemory();
                    DataContextCache.Current.Set(DataContextKey, _context);
                }
                return _context;
            }
        }

        public void SaveChanges()
        {
            Session.Commit();
        }

        public void CancelChanges()
        {
            Session.Rollback();
        }

        public string Source
        {
            get { return "NDatabase"; }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    DataContextCache.Current.Cleanup(DataContextKey);
                    _context.Dispose();
                    _context = null;
                }
            }
        }
    }
}
