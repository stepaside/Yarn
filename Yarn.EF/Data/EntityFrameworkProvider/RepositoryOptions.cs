namespace Yarn.Data.EntityFrameworkProvider
{
    public class RepositoryOptions
    {
        public bool MergeOnUpdate { get; set; }
        public bool CommitOnCrud { get; set; } = true;
    }
}
