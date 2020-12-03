using System;
using Microsoft.Extensions.Configuration;

namespace Yarn.Data.Configuration
{
    internal static class ConnectionStringSettingsExtensions
    {
        public static ConnectionStringSettingsCollection ConnectionStrings(this IConfiguration configuration, string section = "ConnectionStrings")
        {
            var connectionStringCollection = configuration.GetSection(section).Get<ConnectionStringSettingsCollection>();
            if (connectionStringCollection == null)
            {
                return new ConnectionStringSettingsCollection();
            }

            return connectionStringCollection;
        }

        public static ConnectionStringSettings ConnectionString(this IConfiguration configuration, string name, string section = "ConnectionStrings")
        {
            ConnectionStringSettings connectionStringSettings;

            var connectionStringCollection = configuration.GetSection(section).Get<ConnectionStringSettingsCollection>();
            if (connectionStringCollection == null || !connectionStringCollection.TryGetValue(name, out connectionStringSettings))
            {
                return null;
            }

            return connectionStringSettings;
        }
    }
}