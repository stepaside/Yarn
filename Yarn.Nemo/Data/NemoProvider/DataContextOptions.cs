using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContextOptions
    {
        public DataContextOptions() { }

        public DataContextOptions(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public IConfiguration Configuration { get; set; }
}
}
