using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarn.Reflection;

namespace Yarn.Extensions
{
    public static class CascadeExtensions
    {
        public static void Cascade<T>(this T root, Action<T, T> action)
            where T : class
        {
            CascadeImplementation(root, action, null);
        }

        private static void CascadeImplementation<T>(T root, Action<T, T> action, HashSet<T> ancestors)
            where T : class
        {
            ancestors = ancestors ?? new HashSet<T>();
            ancestors.Add(root);

            var properties = root.GetType().GetProperties();
            var objectProperties = properties.Where(t => typeof(T).IsAssignableFrom(t.PropertyType));
            var collectionProperties = properties.Where(t => t.PropertyType.IsGenericType
                                                                && (t.PropertyType.IsClass || typeof(IEnumerable).IsAssignableFrom(t.PropertyType))
                                                                && typeof(ICollection<T>).IsAssignableFrom(t.PropertyType.GetGenericTypeDefinition().MakeGenericType(typeof(T)))
                                                                && typeof(T).IsAssignableFrom(t.PropertyType.GetGenericArguments()[0]));

            foreach (var property in objectProperties)
            {
                var item = (T)PropertyAccessor.Get(root.GetType(), root, property.Name);
                if (item != null && !ancestors.Contains(item))
                {
                    action(root, item);
                    CascadeImplementation(item, action, ancestors);
                }
            }

            foreach (var property in collectionProperties)
            {
                var items = (IEnumerable)PropertyAccessor.Get(root.GetType(), root, property.Name);
                if (items != null)
                {
                    foreach (var item in items.Cast<T>())
                    {
                        if (item != null && !ancestors.Contains(item))
                        {
                            action(root, item);
                            CascadeImplementation(item, action, ancestors);
                        }
                    }
                }
            }
        }
    }
}
