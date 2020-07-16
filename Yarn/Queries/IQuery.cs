using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Yarn.Queries
{
    public interface IQuery<T>
    {
        IQuery<T> Include(Expression<Func<T, object>> expression);
        IQuery<T> Page(int pageSize, int pageNumber);
        IQuery<T> Sort(Sorting<T> orderBy);
        IQuery<T> Where(ISpecification<T> query);

        IQueryResult<T> Execute();
        IQueryResult<TResult> Execute<TResult>(Func<T, TResult> translate) where TResult : class;
    }
}
