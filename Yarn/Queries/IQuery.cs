using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Queries
{
    public interface IQuery<T>
    {
        IQueryResult<T> Execute(IRepository repository);
    }

    public interface IQueryAsync<T> : IQuery<T>
    {
        Task<IQueryResult<T>> ExecuteAsync(IRepositoryAsync repository);
    }
}
