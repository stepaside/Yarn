using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Yarn.Reflection
{
    public static class Mapper
    {
        public delegate void PropertyMapper(object source, object target);
        private static ConcurrentDictionary<Tuple<Type, Type>, PropertyMapper> _mappers = new ConcurrentDictionary<Tuple<Type, Type>, PropertyMapper>();

        internal static PropertyMapper CreateDelegate(Type sourceType, Type targetType)
        {
            var key = Tuple.Create(sourceType, targetType);
            var mapper = _mappers.GetOrAdd(key, t => (PropertyMapper)CreateDynamicMethod(t.Item1, t.Item2).CreateDelegate(typeof(PropertyMapper)));
            return mapper;
        }

        private static DynamicMethod CreateDynamicMethod(Type sourceType, Type targetType)
        {
            var method = new DynamicMethod("Map_" + sourceType.FullName + "_" + targetType.FullName, null, new[] { typeof(object), typeof(object) });
            var il = method.GetILGenerator();

            var mapMethod = typeof(Mapper).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == "Map" && m.GetParameters().Length == 2);
            var mapCollectionsMethod = typeof(Mapper).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == "Map" && m.GetParameters().Length == 1 && typeof(IEnumerable).IsAssignableFrom(m.GetParameters()[0].ParameterType));
            var toListMethod = typeof(Enumerable).GetMethod("ToList");

            var sourceProperties = sourceType.GetProperties();
            var targetProperties = targetType.GetProperties();

            var matches = sourceProperties.SelectMany(s => targetProperties.Select(t => Tuple.Create(s, t)))
                                            .Where(t => t.Item1.PropertyType.IsPublic
                                                    && t.Item2.PropertyType.IsPublic
                                                    && t.Item1.CanRead
                                                    && t.Item2.CanWrite
                                                    && t.Item1.Name == t.Item2.Name);

            var simpleMatchesByType = matches.Where(t => t.Item1.PropertyType == t.Item2.PropertyType);

            var objectMatchesByName = matches.Where(t => t.Item1.PropertyType != t.Item2.PropertyType
                                                    && t.Item1.PropertyType.IsClass
                                                    && t.Item2.PropertyType.IsClass
                                                    && t.Item1.PropertyType != typeof(string)
                                                    && t.Item2.PropertyType != typeof(string)
                                                    && !typeof(IEnumerable).IsAssignableFrom(t.Item1.PropertyType)
                                                    && !typeof(IEnumerable).IsAssignableFrom(t.Item2.PropertyType));

            var collectionMatchesByName = matches.Where(t => t.Item1.PropertyType != t.Item2.PropertyType
                                                    && t.Item1.PropertyType.IsGenericType
                                                    && t.Item2.PropertyType.IsGenericType
                                                    && typeof(ICollection).IsAssignableFrom(t.Item1.PropertyType)
                                                    && typeof(ICollection).IsAssignableFrom(t.Item2.PropertyType));

            foreach (var match in simpleMatchesByType)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, targetType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, sourceType);
                il.Emit(OpCodes.Callvirt, match.Item1.GetGetMethod());
                il.Emit(OpCodes.Callvirt, match.Item2.GetSetMethod());
            }

            foreach (var match in objectMatchesByName)
            {
                var ifNull = il.DefineLabel();
                var ifNotNull = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, sourceType);
                il.Emit(OpCodes.Callvirt, match.Item1.GetGetMethod());
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, ifNull);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, targetType);
                il.Emit(OpCodes.Callvirt, match.Item2.GetGetMethod());
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, ifNotNull);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, targetType);
                il.Emit(OpCodes.Newobj, match.Item2.PropertyType.GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Callvirt, match.Item2.GetSetMethod());

                il.MarkLabel(ifNotNull);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, sourceType);
                il.Emit(OpCodes.Callvirt, match.Item1.GetGetMethod());

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, targetType);
                il.Emit(OpCodes.Callvirt, match.Item2.GetGetMethod());

                var call = mapMethod.MakeGenericMethod(match.Item1.PropertyType, match.Item2.PropertyType);
                il.Emit(OpCodes.Call, call);

                il.MarkLabel(ifNull);
            }

            foreach (var match in collectionMatchesByName)
            {
                var ifNull = il.DefineLabel();

                var sourceElementType = match.Item1.PropertyType.GetGenericArguments()[0];
                var targetElementType = match.Item2.PropertyType.GetGenericArguments()[0];

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, sourceType);
                il.Emit(OpCodes.Callvirt, match.Item1.GetGetMethod());
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, ifNull);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, targetType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, sourceType);
                il.Emit(OpCodes.Callvirt, match.Item1.GetGetMethod());
                il.Emit(OpCodes.Castclass, typeof(IEnumerable<>).MakeGenericType(sourceElementType));
                var call = mapCollectionsMethod.MakeGenericMethod(sourceElementType, targetElementType);
                il.Emit(OpCodes.Call, call);
                var toList = toListMethod.MakeGenericMethod(targetElementType);
                il.Emit(OpCodes.Call, toList);
                il.Emit(OpCodes.Castclass, match.Item2.PropertyType);
                il.Emit(OpCodes.Callvirt, match.Item2.GetSetMethod());

                il.MarkLabel(ifNull);
            }

            il.Emit(OpCodes.Ret);

            return method;
        }

        public static TResult Map<TSource, TResult>(TSource source)
            where TSource : class
            where TResult : class, new()
        {
            var result = new TResult();// Activator.CreateInstance<TResult>();
            var mapper = CreateDelegate(typeof(TSource), typeof(TResult));
            mapper(source, result);
            return result;
        }

        public static void Map<TSource, TResult>(TSource source, TResult result)
            where TSource : class
            where TResult : class
        {
            var mapper = CreateDelegate(typeof(TSource), typeof(TResult));
            mapper(source, result);
        }

        public static IEnumerable<TResult> Map<TSource, TResult>(IEnumerable<TSource> source)
            where TSource : class
            where TResult : class, new()
        {
            foreach (var item in source)
            {
                //var result = Activator.CreateInstance<TResult>();
                //Map<TSource, TResult>(item, result);
                //yield return result;
                yield return Map<TSource, TResult>(item);
            }
        }

        public static TResult Map<TSource, TResult>(params TSource[] sources)
            where TSource : class
            where TResult : class, new()
        {
            var result = new TResult();
            foreach (var source in sources)
            {
                Map(source, result);
            }
            return result;
        }
    }
}