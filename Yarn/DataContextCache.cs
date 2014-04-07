using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;

namespace Yarn
{
    public class DataContextCache
    {
        private static readonly DataContextCache _instance;

        private DataContextCache() { }

        public void Initialize(string name) { }

        public object Get(string name)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                return context.Items[name];
            }
            else
            {
                return CallContext.GetData(name);
            }
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
            _instance = new DataContextCache();
        }

        public static DataContextCache Current
        {
            get
            {
                return _instance;
            }
        }
    }
}
