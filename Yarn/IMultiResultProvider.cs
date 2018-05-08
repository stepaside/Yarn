using System;
using System.Linq.Expressions;

namespace Yarn
{
    public interface IMultiResultProvider<TAggregate>
        where TAggregate : class
    {
        TAggregate Execute(string command, ParamList parameters);
        IMultiResultProvider<TAggregate> Include<TProperty>(Expression<Func<TAggregate, TProperty>> path) where TProperty : class;
    }
}