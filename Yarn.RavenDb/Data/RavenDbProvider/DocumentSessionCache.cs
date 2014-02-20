using System.Runtime.Remoting.Messaging;
using System.Web;
using Yarn;
using Raven.Client;

namespace Yarn.Data.RavenDbProvider
{
    public class DocumentSessionCache : IDataContextCache
    {
        private const string CURRENT_SESSION_KEY = "ravendb.session.current";
        private static IDataContextCache _instance;

        private DocumentSessionCache() { }

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
        { }

        static DocumentSessionCache()
        {
            _instance = new DocumentSessionCache();
            _instance.Initialize();
        }

        internal static IDataContextCache Instance
        {
            get
            {
                return _instance;
            }
        }

        public static IDocumentSession CurrentSession
        {
            get
            {
                return (IDocumentSession)_instance.Get();
            }
            set
            {
                _instance.Set(value);   
            }
        }
    }
}
