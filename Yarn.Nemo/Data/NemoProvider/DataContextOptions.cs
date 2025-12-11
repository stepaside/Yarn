using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContextOptions
    {
        public DataContextOptions() { }

        public DataContextOptions(INemoConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public INemoConfiguration Configuration { get; set; }
}
}
