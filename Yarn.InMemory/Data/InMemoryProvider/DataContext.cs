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
        private readonly IOdb _context;

        public DataContext()
        {
            _context = OdbFactory.OpenInMemory();
        }

        public IOdb Session
        {
            get
            {
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
                _context?.Dispose();
            }
        }
    }
}
