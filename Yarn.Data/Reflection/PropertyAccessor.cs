using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace Yarn.Reflection
{
    public static class PropertyAccessor
    {
        private static ConcurrentDictionary<Tuple<Type, string>, GenericGetter> _getters = new ConcurrentDictionary<Tuple<Type, string>, GenericGetter>();
        private static ConcurrentDictionary<Tuple<Type, string>, GenericSetter> _setters = new ConcurrentDictionary<Tuple<Type, string>, GenericSetter>();

        #region Exist Methods

        public static bool Exits<T>(string propertyName)
        {
            return Exits(typeof(T), propertyName);
        }

        public static bool Exits(Type targetType, string propertyName)
        {
            var propertyKey = Tuple.Create(targetType, propertyName);
            var getMethod = _getters.GetOrAdd(propertyKey, key => GenerateGetter(key));
            var setMethod = _setters.GetOrAdd(propertyKey, key => GenerateSetter(key));
            return getMethod != null && setMethod != null;
        }

        #endregion

        #region Property Getters

        public static object Get<T>(T target, string propertyName)
        {
            return Get(typeof(T), target, propertyName);
        }

        public static TResult Get<T, TResult>(T target, string propertyName)
        {
            return (TResult)Get(typeof(T), target, propertyName);
        }

        public static object Get(Type targetType, object target, string propertyName)
        {
            if (target != null)
            {
                var propertyKey = Tuple.Create(targetType, propertyName);
                var getMethod = _getters.GetOrAdd(propertyKey, key => GenerateGetter(key));
                return getMethod(target);
            }
            return null;
        }

        private static GenericGetter GenerateGetter(Tuple<Type, string> key)
        {
            return CreateGetMethod(key.Item1.GetProperty(key.Item2), key.Item1);
        }

        #endregion

        #region Property Setters

        public static void Set<T>(T target, string propertyName, object value)
        {
            Set(typeof(T), target, propertyName, value);
        }

        public static void Set<T, TValue>(T target, string propertyName, TValue value)
        {
            Set(typeof(T), target, propertyName, value);
        }

        public static void Set(Type targetType, object target, string propertyName, object value)
        {
            if (target != null)
            {
                var propertyKey = Tuple.Create(targetType, propertyName);
                var setMethod = _setters.GetOrAdd(propertyKey, key => GenerateSetter(key));
                setMethod(target, value);
            }
        }

        private static GenericSetter GenerateSetter(Tuple<Type, string> key)
        {
            return CreateSetMethod(key.Item1.GetProperty(key.Item2), key.Item1);
        }

        #endregion

        #region Getter/Setter Methods

        public delegate void GenericSetter(object target, object value);
        public delegate object GenericGetter(object target);

        /// <summary>
        /// Creates a dynamic setter for the property
        /// </summary>
        /// <param name="propertyInfo"></param>
        private static GenericSetter CreateSetMethod(PropertyInfo propertyInfo, Type targetType)
        {
            var setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
            {
                return null;
            }

            var setter = new DynamicMethod("Set_" + propertyInfo.Name, typeof(void), new[] { typeof(object), typeof(object) }, typeof(ObjectFactory), true);
            var generator = setter.GetILGenerator();
            if (!setMethod.IsStatic)
            {
                generator.PushInstance(propertyInfo.DeclaringType);
            }

            generator.Emit(OpCodes.Ldarg_1);
            generator.UnboxIfNeeded(propertyInfo.PropertyType);

            if (setMethod.IsFinal || !setMethod.IsVirtual)
            {
                generator.Emit(OpCodes.Call, setMethod);
            }
            else
            {
                generator.Emit(OpCodes.Callvirt, setMethod);
            }
            generator.Emit(OpCodes.Ret);

            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }

        /// <summary>
        /// Creates a dynamic getter for the property
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private static GenericGetter CreateGetMethod(PropertyInfo propertyInfo, Type targetType)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                return null;
            }

            var getter = new DynamicMethod("Get_" + propertyInfo.Name, typeof(object), new[] { typeof(object) }, typeof(ObjectFactory), true);
            var generator = getter.GetILGenerator();
            if (!getMethod.IsStatic)
            {
                generator.PushInstance(propertyInfo.DeclaringType);
            }

            if (getMethod.IsFinal || !getMethod.IsVirtual)
            {
                generator.Emit(OpCodes.Call, getMethod);
            }
            else
            {
                generator.Emit(OpCodes.Callvirt, getMethod);
            }

            generator.BoxIfNeeded(propertyInfo.PropertyType);
            generator.Emit(OpCodes.Ret);

            return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
        }

        #endregion
    }
}
