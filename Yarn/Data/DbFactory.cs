using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Configuration;

namespace Yarn.Data
{
    public static class DbFactory
    {
        public static string GetProviderInvariantName(string connectionName)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            return config.ProviderName;
        }

        public static string GetProviderInvariantNameByConnectionString(string connectionString)
        {
            for (int i = 0; i < ConfigurationManager.ConnectionStrings.Count; i++)
            {
                var config = ConfigurationManager.ConnectionStrings[i];
                if (string.Equals(config.ConnectionString, connectionString, StringComparison.OrdinalIgnoreCase))
                {
                    return config.ProviderName;
                }
                else if (config.ConnectionString.IndexOf(connectionString, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var pos1 = config.ConnectionString.IndexOf("provider=", StringComparison.OrdinalIgnoreCase);
                    if (pos1 > -1)
                    {
                        var pos2 = config.ConnectionString.IndexOf(";", pos1);
                        if (pos2 > -1)
                        {
                            return config.ConnectionString.Substring(pos1 + 9, pos2 - pos1 - 9);
                        }
                        else
                        {
                            return config.ConnectionString.Substring(pos1 + 9);
                        }
                    }
                }
            }
            return null;
        }

        public static DbConnection CreateConnection(string connectionString, string providerName)
        {
            var factory = DbProviderFactories.GetFactory(providerName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;
            return connection;
        }

        public static DbConnection CreateConnection(string connectionName)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            var factory = DbProviderFactories.GetFactory(config.ProviderName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = config.ConnectionString;
            return connection;
        }

        public static DbDataAdapter CreateDataAdapter(DbConnection connection)
        {
            var providerName = GetProviderInvariantNameByConnectionString(connection.ConnectionString);
            if (providerName != null)
            {
                return DbProviderFactories.GetFactory(providerName).CreateDataAdapter();
            }
            return null;
        }

        public static DbParameter CreateParameter(DbConnection connection, string name, object value)
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
