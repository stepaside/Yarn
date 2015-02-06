using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn.Specification
{
    public abstract class CompositeSpecification<T> : ISpecification<T>
    {
        protected readonly Specification<T> LeftSide;
        protected readonly Specification<T> RightSide;

        protected CompositeSpecification(Specification<T> leftSide, Specification<T> rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
        }

        public bool IsSatisfiedBy(T item)
        {
            return Apply(new[] { item }.AsQueryable()).Any();
        }

        public abstract IQueryable<T> Apply(IQueryable<T> query);
    }
}
