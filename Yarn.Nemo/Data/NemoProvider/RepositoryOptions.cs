using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class RepositoryOptions
    {
        public RepositoryOptions() { }

        public RepositoryOptions(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public bool UseStoredProcedures { get; set; }
        public IConfiguration Configuration { get; set; }
    }
}
