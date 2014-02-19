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
        private Action _evictKeys;

        public DataContext(IDataContext context, Action evictKeys)
        {
            _context = context;
            _evictKeys = evictKeys;
        }

        public void SaveChanges()
        {
            if (_context != null)
            {
                _context.SaveChanges();
                if (_evictKeys != null)
                {
                    _evictKeys();
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
    }
}
