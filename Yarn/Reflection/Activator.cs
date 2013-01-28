using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Yarn.Reflection
{
    public static class Activator
    {
        private static ConcurrentDictionary<TypeArray, ObjectActivator> _activatorCache = new ConcurrentDictionary<TypeArray, ObjectActivator>();

        private class TypeArray
        {
            Type _first;
            Type[] _rest;
            uint _hash;

            public TypeArray(IList<Type> types)
            {
                _first = types.First();
                _rest = types.Skip(1).ToArray();
                _hash = ComputeHash(types.OrderBy(t => t.FullName));
            }

            public Type First
            {
                get
                {
                    return _first;
                }
            }

            public Type[] Rest
            {
                get
                {
                    return _rest;
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is TypeArray)
                {
                    return this.GetHashCode() == ((TypeArray)obj).GetHashCode();
                }
                return false;
            }

            public override int GetHashCode()
            {
                return unchecked((int)_hash);
            }

            private static uint ComputeHash(IEnumerable<Type> types)
            {
                var hash = 0u;
                foreach (var type in types)
                {
                    hash += Convert.ToUInt32(type.GetHashCode());
                    hash += (hash << 10);
                    hash ^= (hash >> 6);
                }

                hash += (hash << 3);
                hash ^= (hash >> 11);
                hash += (hash << 15);
                return hash;
            }

        }

        public delegate object ObjectActivator(params object[] args);

        public static ObjectActivator CreateDelegate(Type objectType, params Type[] constructorArgumentTypes)
        {
            var typeList = new List<Type> { objectType };
            if (constructorArgumentTypes.Length > 0)
            {
                typeList.AddRange(constructorArgumentTypes);
            }
            var key = new TypeArray(typeList);
            return _activatorCache.GetOrAdd(key, k => GenerateDelegate(k.First, k.Rest));
        }

        private static ObjectActivator GenerateDelegate(Type objectType, params Type[] types)
        {
            var ctors = objectType.GetConstructors();

            ConstructorInfo ctor = null;
            ParameterInfo[] paramsInfo = null;

            for (int i = 0; i < ctors.Length; i++)
            {
                var c = ctors[i];
                var p = c.GetParameters();
                if (p.Length == types.Length)
                {
                    if (p.Length == 0)
                    {
                        ctor = c;
                        paramsInfo = p;
                        break;
                    }
                    else
                    {
                        var count = p.Select(a => a.ParameterType).Zip(types, (t1, t2) => t1 == t2 || t1.IsAssignableFrom(t2) || t2.IsAssignableFrom(t1) ? 1 : 0).Sum();
                        if (count == types.Length)
                        {
                            ctor = c;
                            paramsInfo = p;
                            break;
                        }
                    }
                }
            }

            var method = new DynamicMethod("CreateInstance", objectType, new[] { typeof(object[]) }, true); // skip visibility is on to allow instantiation of anonyopus type wrappers
            var il = method.GetILGenerator();
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                il.EmitCastToReference(paramsInfo[i].ParameterType);
            }

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            var activator = (ObjectActivator)method.CreateDelegate(typeof(ObjectActivator));
            return activator;
        }
    }
}
