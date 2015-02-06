using System.Runtime.Remoting.Messaging;
using System.Web;

namespace Yarn
{
    public class DataContextCache
    {
        private static readonly DataContextCache Instance;

        private DataContextCache() { }

        public void Initialize(string name) { }

        public object Get(string name)
        {
            var context = HttpContext.Current;
            return context != null ? context.Items[name] : CallContext.GetData(name);
        }

        public void Set(string name, object value)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                context.Items[name] = value;
            }
            else
            {
                CallContext.SetData(name, value);
            }
        }

        public void Cleanup(string name)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                context.Items.Remove(name);
            }
            else
            {
                CallContext.FreeNamedDataSlot(name);
            }
        }

        static DataContextCache()
        {
            Instance = new DataContextCache();
        }

        public static DataContextCache Current
        {
            get
            {
                return Instance;
            }
        }
    }
}
