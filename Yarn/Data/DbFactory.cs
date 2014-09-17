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

            if (connectionString == null) return null;

            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            object providerValue;
            if (builder.TryGetValue("provider", out providerValue))
            {
                return providerValue.ToString();
            }

            var persistSecurityInfo = false;
            object persistSecurityInfoValue;
            if (builder.TryGetValue("persist security info", out persistSecurityInfoValue))
            {
                persistSecurityInfo = Convert.ToBoolean(persistSecurityInfoValue);
            }

            var lostPassword = !persistSecurityInfo && !builder.ContainsKey("pwd") && !builder.ContainsKey("password");

            if (!lostPassword)
            {
                for (var i = 0; i < ConfigurationManager.ConnectionStrings.Count; i++)
                {
                    var config = ConfigurationManager.ConnectionStrings[i];
                    if (string.Equals(config.ConnectionString, connectionString, StringComparison.OrdinalIgnoreCase))
                    {
                        return config.ProviderName;
                    }
                }
            }
            else
            {
                object uid;
                if (builder.TryGetValue("uid", out uid))
                {
                    builder.Remove("uid");
                    builder["user id"] = uid;
                }

                for (var i = 0; i < ConfigurationManager.ConnectionStrings.Count; i++)
                {
                    var config = ConfigurationManager.ConnectionStrings[i];

                    var otherBuilder = new DbConnectionStringBuilder { ConnectionString = config.ConnectionString };
                    otherBuilder.Remove("pwd");
                    otherBuilder.Remove("password");

                    object otherUid;
                    if (otherBuilder.TryGetValue("uid", out otherUid))
                    {
                        otherBuilder.Remove("uid");
                        otherBuilder["user id"] = otherUid;
                    }

                    if (otherBuilder.Count != builder.Count) continue;

                    var equivalenCount = builder.Cast<KeyValuePair<string, object>>().Select(p =>
                    {
                        object value;
                        return otherBuilder.TryGetValue(p.Key, out value) && string.Equals(Convert.ToString(value), Convert.ToString(p.Value), StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    }).Sum();

                    if (equivalenCount == builder.Count)
                    {
                        return config.ProviderName;
                    }
                }
            }

            return null;
        }

        public static DbConnection CreateConnection(string connectionString, string providerName)
        {
            var factory = DbProviderFactories.GetFactory(providerName);
            var connection = factory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = connectionString;
            }
            return connection;
        }

        public static DbConnection CreateConnection(string connectionName)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            var factory = DbProviderFactories.GetFactory(config.ProviderName);
            var connection = factory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = config.ConnectionString;
            }
            return connection;
        }

        public static DbDataAdapter CreateDataAdapter(DbConnection connection)
        {
            var providerName = GetProviderInvariantNameByConnectionString(connection.ConnectionString);
            return providerName != null ? DbProviderFactories.GetFactory(providerName).CreateDataAdapter() : null;
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
