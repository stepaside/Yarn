using System.Collections;
using NHibernate;
using NHibernate.Dialect.Function;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.Type;

namespace Yarn.Data.NHibernateProvider.SqlClient.Dialects
{
    public class ContainsFunction : ISQLFunction
    {
        public bool HasArguments
        {
            get { return true; }
        }

        public bool HasParenthesesIfNoArguments
        {
            get { return true; }
        }

        public virtual SqlString Render(IList args, ISessionFactoryImplementor factory)
        {
            SqlStringBuilder builder = new SqlStringBuilder();
            builder.Add("contains(");

            if (args.Count == 1)
            {
                builder.Add("*, ");
                builder.AddObject(args[0]);
            }
            else
            {
                builder.AddObject(args[0]);
                builder.Add(", ");
                builder.AddObject(args[1]);
            }

            builder.Add(")");

            return builder.ToSqlString();
        }

        public virtual IType ReturnType(IType columnType, IMapping mapping)
        {
            return NHibernateUtil.Boolean;
        }
    }
}
