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
using EntityFramework.MappingAPI.Extensions;

namespace Yarn.Data.EntityFrameworkProvider
{
    public static class ContextExtensions
    {
        public static string GetTableName<T>(this DbContext context) where T : class
        {
            var map = context.Db<T>();
            return map.TableName;
        }

        internal static string GetTableName(this DbContext context, Type type)
        {
            var map = context.Db(type);
            return map.TableName;
        }
    }
}