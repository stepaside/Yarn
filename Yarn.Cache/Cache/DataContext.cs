using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Cache
{
    public class DataContext : IDataContext
    {
        private IDataContext _context;
        private Action _handleCache;

        public DataContext(IDataContext context, Action handleCache)
        {
            _context = context;
            _handleCache = handleCache;
        }

        public void SaveChanges()
        {
            if (_context != null)
            {
                _context.SaveChanges();
                if (_handleCache != null)
                {
                    _handleCache();
                }
            }
        }

        public string Source
        {
            get { return _context.Source; }
        }

        public IDataContextCache DataContextCache
        {
            get { return _context.DataContextCache; }
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
                    _context.Dispose();
                    _context = null;
                }
            }
        }
    }
}
