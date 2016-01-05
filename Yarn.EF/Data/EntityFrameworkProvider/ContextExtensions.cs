using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Yarn.Data.EntityFrameworkProvider
{
    public static class ContextExtensions
    {
        public static string GetTableName<T>(this DbContext context) where T : class
        {
            var objectContext = ((IObjectContextAdapter)context).ObjectContext;
            return objectContext.GetTableName<T>();
        }

        internal static string GetTableName(this DbContext context, Type type)
        {
            var objectContext = ((IObjectContextAdapter)context).ObjectContext;
            return objectContext.GetTableName(type);
        }

        //public static string GetTableName<T>(this ObjectContext context) where T : class
        //{
        //    var sql = context.CreateObjectSet<T>().ToTraceString();
        //    var regex = new Regex("FROM\\s+(?<table>.*)\\s+AS");
        //    var match = regex.Match(sql);

        //    var table = match.Groups["table"].Value;
        //    return table;
        //}

        //public static string GetColumnName<T>(this DbContext context, string name) where T : class
        //{
        //    var objectContext = ((IObjectContextAdapter)context).ObjectContext;
        //    var tableName = objectContext.GetTableName<T>();

        //    var itemCollection = objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
        //    GlobalItem i;
        //    if (itemCollection == null)
        //    {
        //        return null;
        //    }

        //    return null;
        //}

        private const BindingFlags Bindings = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        public static string GetTableName<T>(this ObjectContext context) where T : class
        {
            return context.GetTableName(typeof(T));
        }

        internal static string GetTableName(this ObjectContext context, Type type)
        {
            var ocModel = context.MetadataWorkspace.GetItemCollection(DataSpace.OCSpace);
            var ocItem = ocModel.FirstOrDefault(o => o.GetType().Name == "ObjectTypeMapping" && ((EdmType)o.GetType().GetProperty("ClrType", Bindings).GetValue(o)).FullName == type.FullName);
            return ocItem != null ? ((EdmType)ocItem.GetType().GetProperty("EdmType", Bindings).GetValue(ocItem)).Name : "";
        }
    }
}