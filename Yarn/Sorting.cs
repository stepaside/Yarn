using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;

namespace Yarn
{
    public class Sorting<T>
    {  
        public Sorting(Expression<Func<T, object>> orderBy, bool reverse = false)
        {
            OrderBy = orderBy;
            Path = orderBy?.Body?.ToString();
            Reverse = reverse;
        }

        public Sorting(string path, bool reverse = false)
        {
            OrderBy = path != null ? typeof(T).BuildLambdaExpression(path) as Expression<Func<T, object>> : null;
            Path = path;
            Reverse = reverse;
        }


        public Expression<Func<T, object>> OrderBy { get; }
        
        public string Path { get; }
        
        public bool Reverse { get; set; }

        public override string ToString()
        {
            return Path + " " + (Reverse ? "DESC" : "ASC");
        }
    }
}
