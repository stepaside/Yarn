using System;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Extensions;

namespace Yarn.Specification
{
    public class PagedSpecification<T> : Specification<T>
    {
        public PagedSpecification(Expression<Func<T, bool>> predicate) : base(predicate)
        {

        }

        public int PageSize { get; private set; }

        public int PageNumber { get; private set; }

        public Sorting<T> Sorting { get; private set; }

        public PagedSpecification<T> Page(int pageNumber, int pageSize)
        {
            if (pageNumber < 1 || pageNumber > 5000)
            {
                throw new ArgumentOutOfRangeException("pageNumber", "Page number must be between 1 and 5000.");
            }

            if (pageSize < 1 || pageNumber > 5000)
            {
                throw new ArgumentOutOfRangeException("pageSize", "Page size must be between 1 and 5000.");
            }

            PageSize = pageSize;
            PageNumber = pageNumber;
            return this;
        }

        public PagedSpecification<T> SortBy(string propertyName)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            ValidatePropertyName(propertyName);

            Sorting = new Sorting<T> { Path = propertyName };
            return this;
        }

        public PagedSpecification<T> SortByDescending(string propertyName)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            ValidatePropertyName(propertyName);

            Sorting = new Sorting<T> { Path = propertyName, Reverse = true };
            return this;
        }

        public PagedSpecification<T> SortBy(Expression<Func<T, object>> propertySelector)
        {
            if (propertySelector == null) throw new ArgumentNullException("propertySelector");

            Sorting = new Sorting<T> { OrderBy = propertySelector };
            return this;
        }
        
        public PagedSpecification<T> SortByDescending(Expression<Func<T, object>> propertySelector)
        {
            if (propertySelector == null) throw new ArgumentNullException("propertySelector");

            Sorting = new Sorting<T> { OrderBy = propertySelector, Reverse = true };
            return this;
        }

        protected virtual void ValidatePropertyName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (typeof(T).GetProperty(name) == null)
            {
                throw new ArgumentException(string.Format("'{0}' is not a public property of '{1}'.", name, typeof(T).FullName));
            }
        }

        public override IQueryable<T> Apply(IQueryable<T> query)
        {
            query = base.Apply(query);
            
            if (Sorting != null && Sorting.OrderBy != null)
            {
                query = Sorting.Reverse ? query.OrderByDescending(Sorting.OrderBy) : query.OrderBy(Sorting.OrderBy);
            }

            if (PageNumber >= 1)
            {
                query = query.Skip((PageNumber - 1) * PageSize).Take(PageSize);
            }

            return query;
        }
    }
}