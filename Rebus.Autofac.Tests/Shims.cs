#if NETSTANDARD1_6
// ReSharper disable CheckNamespace
using System.Reflection;

namespace System
{
    static class ReflectionExtensions
    {
        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return type.GetTypeInfo().GetProperty(name);
        }

        public static bool IsInstanceOfType(this Type type, object o)
        {
            return type.GetTypeInfo().IsInstanceOfType(o);
        }

        public static Type[] GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().GetInterfaces();
        }

        public static bool HasGenericTypeDefinition(this Type type, Type openGenericType)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == openGenericType;
        }
    }
}
#endif

#if NET45
using System.Reflection;

namespace System
{
    static class ReflectionExtensions
    {
        public static bool HasGenericTypeDefinition(this Type type, Type openGenericType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == openGenericType;
        }
    }
}
#endif