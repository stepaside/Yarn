using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Yarn.Reflection
{
    public static class Mapper
    {
        public delegate void PropertyMapper(object source, object target);
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, PropertyMapper> Mappers = new ConcurrentDictionary<Tuple<Type, Type>, PropertyMapper>();
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> Ancestors = new ConcurrentDictionary<Type, HashSet<Type>>();
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, MapperConfiguration> MapperConfigurations = new ConcurrentDictionary<Tuple<Type, Type>, MapperConfiguration>();
        
        internal static PropertyMapper CreateDelegate(Type sourceType, Type targetType)
        {
            var key = Tuple.Create(sourceType, targetType);
            var mapper = Mappers.GetOrAdd(key, t => (PropertyMapper)CreateDynamicMethod(t.Item1, t.Item2).CreateDelegate(typeof(PropertyMapper)));
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

            MapperConfiguration config;
            MapperConfigurations.TryGetValue(Tuple.Create(sourceType, targetType), out config);

            var matches = sourceProperties.SelectMany(s => targetProperties.Select(t => Tuple.Create(s, t)))
                                            .Where(t => t.Item1.PropertyType.IsPublic
                                                    && t.Item2.PropertyType.IsPublic
                                                    && t.Item1.CanRead
                                                    && t.Item2.CanWrite
                                                    && t.Item1.Name == t.Item2.Name
                                                    && (config == null || !config.IsIgnored(t.Item1.Name))).ToList();

            var simpleMatches = matches.Where(t => t.Item1.PropertyType == t.Item2.PropertyType
                                                    && (t.Item1.PropertyType.IsValueType || t.Item1.PropertyType == typeof(string))
                                                    && (t.Item2.PropertyType.IsValueType || t.Item2.PropertyType == typeof(string)));

            var objectMatches = matches.Where(t => t.Item1.PropertyType.IsClass
                                                    && t.Item2.PropertyType.IsClass
                                                    && t.Item1.PropertyType != typeof(string)
                                                    && t.Item2.PropertyType != typeof(string)
                                                    && !typeof(IEnumerable).IsAssignableFrom(t.Item1.PropertyType)
                                                    && !typeof(IEnumerable).IsAssignableFrom(t.Item2.PropertyType)
                                                    && !t.Item1.PropertyType.IsAbstract
                                                    && !t.Item2.PropertyType.IsAbstract
                                                    && !HasCycle(sourceType, t.Item1.PropertyType)
                                                    && !HasCycle(targetType, t.Item2.PropertyType)).ToList();

            foreach (var match in objectMatches)
            {
                if (match.Item1.PropertyType == match.Item2.PropertyType)
                {
                    Ancestors.AddOrUpdate(match.Item1.PropertyType, t => new HashSet<Type>(new[] { sourceType, targetType }), (t, h) =>
                    {
                        h.Add(sourceType);
                        h.Add(targetType);
                        return h;
                    });
                }
            }

            var collectionMatches = matches.Where(t => t.Item1.PropertyType.IsGenericType
                                                    && t.Item2.PropertyType.IsGenericType
                                                    && typeof(IEnumerable).IsAssignableFrom(t.Item1.PropertyType)
                                                    && typeof(IEnumerable).IsAssignableFrom(t.Item2.PropertyType)
                                                    && !t.Item1.PropertyType.GetGenericArguments()[0].IsAbstract
                                                    && !t.Item2.PropertyType.GetGenericArguments()[0].IsAbstract
                                                    && t.Item1.PropertyType.GetGenericArguments()[0].IsClass 
                                                    && t.Item2.PropertyType.GetGenericArguments()[0].IsClass
                                                    && t.Item1.PropertyType.GetGenericArguments()[0] != typeof(string)
                                                    && t.Item2.PropertyType.GetGenericArguments()[0] != typeof(string)
                                                    && !HasCycle(sourceType, t.Item1.PropertyType.GetGenericArguments()[0])
                                                    && !HasCycle(targetType, t.Item2.PropertyType.GetGenericArguments()[0])).ToList();

            foreach (var match in collectionMatches)
            {
                if (match.Item1.PropertyType == match.Item2.PropertyType)
                {
                    Ancestors.AddOrUpdate(match.Item1.PropertyType.GetGenericArguments()[0], t => new HashSet<Type>(new[] { sourceType, targetType }), (t, h) =>
                    {
                        h.Add(sourceType);
                        h.Add(targetType);
                        return h;
                    });
                }
            }

            foreach (var match in simpleMatches)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, targetType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, sourceType);
                il.Emit(OpCodes.Callvirt, match.Item1.GetGetMethod());
                il.Emit(OpCodes.Callvirt, match.Item2.GetSetMethod());
            }

            foreach (var match in objectMatches)
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

            foreach (var match in collectionMatches)
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

        private static bool HasCycle(Type childType, Type parentType)
        {
            HashSet<Type> ancestors;
            if (!Ancestors.TryGetValue(childType, out ancestors))
            {
                return false;
            }
            return ancestors.Contains(parentType) || ancestors.Any(type => HasCycle(parentType, type));
        }

        public static TResult Map<TSource, TResult>(TSource source)
            where TSource : class
            where TResult : class, new()
        {
            var result = new TResult();
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
            return source.Select(Map<TSource, TResult>);
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

        public static MapperConfiguration<TSource, TResult> For<TSource, TResult>()
        {
            return (MapperConfiguration<TSource, TResult>)MapperConfigurations.GetOrAdd(Tuple.Create(typeof(TSource), typeof(TResult)), t => new MapperConfiguration<TSource, TResult>());
        }
    }

    public abstract class MapperConfiguration
    {
        public abstract bool IsIgnored(string property);
    }

    public sealed class MapperConfiguration<TSource, TResult> : MapperConfiguration
    {
        private readonly HashSet<string> _ignoredProperties = new HashSet<string>();

        internal MapperConfiguration()
        {
        }

        public MapperConfiguration<TSource, TResult> Ignore(Expression<Func<TSource, object>> selector)
        {
            var memberExpression = selector.Body as MemberExpression;
            if (memberExpression != null && memberExpression.Expression.NodeType == ExpressionType.Parameter)
            {
                _ignoredProperties.Add(memberExpression.Member.Name);
            }
            return this;
        }

        public override bool IsIgnored(string property)
        {
            return _ignoredProperties.Contains(property);
        }
    }
}