using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    public static class ContextExtensions
    {
        public static string GetTableName<T>(this DbContext context) where T : class
        {
            return context.GetTableName(typeof(T));
        }

        public static string GetTableName(this DbContext context, Type type)
        {
            return context.Model.FindEntityType(type).GetTableName();
        }

        public static string GetColumnName<T>(this DbContext context, string propertyName) where T : class
        {
            return context.GetColumnName(typeof(T), propertyName);
        }

        public static string GetColumnName(this DbContext context, Type type, string propertyName)
        {
            return context.Model.FindEntityType(type).FindProperty(propertyName).GetColumnName();
        }

        internal static IList<ColumnMapping> GetColumns<T>(this DbContext context) where T : class
        {
            return context.GetColumns(typeof(T));
        }

        internal static IList<ColumnMapping> GetColumns(this DbContext context, Type type)
        {
            return context.Model.FindEntityType(type).GetProperties().Select(p => new ColumnMapping { PropertyName = p.Name, ColumnName = p.GetColumnName() }).ToArray();
        }
    }
}