using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Configuration;
using System.Reflection;
using System.IO;

namespace Yarn.Data
{
    public static class DbFactory
    {
        public static string GetProviderInvariantName(string connectionName)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            return config != null ? config.ProviderName : null;
        }

        public static string GetProviderInvariantName(string connectionName, out string connectionString)
        {
            var config = ConfigurationManager.ConnectionStrings[connectionName];
            connectionString = config != null ? config.ConnectionString : null;
            return config != null ? config.ProviderName : null;
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
#if NETSTANDARD2_0
            var factory = GetDbProviderFactory(providerName);
#else
            var factory = DbProviderFactories.GetFactory(providerName);
#endif
            var connection = factory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = connectionString;
            }
            return connection;
        }

        public static DbConnection CreateConnection(string connectionName)
        {
            string connectionString;
            var providerName = GetProviderInvariantName(connectionName, out connectionString);
            if (providerName == null) return null;

#if NETSTANDARD2_0
            var factory = GetDbProviderFactory(providerName);
#else
            var factory = DbProviderFactories.GetFactory(providerName);
#endif
            var connection = factory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = connectionString;
            }
            return connection;
        }

        public static DbDataAdapter CreateDataAdapter(DbConnection connection)
        {
            return CreateDataAdapter(connection.ConnectionString);
        }

        public static DbDataAdapter CreateDataAdapter(IDataContext dataContext)
        {
            return CreateDataAdapter(dataContext.Source);
        }

        public static DbDataAdapter CreateDataAdapter(string connectionString)
        {
            var providerName = GetProviderInvariantNameByConnectionString(connectionString);
#if NETSTANDARD2_0
            return providerName != null ? GetDbProviderFactory(providerName).CreateDataAdapter() : null;
#else
            return providerName != null ? DbProviderFactories.GetFactory(providerName).CreateDataAdapter() : null;
#endif
        }

        public static DbParameter CreateParameter(DbConnection connection, string name, object value)
        {
            return CreateParameter(connection.ConnectionString, name, value);
        }

        public static DbParameter CreateParameter(IDataContext dataContext, string name, object value)
        {
            return CreateParameter(dataContext.Source, name, value);
        }

        public static DbParameter CreateParameter(string connectionString, string name, object value)
        {
            var providerName = GetProviderInvariantNameByConnectionString(connectionString);
            if (providerName != null)
            {

#if NETSTANDARD2_0
                var parameter = GetDbProviderFactory(providerName).CreateParameter();
#else
                var parameter = DbProviderFactories.GetFactory(providerName).CreateParameter();
#endif
                parameter.ParameterName = name;
                parameter.Value = value;
                return parameter;
            }
            return null;
        }

#if NETSTANDARD2_0
        private static DbProviderFactory GetDbProviderFactory(string providerName)
        {
            if (providerName == null)
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            providerName = providerName.ToLower();

            if (providerName == "system.data.sqlclient")
            {
                return GetDbProviderFactory(DataAccessProviderTypes.SqlServer);
            }

            if (providerName == "microsoft.data.sqlclient")
            {
                return GetDbProviderFactory(DataAccessProviderTypes.SqlServerCore);
            }

            if (providerName == "system.data.sqlite" || providerName == "microsoft.data.sqlite")
            {
                return GetDbProviderFactory(DataAccessProviderTypes.SqLite);
            }

            if (providerName == "mysql.data.mysqlclient" || providerName == "mysql.data")
            {
                return GetDbProviderFactory(DataAccessProviderTypes.MySql);
            }

            if (providerName == "oracle.dataaccess.client")
            {
                return GetDbProviderFactory(DataAccessProviderTypes.Oracle);
            }

            if (providerName == "npgsql")
            {
                return GetDbProviderFactory(DataAccessProviderTypes.PostgreSql);
            }

            throw new NotSupportedException(string.Format("Unsupported Provider Factory specified: {0}", providerName));
        }

        private static DbProviderFactory GetDbProviderFactory(DataAccessProviderTypes type)
        {
            if (type == DataAccessProviderTypes.SqlServer)
            {
                return GetDbProviderFactory("System.Data.SqlClient.SqlClientFactory", "System.Data.SqlClient");
            }

            if (type == DataAccessProviderTypes.SqlServerCore)
            {
                return GetDbProviderFactory("Microsoft.Data.SqlClient.SqlClientFactory", "Microsoft.Data.SqlClient");
            }

            if (type == DataAccessProviderTypes.SqLite)
            {
                return GetDbProviderFactory("Microsoft.Data.Sqlite.SqliteFactory", "Microsoft.Data.Sqlite");
            }

            if (type == DataAccessProviderTypes.MySql)
            {
                return GetDbProviderFactory("MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data");
            }

            if (type == DataAccessProviderTypes.PostgreSql)
            {
                return GetDbProviderFactory("Npgsql.NpgsqlFactory", "Npgsql");
            }

            if (type == DataAccessProviderTypes.Oracle)
            {
                return GetDbProviderFactory("Oracle.DataAccess.Client.OracleClientFactory", "Oracle.DataAccess");
            }

            throw new NotSupportedException(string.Format("Unsupported Provider Factory specified: {0}", type.ToString()));
        }

        private static DbProviderFactory GetDbProviderFactory(string dbProviderFactoryTypename, string assemblyName)
        {
            var instance = GetStaticProperty(dbProviderFactoryTypename, "Instance");
            if (instance == null)
            {
                var a = LoadAssembly(assemblyName);
                if (a != null)
                {
                    instance = GetStaticProperty(dbProviderFactoryTypename, "Instance");
                }
            }

            if (instance == null)
            {
                throw new InvalidOperationException(string.Format("Unable to retrieve DbProviderFactory for: {0}", dbProviderFactoryTypename));
            }

            return instance as DbProviderFactory;
        }

        private static object GetStaticProperty(string typeName, string property)
        {
            var type = GetTypeFromName(typeName);
            return type == null ? null : GetStaticProperty(type, property);
        }

        public static object GetStaticProperty(Type type, string property)
        {
            object result;
            try
            {
                result = type.InvokeMember(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty, null, type, null);
            }
            catch
            {
                return null;
            }

            return result;
        }

        private static Type GetTypeFromName(string typeName, string assemblyName)
        {
            var type = Type.GetType(typeName, false);
            if (type != null) return type;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // try to find manually
            foreach (Assembly asm in assemblies)
            {
                type = asm.GetType(typeName, false);

                if (type != null) break;
            }

            if (type != null) return type;

            // see if we can load the assembly
            if (!string.IsNullOrEmpty(assemblyName))
            {
                var a = LoadAssembly(assemblyName);
                if (a != null)
                {
                    type = Type.GetType(typeName, false);
                    if (type != null) return type;
                }
            }

            return null;
        }

        private static Type GetTypeFromName(string typeName)
        {
            return GetTypeFromName(typeName, null);
        }

        private static Assembly LoadAssembly(string assemblyName)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch { }

            if (assembly != null) return assembly;

            if (File.Exists(assemblyName))
            {
                assembly = Assembly.LoadFrom(assemblyName);
                if (assembly != null) return assembly;
            }
            return null;
        }
#endif
    }
}
