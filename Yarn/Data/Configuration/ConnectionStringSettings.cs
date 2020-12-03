namespace Yarn.Data.Configuration
{
    internal class ConnectionStringSettings
    {
        public string Name { get; set; }
        public string ConnectionString { get; set;  }
        public string ProviderName { get; set; }

        public ConnectionStringSettings()
        {
        }

        public ConnectionStringSettings(string name, string connectionString)
            : this(name, connectionString, null)
        {
        }

        public ConnectionStringSettings(string name, string connectionString, string providerName)
        {
            Name = name;
            ConnectionString = connectionString;
            ProviderName = providerName;
        }

        protected bool Equals(ConnectionStringSettings other)
        {
            return string.Equals(Name, other.Name) && string.Equals(ConnectionString, other.ConnectionString) && string.Equals(ProviderName, other.ProviderName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConnectionStringSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProviderName != null ? ProviderName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ConnectionStringSettings left, ConnectionStringSettings right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ConnectionStringSettings left, ConnectionStringSettings right)
        {
            return !Equals(left, right);
        }
    }
}
