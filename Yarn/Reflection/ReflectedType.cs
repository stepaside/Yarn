using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Yarn.Reflection
{
    public static class ReflectedType<T>
    {
        private static readonly Lazy<HashSet<Type>> ChildTypesLazy = new Lazy<HashSet<Type>>(() => GetChildTypes(typeof(T)), true);

        public static HashSet<Type> ChildTypes
        {
            get { return ChildTypesLazy.Value; }
        }

        private static HashSet<Type> GetChildTypes(Type type, HashSet<Type> ancestors = null)
        {
            var set = new HashSet<Type>();
            (ancestors = ancestors ?? new HashSet<Type>()).Add(type);

            var properties = type.GetProperties().Where(p => p.PropertyType.IsPublic
                                                          && !p.PropertyType.IsValueType
                                                          && p.PropertyType != typeof(string)).ToList();

            var objectProperties = properties.Where(p => p.PropertyType.IsClass
                                                         && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType));

            foreach (var property in objectProperties.Where(property => !ancestors.Contains(property.PropertyType)))
            {
                set.Add(property.PropertyType);
                var childTypes = GetChildTypes(property.PropertyType, ancestors);
                set.UnionWith(childTypes);
            }

            var collectionProperties = properties.Where(p => p.PropertyType.IsGenericType
                                                             && typeof(IEnumerable).IsAssignableFrom(p.PropertyType));

            foreach (var propertyType in collectionProperties.Select(property => property.PropertyType.GetGenericArguments()[0]).Where(propertyType => !ancestors.Contains(propertyType)))
            {
                set.Add(propertyType);
                var childTypes = GetChildTypes(propertyType, ancestors);
                set.UnionWith(childTypes);
            }

            return set;
        }
    }
}
