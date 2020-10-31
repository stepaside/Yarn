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

        private Sorting<T> _previous;

        public Expression<Func<T, object>> OrderBy { get; }
        
        public string Path { get; }
        
        public bool Reverse { get; set; }

        public Sorting<T> Then(Sorting<T> next)
        {
            next._previous = this;
            return next;
        }

        public Sorting<T> Then(Expression<Func<T, object>> orderBy, bool reverse = false)
        {
            return Then(new Sorting<T>(orderBy, reverse));
        }

        public override string ToString()
        {
            var orderBy = $"{Path} {(Reverse ? "DESC" : "ASC")}"; 
            if (_previous != null)
            {
                orderBy = $"{_previous}, {orderBy}";
            }
            return orderBy;
        }

        public IQueryable<T> Apply(IQueryable<T> query)
        {
            var items = ToArray();
            var first = true;
            foreach(var item in items)
            {
                if (!first && query is IOrderedQueryable<T> ordered)
                {
                    query = item.Reverse ? ordered.ThenByDescending(item.OrderBy) : ordered.ThenBy(item.OrderBy);
                }
                else
                {
                    query = item.Reverse ? query.OrderByDescending(item.OrderBy) : query.OrderBy(item.OrderBy);
                    first = false;
                }
            }
            return query;
        }

        public Sorting<T>[] ToArray()
        {
            if (_previous == null)
            {
                return new Sorting<T>[] { this };
            }
            else
            {
                var stack = new Stack<Sorting<T>>();
                var current = this;
                while (current != null)
                {
                    stack.Push(current);
                    current = current._previous;
                }

                return stack.ToArray();
            }
        }
    }
}
