using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Exsage.Core.Extensions;

namespace Exsage.Core.Specification
{
    public class Specification<T> : ISpecification<T>
    {
        public Specification(Expression<Func<T, bool>> predicate)
        {
            Predicate = predicate;
        }

        public Specification<T> And(Specification<T> specification)
        {
            return new Specification<T>(this.Predicate.And(specification.Predicate));
        }

        public Specification<T> And(Expression<Func<T, bool>> predicate)
        {
            return new Specification<T>(this.Predicate.And(predicate));
        }

        public Specification<T> Or(Specification<T> specification)
        {
            return new Specification<T>(this.Predicate.Or(specification.Predicate));
        }

        public Specification<T> Or(Expression<Func<T, bool>> predicate)
        {
            return new Specification<T>(this.Predicate.Or(predicate));
        }

        public bool IsSatisfiedBy(T item)
        {
            return Apply(new[] { item }.AsQueryable()).Any();
        }

        public IQueryable<T> Apply(IQueryable<T> query)
        {
            return query.Where(Predicate);
        }

        public Expression<Func<T, bool>> Predicate;
    }

}
