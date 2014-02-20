using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;
using Yarn;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DbContextCache : IDataContextCache
    {
        private const string CURRENT_DB_CONTEXT_KEY = "ef.dbcontext.current";
        private static IDataContextCache _instance;
        
        private DbContextCache() { }

        void IDataContextCache.Initialize() { }

        object IDataContextCache.Get()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                return context.Items[CURRENT_DB_CONTEXT_KEY];
            }
            else
            {
                return CallContext.GetData(CURRENT_DB_CONTEXT_KEY);
            }
        }

        void IDataContextCache.Set(object value)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                context.Items[CURRENT_DB_CONTEXT_KEY] = value;
            }
            else
            {
                CallContext.SetData(CURRENT_DB_CONTEXT_KEY, value);
            }
        }

        void IDataContextCache.Cleanup()
        {
             var context = HttpContext.Current;
             if (context != null)
             {
                 var dbContext = (DbContext)context.Items[CURRENT_DB_CONTEXT_KEY];
                 if (dbContext != null)
                 {
                     dbContext.Dispose();
                     context.Items.Remove(CURRENT_DB_CONTEXT_KEY);
                 }
             }
        }

        static DbContextCache()
        {
            _instance = new DbContextCache();
            _instance.Initialize();
        }

        internal static IDataContextCache Instance
        {
            get
            {
                return _instance;
            }
        }

        public static DbContext CurrentContext
        {
            get
            {
                return (DbContext)_instance.Get();
            }
            set
            {
                _instance.Set(value);
            }
        }
    }
}
