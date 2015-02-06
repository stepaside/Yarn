using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn.Extensions;

namespace Yarn.Specification
{
    public class OrSpecification<T> : CompositeSpecification<T>
    {
        public OrSpecification(Specification<T> leftSide, Specification<T> rightSide)
            : base(leftSide, rightSide)
        { }
        
        public override IQueryable<T> Apply(IQueryable<T> query)
        {
            return query.Where(LeftSide.Predicate.Or(RightSide.Predicate));
        }
    }
}
