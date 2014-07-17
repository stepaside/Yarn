using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Yarn.Linq.Expressions;

namespace Yarn.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.Or);
        }

        public static Expression<Func<T, bool>> BuildOrExpression<T, ID>(this Expression<Func<T, ID>> valueSelector, IEnumerable<ID> values)
            where T : class
        {

            if (null == valueSelector)
            {
                throw new ArgumentNullException("valueSelector");
            }

            if (null == values)
            {
                throw new ArgumentNullException("values");
            }

            var p = valueSelector.Parameters.Single();

            if (!values.Any())
            {
                return e => false;
            }

            var equals = values.Select(value => (Expression)Expression.Equal(valueSelector.Body, Expression.Constant(value, typeof(ID))));
            var body = @equals.Aggregate(Expression.Or);
            return Expression.Lambda<Func<T, bool>>(body, p);
        }

        public static Expression<Func<T, bool>> BuildPrimaryKeyExpression<T>(this IMetaDataProvider repository, T entity)
            where T : class
        {
            var primaryKeyValue = repository.GetPrimaryKeyValue(entity).First();
            var primaryKey = repository.GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(object));
            var idSelector = Expression.Lambda<Func<T, object>>(body, parameter);
            var predicate = idSelector.BuildOrExpression(new[] { primaryKeyValue });
            return predicate;
        }

        public static Expression<Func<T, bool>> BuildPrimaryKeyExpression<T, ID>(this IMetaDataProvider repository, ID id)
            where T : class
        {
            var primaryKey = repository.GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(ID));
            var idSelector = Expression.Lambda<Func<T, ID>>(body, parameter);
            var predicate = idSelector.BuildOrExpression(new[] { id });
            return predicate;
        }
    }
}
