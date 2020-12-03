using Microsoft.Extensions.Configuration;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    public class RepositoryOptions
    {
        public bool MergeOnUpdate { get; set; }
        public bool CommitOnCrud { get; set; } = true;
        public IConfiguration Configuration { get; set; }
    }
}
