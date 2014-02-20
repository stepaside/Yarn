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
        
        private bool _canInitialize = true;

        private DbContextCache() { }

        private void OnEndRequest(object sender, EventArgs args)
        {
            _instance.Cleanup();
        }

        void IDataContextCache.Initialize()
        {
            if (_canInitialize)
            {
                var context = HttpContext.Current;
                if (context != null && context.ApplicationInstance != null)
                {
                    context.ApplicationInstance.EndRequest += OnEndRequest;
                }
                _canInitialize = false;
            }
        }

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
                     dbContext.Database.Connection.Close();
                     context.Items.Remove(CURRENT_DB_CONTEXT_KEY);
                 }
             }
        }

        public void Reset()
        {
            if (!_canInitialize)
            {
                var context = HttpContext.Current;
                if (context != null && context.ApplicationInstance != null)
                {
                    context.ApplicationInstance.EndRequest -= OnEndRequest;
                }
                _canInitialize = true;
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

        public static DbContext CurrentContextAsync
        {
            get
            {
                ((DbContextCache)_instance).Reset();
                return (DbContext)_instance.Get();
            }
            set
            {
                ((DbContextCache)_instance).Reset();
                _instance.Set(value);
            }
        }
    }
}
