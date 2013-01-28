using System;
using System.Collections.Generic;
using System.IO;
using Yarn;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NHibernate;
using NHibernate.Event;
using NHibernate.Search.Event;

namespace Yarn.Data.NHibernateProvider.LuceneClient
{
    public class LuceneFullTextContext : FullTextProvider
    {
        private string IndexDirectory
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuceneIndex");
            }
        }

        public override void Index<T>()
        {
            FSDirectory entityDirectory = null;
            IndexWriter writer = null;

            var entityType = typeof(T);

            var indexDirectory = new DirectoryInfo(this.IndexDirectory);

            if (indexDirectory.Exists)
            {
                indexDirectory.Delete(true);
            }

            try
            {
                entityDirectory = new Lucene.Net.Store.MMapDirectory(new DirectoryInfo(Path.Combine(indexDirectory.FullName, entityType.Name)));
                writer = new IndexWriter(entityDirectory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), true, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED);
            }
            finally
            {
                if (entityDirectory != null)
                {
                    entityDirectory.Dispose();
                }

                if (writer != null)
                {
                    writer.Dispose();
                }
            }

            var localContext = (IDataContext<ISession>)this.DataContext;
            var fullTextSession = NHibernate.Search.Search.CreateFullTextSession(localContext.Session);
            foreach (var instance in localContext.Session.CreateCriteria<T>().List<T>())
            {
                fullTextSession.Index(instance);
            }

            var sessionImplementation = localContext.Session.GetSessionImplementation();

            sessionImplementation.Listeners.PostDeleteEventListeners = new IPostDeleteEventListener[] { new FullTextIndexEventListener() };
            sessionImplementation.Listeners.PostInsertEventListeners = new IPostInsertEventListener[] { new FullTextIndexEventListener() };
            sessionImplementation.Listeners.PostUpdateEventListeners = new IPostUpdateEventListener[] { new FullTextIndexEventListener() };
        }

        public override IList<T> Search<T>(string searchTerms)
        {
            var localContext = (IDataContext<ISession>)this.DataContext;
            var session = NHibernate.Search.Search.CreateFullTextSession(localContext.Session);
            var fullTextQuery = session.CreateFullTextQuery<T>(searchTerms);
            var results = fullTextQuery.List<T>();
            return results;
        }
    }
}
