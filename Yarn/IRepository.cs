using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Yarn
{
    public interface IRepository : IDisposable
    {
        // Retrieve methods
        T GetById<T, ID>(ID id) where T : class;
        T Find<T>(ISpecification<T> criteria) where T : class;
        T Find<T>(Expression<Func<T, bool>> criteria) where T : class;
        IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class;
        IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class;

        // Execute methods
        IList<T> Execute<T>(string command, ParamList parameters) where T : class;

        // DML methods
        T Add<T>(T entity) where T : class;
        T Remove<T>(T entity) where T : class;
        T Remove<T, ID>(ID id) where T : class;
        T Update<T>(T entity) where T : class;
                
        // Count methods
        long Count<T>() where T : class;
        long Count<T>(ISpecification<T> criteria) where T : class;
        long Count<T>(Expression<Func<T, bool>> criteria) where T : class;
        
        // LINQ methods
        IQueryable<T> All<T>() where T : class;

        // Unit of work methods
        void Detach<T>(T entity) where T : class;
        void Attach<T>(T entity) where T : class;

        IDataContext DataContext { get; }
    }
}
