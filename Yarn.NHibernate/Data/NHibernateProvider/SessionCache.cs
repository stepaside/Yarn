using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;
using Yarn;
using NHibernate;

namespace Yarn.Data.NHibernateProvider
{
    public class SessionCache : IDataContextCache
    {
        private const string CURRENT_SESSION_KEY = "nhibernate.session.current";
        private static IDataContextCache _instance;

        private SessionCache() { }

        void IDataContextCache.Initialize() { }

        object IDataContextCache.Get()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                return context.Items[CURRENT_SESSION_KEY];
            }
            else
            {
                return CallContext.GetData(CURRENT_SESSION_KEY);
            }
        }

        void IDataContextCache.Set(object value)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                context.Items[CURRENT_SESSION_KEY] = value;
            }
            else
            {
                CallContext.SetData(CURRENT_SESSION_KEY, value);
            }
        }

        void IDataContextCache.Cleanup()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                var session = (ISession)context.Items[CURRENT_SESSION_KEY];
                if (session != null)
                {
                    session.Close();
                    context.Items.Remove(CURRENT_SESSION_KEY);
                }
            }
        }

        static SessionCache()
        {
            _instance = new SessionCache();
            _instance.Initialize();
        }

        internal static IDataContextCache Instance
        {
            get
            {
                return _instance;
            }
        }

        public static ISession CurrentSession
        {
            get
            {
                return (ISession)_instance.Get();
            }
            set
            {
                _instance.Set(value);
            }
        }
    }
}
