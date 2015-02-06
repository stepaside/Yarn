using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Linq.Expressions
{
    public sealed class CastRemoverVisitor<TInterface> : ExpressionVisitor
    {
        public static Expression<Func<T, bool>> Convert<T>(
            Expression<Func<T, bool>> predicate)
        {
            var visitor = new CastRemoverVisitor<TInterface>();

            var visitedExpression = visitor.Visit(predicate);

            return (Expression<Func<T, bool>>)visitedExpression;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert && node.Type == typeof(TInterface))
            {
                return node.Operand;
            }

            return base.VisitUnary(node);
        }
    }
}
