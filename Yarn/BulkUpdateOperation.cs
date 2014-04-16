using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public class BulkUpdateOperation<T>
    {
        public Expression<Func<T, bool>> Criteria { get; set; }
        public Expression<Func<T, T>> Update { get; set; }
    }
}
