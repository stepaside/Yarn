using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exsage.Core;
using NHibernate.Tool.hbm2ddl;

namespace Exsage.Data.NHibernateProvider
{
    public class NHibernateMigration : IMigration
    {
        private NHibernateDataContext _dataContext = null;

        public NHibernateMigration(NHibernateDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public override void BuildSchema()
        {
            var session = this.Session;
            if (session != null)
            {
                var export = new SchemaExport(this.CreateSessionFactory(this.DefaultFactoryKey).Item2);
                export.Execute(true, true, false, session.Connection, null);
            }
        }

        public override void UpdateSchema()
        {
            var session = this.Session;
            if (session != null)
            {
                var update = new SchemaUpdate(this.CreateSessionFactory(this.DefaultFactoryKey).Item2);
                update.Execute(false, true);
            }
        }
    }
}
