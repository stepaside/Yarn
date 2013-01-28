using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Configuration;

namespace Yarn.Data
{
    internal static class DbFactory
    {
        internal static string GetProviderInvariantName(string connectionName)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            return config.ProviderName;
        }

        internal static string GetProviderInvariantNameByConnectionString(string connectionString)
        {
            for (int i = 0; i < ConfigurationManager.ConnectionStrings.Count; i++)
            {
                var config = ConfigurationManager.ConnectionStrings[i];
                if (string.Equals(config.ConnectionString, connectionString, StringComparison.OrdinalIgnoreCase))
                {
                    return config.ProviderName;
                }
            }
            return null;
        }

        internal static DbConnection CreateConnection(string connectionString, string providerName)
        {
            var factory = DbProviderFactories.GetFactory(providerName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;
            return connection;
        }

        internal static DbConnection CreateConnection(string connectionName)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            var factory = DbProviderFactories.GetFactory(config.ProviderName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = config.ConnectionString;
            return connection;
        }

        internal static DbDataAdapter CreateDataAdapter(DbConnection connection)
        {
            var providerName = GetProviderInvariantNameByConnectionString(connection.ConnectionString);
            if (providerName != null)
            {
                return DbProviderFactories.GetFactory(providerName).CreateDataAdapter();
            }
            return null;
        }

        internal static DbParameter CreateParameter(DbConnection connection, string name, object value)
        {
            var providerName = GetProviderInvariantNameByConnectionString(connection.ConnectionString);
            if (providerName != null)
            {
                var parameter = DbProviderFactories.GetFactory(providerName).CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                return parameter;
            }
            return null;
        }
    }
}
