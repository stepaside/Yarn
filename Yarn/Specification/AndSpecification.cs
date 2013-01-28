using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn.Extensions;

namespace Yarn.Specification
{
    public class AndSpecification<T> : CompositeSpecification<T>
    {
        public AndSpecification(Specification<T> leftSide, Specification<T> rightSide)
            : base(leftSide, rightSide)
        { }

        public override IQueryable<T> Apply(IQueryable<T> query)
        {
            return query.Where(_leftSide.Predicate.And(_rightSide.Predicate));
        }
    }

}
