using NDatabase.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Yarn.Data.InMemoryProvider
{
    public class OdbCache : IDataContextCache
    {
        private const string CURRENT_DB_CONTEXT_KEY = "inmemory.dbcontext.current";
        private static IDataContextCache _instance;

        private OdbCache() { }

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
                 var dbContext = (IOdb)context.Items[CURRENT_DB_CONTEXT_KEY];
                 if (dbContext != null)
                 {
                     dbContext.Close();
                     dbContext.Dispose();
                     context.Items.Remove(CURRENT_DB_CONTEXT_KEY);
                 }
             }
        }

        static OdbCache()
        {
            _instance = new OdbCache();
            _instance.Initialize();
        }

        internal static IDataContextCache Instance
        {
            get
            {
                return _instance;
            }
        }

        public static IOdb CurrentContext
        {
            get
            {
                return (IOdb)_instance.Get();
            }
            set
            {
                _instance.Set(value);
            }
        }
    }
}
