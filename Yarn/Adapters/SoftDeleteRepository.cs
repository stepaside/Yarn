using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using Yarn.Extensions;
using Yarn.Linq.Expressions;
using Yarn.Specification;

namespace Yarn.Adapters
{
    public class SoftDeleteRepository : RepositoryAdapter
    {
        private readonly IPrincipal _principal;

        public SoftDeleteRepository(IRepository repository, IPrincipal principal) 
            : base(repository)
        {
            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }
            _principal = principal;
        }

        public override T GetById<T, ID>(ID id)
        {
            var entity = base.GetById<T, ID>(id);
            if (entity is ISoftDelete && ((ISoftDelete)entity).IsDeleted)
            {
                return null;
            }
            return entity;
        }

        public override T Find<T>(ISpecification<T> criteria)
        {
            return Find(((Specification<T>)criteria).Predicate);
        }
        
        public override T Find<T>(Expression<Func<T, bool>> criteria)
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? base.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)).FirstOrDefault(criteria) : base.Find(criteria);
        }

        public override IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return base.FindAll(criteria, offset, limit, orderBy);
            }

            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            var spec = ((Specification<T>)criteria).And(CastRemoverVisitor<ISoftDelete>.Convert(filter));
            var query = base.All<T>().Where(spec.Predicate);
            return this.Page(query, offset, limit, orderBy);
        }

        public override IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return base.FindAll(criteria, offset, limit, orderBy);
            }

            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            var spec = new Specification<T>(CastRemoverVisitor<ISoftDelete>.Convert(filter)).And(criteria);
            var query = base.All<T>().Where(spec.Predicate);
            return this.Page(query, offset, limit, orderBy);
        }

        public override IList<T> Execute<T>(string command, ParamList parameters)
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? base.Execute<T>(command, parameters).Where(CastRemoverVisitor<ISoftDelete>.Convert(filter).Compile()).ToArray() : base.Execute<T>(command, parameters);
        }

        public override T Remove<T>(T entity)
        {
            var deleted = entity as ISoftDelete;
            if (deleted == null)
            {
                return base.Remove(entity);
            }
            deleted.IsDeleted = true;
            deleted.UpdateDate = DateTime.UtcNow;
            if (_principal != null)
            {
                deleted.UpdatedBy = _principal.Identity.Name;
            }
            return base.Update(entity);
        }

        public override T Remove<T, ID>(ID id)
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return base.Remove<T, ID>(id);
            }
            var entity = base.GetById<T, ID>(id);
            ((ISoftDelete)entity).IsDeleted = true;
            ((ISoftDelete)entity).UpdateDate = DateTime.UtcNow;
            if (_principal != null)
            {
                ((ISoftDelete)entity).UpdatedBy = _principal.Identity.Name;
            }
            return base.Update(entity);
        }

        public override long Count<T>()
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? base.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)).LongCount() : base.Count<T>();
        }

        public override long Count<T>(ISpecification<T> criteria)
        {
            return FindAll(criteria).AsQueryable().LongCount();
        }

        public override long Count<T>(Expression<Func<T, bool>> criteria)
        {
            return FindAll(criteria).AsQueryable().LongCount();
        }

        public override IQueryable<T> All<T>()
        {
            Expression<Func<T, bool>> filter = e => !((ISoftDelete)e).IsDeleted;
            return typeof(ISoftDelete).IsAssignableFrom(typeof(T)) ? base.All<T>().Where(CastRemoverVisitor<ISoftDelete>.Convert(filter)) : base.All<T>();
        }
    }
}
