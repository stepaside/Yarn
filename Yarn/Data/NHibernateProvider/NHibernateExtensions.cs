using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;

namespace Yarn.Data.NHibernateProvider
{
    public static class NHibernateExtensions
    {
        public static bool Delete<T, ID>(this ISession session, ID id)
        {
            var queryString = string.Format("delete {0} where id = :id",
                                            typeof(T));
            return session.CreateQuery(queryString)
                   .SetParameter("id", id)
                   .ExecuteUpdate() > 0;
        }
    }
}
