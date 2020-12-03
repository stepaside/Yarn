using System;
using System.Linq;
using System.Linq.Expressions;

namespace Yarn.Specification
{
    public class NotSpecification<T> : ISpecification<T>
    {
        private readonly Specification<T> _spec;

        public NotSpecification(Specification<T> specification)
        {
            _spec = specification ?? throw new ArgumentNullException(nameof(specification));
        }

        public bool IsSatisfiedBy(T item)
        {
            return !_spec.IsSatisfiedBy(item);
        }

        public virtual IQueryable<T> Apply(IQueryable<T> query)
        {
            return query.Where(_spec.Not().Predicate);
        }
    }

}
