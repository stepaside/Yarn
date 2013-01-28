using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Yarn;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class Repository : IRepository
    {
        private IDataContext<DbContext> _context;
        protected readonly string _contextKey;

        public Repository() : this(null) { }

        public Repository(string contextKey = null) 
        {
            _contextKey = contextKey;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return this.Table<T>().Find(id);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.Table<T>().FirstOrDefault(criteria);
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }
        
        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.Table<T>().Where(criteria);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria) where T : class
        {
            return criteria.Apply(Table<T>());
        }

        public IList<T> Execute<T>(string command, params System.Tuple<string, object>[] parameters) where T : class
        {
            var connection = this.PrivateContext.Session.Database.Connection;

            var procedureName = command;
            var qualifier = string.Empty;

            // If the procedure is multipart
            if (procedureName.Contains('.'))
            {
                // separate qualified portion from the actual stored procedure name
                var parts = procedureName.Split('.');
                qualifier = string.Concat(string.Join(".", parts.Take(parts.Length - 1)), ".");
                procedureName = parts.Last();
            }

            // Check if the stored procedure name is escaped
            char? first = procedureName.First();
            char? last = procedureName.Last();
            if ((first == '`' && last == '`') || (first == '[' && last == ']') || (first == '\"' && last == '\"'))
            {
                procedureName = procedureName.Substring(1, command.Length - 1);
            }
            else
            {
                first = null;
                last = null;
            }

            var isEscaped = first != char.MinValue && last != char.MinValue;

            // If procedure name is not escaped make sure it is a valid name
            if (!isEscaped && (!char.IsLetter(command.First()) || procedureName.Any(c => !char.IsLetterOrDigit(c) && c != '_')))
            {
                throw new ArgumentException("procedure");
            }

            var commandText = string.Format("EXEC {0}{1}{2}{3}", qualifier, first, procedureName, last);
            var items = this.PrivateContext.Session.Database.SqlQuery<T>(commandText, parameters.Select(p => DbFactory.CreateParameter(connection, p.Item1, p.Item2)).ToArray());
            return items.ToArray();
        }

        public T Add<T>(T entity) where T : class
        {
            return this.Table<T>().Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            return this.Table<T>().Remove(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var result = this.GetById<T, ID>(id);
            if (result != null)
            {
                Remove<T>(result);
            }
            return result;
        }

        public T Merge<T>(T entity) where T : class
        {
            this.Table<T>().AddOrUpdate<T>(entity);
            return entity;
        }

        public void SaveChanges()
        {
            this.DataContext.SaveChanges();
        }

        public void Attach<T>(T entity) where T : class
        {
            this.Table<T>().Attach(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            ((IObjectContextAdapter)this.PrivateContext.Session).ObjectContext.Detach(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return this.Table<T>().AsQueryable<T>();
        }

        public long Count<T>() where T : class
        {
            return Table<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public DbSet<T> Table<T>() where T : class
        {
            return this.PrivateContext.Session.Set<T>();
        }

        private IDataContext<DbContext> PrivateContext
        {
            get
            {
                return (IDataContext<DbContext>)this.DataContext;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = ObjectFactory.Resolve<IDataContext<DbContext>>(_contextKey);
                }
                return _context;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Session.Dispose();
                    _context = null;
                }
            }
        }
    }
}
