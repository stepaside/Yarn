using System;
using System.Collections.Generic;
using System.Linq;
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
            return first.Compose(second, Expression.AndAlso);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        public static Expression<Func<T, bool>> BuildOrExpression<T, ID>(this Expression<Func<T, ID>> valueSelector, IList<ID> values)
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
            
            if (values.Count == 0)
            {
                return e => false;
            }

            var p = valueSelector.Parameters.Single();
            var equals = values.Select(value => (Expression)Expression.Equal(valueSelector.Body, Expression.Constant(value, typeof(ID))));
            var body = equals.Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<T, bool>>(body, p);
        }

        public static Expression<Func<T, bool>> BuildPrimaryKeyExpression<T>(this IMetaDataProvider repository, T entity)
            where T : class
        {
            var primaryKeyValue = repository.GetPrimaryKeyValue(entity);
            var primaryKey = repository.GetPrimaryKey<T>();

            var values = primaryKey.Zip(primaryKeyValue, Tuple.Create).ToArray();
            
            Expression<Func<T, bool>> predicate = null;
            foreach (var value in values)
            {
                var parameter = Expression.Parameter(typeof(T));
                var left = Expression.Convert(Expression.PropertyOrField(parameter, value.Item1), value.Item2.GetType());
                var body = Expression.Equal(left, Expression.Constant(value.Item2));
                predicate = predicate == null ? Expression.Lambda<Func<T, bool>>(body, parameter) : predicate.And(Expression.Lambda<Func<T, bool>>(body, parameter));
            }
            
            return (Expression<Func<T, bool>>)Evaluator.PartialEval(predicate);
        }

        public static Expression<Func<T, bool>> BuildPrimaryKeyExpression<T, ID>(this IMetaDataProvider repository, ID id)
            where T : class
        {
            var primaryKey = repository.GetPrimaryKey<T>().First();
            var parameter = Expression.Parameter(typeof(T));
            var left = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(ID));
            var body = Expression.Equal(left, Expression.Constant(id, typeof(ID)));
            var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);
            return (Expression<Func<T, bool>>)Evaluator.PartialEval(predicate);
        }

        public static LambdaExpression BuildLambdaExpression(this Type type, string path, bool covariantReturnType = true)
        {
            var properties = path.Split('.').Skip(1).ToList();

            var t = type;
            var parameter = Expression.Parameter(t);
            Expression expression = parameter;

            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                var start = property.IndexOf('[');
                var end = property.IndexOf(']');

                int index;
                if (start > -1 && int.TryParse(property.Substring(start + 1, end - start - 1), out index))
                {
                    property = property.Substring(0, start);
                    var temp = Expression.Property(expression, t, property);
                    expression = Expression.Call(temp, temp.Type.GetMethod("get_Item"), new Expression[] { Expression.Constant(index) });
                }
                else
                {
                    expression = Expression.Property(expression, t, property);
                }
                t = expression.Type;
            }

            var lambdaExpression = covariantReturnType ? Expression.Lambda(typeof(Func<,>).MakeGenericType(type, typeof(object)), expression, parameter) : Expression.Lambda(expression, parameter);
            return lambdaExpression;
        }
    }
}
