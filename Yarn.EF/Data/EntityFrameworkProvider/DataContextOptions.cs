using System;
using System.Reflection;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DataContextOptions
    {
        public bool LazyLoadingEnabled { get; set; } = true;
        public bool ProxyCreationEnabled { get; set; } = true;
        public bool AutoDetectChangesEnabled { get; set; } 
        public bool ValidateOnSaveEnabled { get; set; } = true;
        public bool MigrationEnabled { get; set; }
        public string NameOrConnectionString { get; set; }
        public string AssemblyNameOrLocation { get; set; }
        public Assembly ConfigurationAssembly { get; set; }
        public Type DbContextType { get; set; }
    }
}
