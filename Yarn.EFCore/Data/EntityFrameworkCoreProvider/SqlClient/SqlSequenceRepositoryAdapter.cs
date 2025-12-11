using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using Yarn.Adapters;
using Yarn.Reflection;

namespace Yarn.Data.EntityFrameworkCoreProvider.SqlClient
{
    public class SqlSequenceRepositoryAdapter : RepositoryAdapter
    {
        private readonly ISequenceNameProvider _provider;
        private readonly bool _autoRestart;

        private class SequenceRange
        {
            internal SequenceRange()
            {
                Current = -1;
            }

            internal long Current { get; set; }
            internal int RangeSize { get; set; }
            internal bool AutoRestart { get; set; }

            internal long First { get; set; }
            internal long Last { get; set; }
            internal int Increment { get; set; }
            internal long MaxValue { get; set; }

            internal bool IsValid
            {
                get { return RangeSize >= Increment; }
            }

            internal bool IsExhausted
            {
                get { return Current == Last; }
            }

            internal bool IsSequenceExhausted
            {
                get { return Last + RangeSize > MaxValue; }
            }
        }

        private static readonly object Locker = new object();
        private static readonly Dictionary<string, SequenceRange> IdGenerator = new Dictionary<string, SequenceRange>();
        private readonly int _rangeSize;
        
        public SqlSequenceRepositoryAdapter(IRepository repository, ISequenceNameProvider provider, bool autoRestart = false, int rangeSize = 10)
            : base(repository)
        {
            if (!(((IDataContext<DbContext>)Repository.DataContext).Session.Database.GetDbConnection() is SqlConnection))
            {
                throw new NotSupportedException();
            }

            _provider = provider;
            _autoRestart = autoRestart;
            _rangeSize = rangeSize < 1 ? 10 : rangeSize;
        }

        public override T Add<T>(T entity)
        {
            var sequence = _provider.GetSequenceName(typeof(T));
            var primaryKey = ((IMetaDataProvider)Repository).GetPrimaryKey<T>();
            if (primaryKey.Length == 1)
            {
                var property = typeof(T).GetProperty(primaryKey[0]);
                var value = GenerateId(sequence, property.PropertyType, (SqlConnection)((IDataContext<DbContext>)Repository.DataContext).Session.Database.GetDbConnection());
                PropertyAccessor.Set(entity, property.Name, value);
            }
            return base.Add(entity);
        }

        protected object GenerateId(string sequenceName, Type idType, SqlConnection connection)
        {
            lock (Locker)
            {
                SequenceRange range;
                if (!IdGenerator.TryGetValue(sequenceName, out range))
                {
                    range = new SequenceRange { AutoRestart = _autoRestart, RangeSize = _rangeSize };
                    IdGenerator.Add(sequenceName, range);
                }

                if (range.Current == -1)
                {
                    ReserveRange(sequenceName, _rangeSize, connection, range);
                }

                if (range.IsExhausted)
                {
                    range.Current = 0;
                    ReserveRange(sequenceName, _rangeSize, connection, range);
                }

                range.Current += range.Increment;
                return Convert.ChangeType(range.Current, idType);
            }
        }

        private const string SequenceRestartSql = "ALTER SEQUENCE [{0}] RESTART ;";

        private const string SequenceGetRangeSql = @"EXEC sp_sequence_get_range @sequence_name = @sequence_name
, @range_size = @range_size
, @range_first_value = @range_first_value OUTPUT 
, @range_last_value = @range_last_value OUTPUT 
, @sequence_increment = @sequence_increment OUTPUT
, @sequence_max_value = @sequence_max_value OUTPUT ;";

        private static void ReserveRange(string sequenceName, int rangeSize, SqlConnection connection, SequenceRange entry)
        {
            using (var sqlCmd = new SqlCommand((entry.IsSequenceExhausted && entry.AutoRestart ? string.Format(SequenceRestartSql, sequenceName) : "") + SequenceGetRangeSql, connection))
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                var firstValueParam = new SqlParameter("@range_first_value", SqlDbType.Variant) { Direction = ParameterDirection.Output };
                var lastValueParam = new SqlParameter("@range_last_value", SqlDbType.Variant) { Direction = ParameterDirection.Output };
                var incrementValueParam = new SqlParameter("@sequence_increment", SqlDbType.Variant) { Direction = ParameterDirection.Output };
                var maxValueParam = new SqlParameter("@sequence_max_value", SqlDbType.Variant) { Direction = ParameterDirection.Output };

                sqlCmd.Parameters.AddWithValue("@sequence_name", sequenceName);
                sqlCmd.Parameters.AddWithValue("@range_size", rangeSize);
                sqlCmd.Parameters.Add(firstValueParam);
                sqlCmd.Parameters.Add(lastValueParam);
                sqlCmd.Parameters.Add(incrementValueParam);
                sqlCmd.Parameters.Add(maxValueParam);

                sqlCmd.ExecuteNonQuery();

                entry.Current = entry.First = Convert.ToInt64(firstValueParam.Value);
                entry.Last = Convert.ToInt64(lastValueParam.Value);
                entry.Increment = Convert.ToInt32(incrementValueParam.Value);
                entry.MaxValue = Convert.ToInt64(maxValueParam.Value);

                if (!entry.IsValid)
                {
                    entry.Increment = 1;
                }
            }
        }
    }

    public static class RepositoryExtensions
    {
        public static IRepository WithSqlSequenceSupport(this IRepository repository, ISequenceNameProvider provider = null)
        {
            return new SqlSequenceRepositoryAdapter(repository, provider ?? new DefaultSequenceNameProvider());
        }
    }
}
