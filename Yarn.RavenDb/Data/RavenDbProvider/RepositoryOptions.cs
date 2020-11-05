using Raven.Client.Documents.Session;
using System;

namespace Yarn.Data.RavenDbProvider
{
    public class RepositoryOptions
    {
        public string ConnectionString { get; set; }
        public Action<IDocumentQueryCustomization> QueryCustomization { get; set; }
    }
}
