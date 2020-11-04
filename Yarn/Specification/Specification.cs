using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Yarn.Extensions;

namespace Yarn.Specification
{
    public class Specification<T> : ISpecification<T>
    {
        public static readonly Specification<T> AlwaysTrue = new Specification<T>(t => true);

        public static readonly Specification<T> AlwaysFalse = new Specification<T>(t => false);

        public Specification(Expression<Func<T, bool>> predicate)
        {
            Predicate = predicate;
        }

        public Specification<T> And(Specification<T> specification)
        {
            return new Specification<T>(Predicate.And(specification.Predicate));
        }

        public Specification<T> And(Expression<Func<T, bool>> predicate)
        {
            return new Specification<T>(Predicate.And(predicate));
        }

        public Specification<T> Or(Specification<T> specification)
        {
            return new Specification<T>(Predicate.Or(specification.Predicate));
        }

        public Specification<T> Or(Expression<Func<T, bool>> predicate)
        {
            return new Specification<T>(Predicate.Or(predicate));
        }

        public bool IsSatisfiedBy(T item)
        {
            return Apply(new[] { item }.AsQueryable()).Any();
        }

        public virtual IQueryable<T> Apply(IQueryable<T> query)
        {
            return query.Where(Predicate);
        }

        public Expression<Func<T, bool>> Predicate;
    }

}
