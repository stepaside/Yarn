using System.Collections.Concurrent;
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
            return CallContext.LogicalGetData(name);
        }

        public void Set(string name, object value)
        {
           CallContext.LogicalSetData(name, value);
        }

        public void Cleanup(string name)
        {
            CallContext.FreeNamedDataSlot(name);
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
