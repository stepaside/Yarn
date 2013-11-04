using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn;
using NHibernate;

namespace Yarn.Data.NHibernateProvider.SqlClient
{
    public class SqlFullTextProvider : FullTextProvider
    {
        private string[] _fields = null;

        public SqlFullTextProvider()
        {
            _fields = new string[] { };
        }

        public SqlFullTextProvider(params string[] fields)
            : base()
        {
            _fields = fields;
        }

        public override IList<T> Search<T>(string searchTerms)
        {
            var queryText = new StringBuilder();
            queryText.AppendFormat("from {0} as t where contains(", typeof(T).Name);
            if (_fields == null)
            {
                queryText.Append("*");
            }
            else
            {
                queryText.Append("(t." + string.Join(",t.", _fields) + ")");
            }
            queryText.Append(", :terms)");
            var query = ((IDataContext<ISession>)this.DataContext).Session.CreateQuery(queryText.ToString());
            searchTerms = string.Join(" AND ", searchTerms.Split(' ').Select(t => "\"" + t + "*\"").ToArray());
            query.SetParameter("terms", searchTerms);
            return query.List<T>();
        }
    }
}
