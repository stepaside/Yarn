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
        private IOdb _context = OdbCache.CurrentContext;

        public IOdb Session
        {
            get
            {
                if (_context == null)
                {
                    _context = OdbFactory.OpenInMemory();
                    OdbCache.CurrentContext = _context;
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

        public IDataContextCache DataContextCache
        {
            get { return OdbCache.Instance; }
        }
    }
}
