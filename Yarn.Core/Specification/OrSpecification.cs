using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exsage.Core.Extensions;

namespace Exsage.Core.Specification
{
    public class OrSpecification<T> : CompositeSpecification<T>
    {
        public OrSpecification(Specification<T> leftSide, Specification<T> rightSide)
            : base(leftSide, rightSide)
        { }
        
        public override IQueryable<T> Apply(IQueryable<T> query)
        {
            return query.Where(_leftSide.Predicate.Or(_rightSide.Predicate));
        }
    }
}
