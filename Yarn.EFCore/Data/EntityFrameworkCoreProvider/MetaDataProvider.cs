﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Yarn.Reflection;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    internal sealed class MetaDataProvider
    {
        private static readonly Lazy<MetaDataProvider> Instance = new Lazy<MetaDataProvider>(() => new MetaDataProvider(), true);
        private readonly ConcurrentDictionary<Type, string[]> _keys = new ConcurrentDictionary<Type, string[]>();

        private MetaDataProvider()
        {
        }

        public static MetaDataProvider Current
        {
            get { return Instance.Value; }
        }

        public string[] GetPrimaryKey<T>(DbContext context) 
            where T : class
        {
            return _keys.GetOrAdd(typeof(T), t => GetPrimaryKeyFromTypeHierarchy(t, context));
        }

        internal static string[] GetPrimaryKeyFromTypeHierarchy(Type type, DbContext context)
        {
            do
            {
                try
                {
                    return GetPrimaryKeyFromType(type, context);
                }
                catch
                {
                    type = type.BaseType;
                }
            } while (type != typeof(object));

            return new string[] { };
        }

        internal static string[] GetPrimaryKeyFromType(Type type, DbContext context)
        {
            return context.Model.FindEntityType(type).FindPrimaryKey().Properties.Select(p => p.Name).ToArray();
        }

        internal object[] GetPrimaryKeyValue(object entity, DbContext context)
        {
            var entityType = entity.GetType();
            var primaryKey = _keys.GetOrAdd(entityType, t => GetPrimaryKeyFromTypeHierarchy(t, context)); 
            var values = new object[primaryKey.Length];
            for (var i = 0; i < primaryKey.Length; i++)
            {
                values[i] = PropertyAccessor.Get(entityType, entity, primaryKey[i]);
            }
            return values;
        }

        public object[] GetPrimaryKeyValue<T>(T entity, DbContext context) 
            where T : class
        {
            var primaryKey = GetPrimaryKey<T>(context);
            var values = new object[primaryKey.Length];
            for (var i = 0; i < primaryKey.Length; i++)
            {
                values[i] = PropertyAccessor.Get(entity, primaryKey[i]);
            }
            return values;
        }
    }
}
