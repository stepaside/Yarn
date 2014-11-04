using System;
using System.Reflection;

namespace Yarn.Adapters
{
    public class InterceptorContext
    {
        internal InterceptorContext()
        {
        }

        public MethodBase Method { get; internal set; }
        public object[] Arguments { get; internal set; }
        public Action Action { get; internal set; }
        public Exception Exception { get; set; }
        public bool Canceled { get; set; }
    }
}