using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Yarn.Queries
{
    public interface IQueryHandler<T>
        where T : class
    {
        IQueryHandler<T> Include(Expression<Func<T, object>> expression);
        IQueryHandler<T> Page(int pageSize, int pageNumber);
        IQueryHandler<T> Sort(Sorting<T> orderBy);

        IQueryResult<T> Handle(IQueryObject<T> query);
        IQueryResult<TResult> Handle<TResult>(IQueryObject<T> query, Func<T, TResult> transformer) where TResult : class;
    }
}
