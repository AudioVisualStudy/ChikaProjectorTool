using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace yugop.fullscreen
{
    /// <summary>Class containing method extensions for getting private and internal members.</summary>
    public static class ReflectionUtility
    {
        private static Assembly[] cachedAssemblies;

        public const BindingFlags FULL_BINDING = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static Type FindClass(string name)
        {
            var result = FindTypeInAssembly(name, typeof(Editor).Assembly);
            if (result != null)
                return result;

            if (cachedAssemblies == null)
                cachedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (var i = 0; i < cachedAssemblies.Length; i++)
            {
                result = FindTypeInAssembly(name, cachedAssemblies[i]);
                if (result != null)
                    return result;
            }

            return result;
        }

        private static Type FindTypeInAssembly(string name, Assembly assembly)
        {
            return assembly == null ? null : assembly.GetType(name, false, true);
        }

        public static FieldInfo FindField(this Type type, string fieldName, bool throwNotFound = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            var field = type.GetField(fieldName, FULL_BINDING);
            if (field == null && throwNotFound)
                throw new MissingFieldException(type.FullName, fieldName);
            return field;
        }

        public static PropertyInfo FindProperty(this Type type, string propertyName, bool throwNotFound = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            var prop = type.GetProperty(propertyName, FULL_BINDING);
            if (prop == null && throwNotFound)
                throw new MissingMemberException(type.FullName, propertyName);
            return prop;
        }

        public static MethodInfo FindMethod(this Type type, string methodName, Type[] args = null, bool throwNotFound = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            MethodInfo method;
            if (args == null)
            {
                method = type.GetMethod(methodName, FULL_BINDING);
            }
            else
            {
                method = type.GetMethod(methodName, FULL_BINDING, null, args, null);
                if (method == null)
                {
                    method = FindMethod(type, methodName, null, throwNotFound);
                    if (method != null && method.GetParameters().Length != args.Length)
                        method = null;
                }
            }

            if (method == null && throwNotFound)
                throw new MissingMethodException(type.FullName, methodName);
            return method;
        }

        public static T GetFieldValue<T>(this Type type, string fieldName) => (T)type.FindField(fieldName).GetValue(null);
        public static T GetFieldValue<T>(this object obj, string fieldName) => (T)obj.GetType().FindField(fieldName).GetValue(obj);
        public static void SetFieldValue(this Type type, string fieldName, object value) => type.FindField(fieldName).SetValue(null, value);
        public static void SetFieldValue(this object obj, string fieldName, object value) => obj.GetType().FindField(fieldName).SetValue(obj, value);

        public static T GetPropertyValue<T>(this Type type, string propertyName) => (T)type.FindProperty(propertyName).GetValue(null, null);
        public static T GetPropertyValue<T>(this object obj, string propertyName) => (T)obj.GetType().FindProperty(propertyName).GetValue(obj, null);
        public static void SetPropertyValue(this Type type, string propertyName, object value) => type.FindProperty(propertyName).SetValue(null, value, null);
        public static void SetPropertyValue(this object obj, string propertyName, object value) => obj.GetType().FindProperty(propertyName).SetValue(obj, value, null);

        public static T InvokeMethod<T>(this Type type, string methodName, params object[] args) => (T)type.FindMethod(methodName, args.Select(a => a?.GetType() ?? typeof(object)).ToArray()).Invoke(null, args);
        public static T InvokeMethod<T>(this object obj, string methodName, params object[] args) => (T)obj.GetType().FindMethod(methodName, args.Select(a => a?.GetType() ?? typeof(object)).ToArray()).Invoke(obj, args);
        public static void InvokeMethod(this Type type, string methodName, params object[] args) => type.FindMethod(methodName, args.Select(a => a?.GetType() ?? typeof(object)).ToArray()).Invoke(null, args);
        public static void InvokeMethod(this object obj, string methodName, params object[] args) => obj.GetType().FindMethod(methodName, args.Select(a => a?.GetType() ?? typeof(object)).ToArray()).Invoke(obj, args);

        public static bool IsOfType(this Type toCheck, Type type, bool orInherited = true) => type == toCheck || (orInherited && type != null && type.IsAssignableFrom(toCheck));
        public static bool IsOfType<T>(this T obj, Type type, bool orInherited = true) => obj != null && obj.GetType().IsOfType(type, orInherited);

        public static void EnsureOfType<T>(this T obj, Type type, bool orInherited = true)
        {
            if (obj == null || !obj.IsOfType(type, orInherited))
                throw new InvalidCastException($"Object {obj?.GetType().FullName} must be of type {type?.FullName} or inherited from it");
        }

        public static bool HasField(this Type type, string fieldName) => type.FindField(fieldName, false) != null;
        public static bool HasProperty(this Type type, string propertyName) => type.FindProperty(propertyName, false) != null;
        public static bool HasMethod(this Type type, string methodName, Type[] args = null) => type.FindMethod(methodName, args, false) != null;
        public static bool HasField(this object obj, string fieldName) => obj.GetType().HasField(fieldName);
        public static bool HasProperty(this object obj, string propertyName) => obj.GetType().HasProperty(propertyName);
        public static bool HasMethod(this object obj, string methodName, Type[] args = null) => obj.GetType().HasMethod(methodName, args);
    }
}
