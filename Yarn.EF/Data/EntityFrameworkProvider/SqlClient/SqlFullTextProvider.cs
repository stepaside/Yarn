using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Yarn;

namespace Yarn.Data.EntityFrameworkProvider.SqlClient
{
    public class SqlFullTextProvider : FullTextProvider
    {
        private string[] _fields = null;

        public SqlFullTextProvider(params string[] fields)
            : base()
        {
            _fields = fields;
        }

        public override IList<T> Search<T>(string searchTerms)
        {
            var dbContext = ((IDataContext<DbContext>)this.DataContext).Session;
            var objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            var tableName = objectContext.GetTableName<T>();

            var queryText = new StringBuilder();
            queryText.AppendFormat("select * from {0} as t where contains(", tableName);
            if (_fields == null)
            {
                queryText.Append("t.*");
            }
            else
            {
                queryText.Append("(t." + string.Join(",t.", _fields) + ")");
            }
            queryText.Append(", @terms)");
            searchTerms = string.Join(" AND ", searchTerms.Split(' ').Select(t => "\"" + t + "*\"").ToArray());

            var result = objectContext.ExecuteStoreQuery<T>(queryText.ToString(), new SqlParameter("terms", searchTerms));
            return result.ToList();
        }
    }
}
