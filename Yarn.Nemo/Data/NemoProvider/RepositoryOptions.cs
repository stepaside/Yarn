using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class RepositoryOptions
    {
        public RepositoryOptions() { }

        public RepositoryOptions(INemoConfiguration configuration)
        {
            Configuration = configuration;
        }

        public bool UseStoredProcedures { get; set; }
        public INemoConfiguration Configuration { get; set; }
    }
}
