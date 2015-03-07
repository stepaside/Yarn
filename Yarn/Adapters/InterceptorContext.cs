using System;
using System.Reflection;

namespace Yarn.Adapters
{
    public class InterceptorContext
    {
        private readonly Action _action;
        private readonly Func<object> _func;

        public InterceptorContext(Action action)
        {
            _action = action;
            _func = null;
        }

        public InterceptorContext(Func<object> func)
        {
            _func = func;
            _action = null;
        }

        public MethodBase Method { get; internal set; }
        public object[] Arguments { get; internal set; }
        public Exception Exception { get; set; }
        public Type ReturnType { get; internal set; }
        public object ReturnValue { get; internal set; }
        public bool Canceled { get; set; }

        public void Execute()
        {
            if (_action != null)
            {
                _action();
            }
            else
            {
                ReturnValue = _func();
            }
        }
    }
}