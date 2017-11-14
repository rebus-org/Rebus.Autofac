using System;
using System.Reflection;

namespace Rebus.Internals
{
    static class Shims
    {
        public static Assembly GetAssembly(this Type type)
        {
#if NET45
            return type.Assembly;
#else
            return type.GetTypeInfo().Assembly;
#endif
        }

        public static bool ItIsAbstract(this Type type)
        {
#if NET45
            return type.IsAbstract;
#else
            return type.GetTypeInfo().IsAbstract;
#endif
        }

        public static bool ItIsGenericType(this Type type)
        {
#if NET45
            return type.IsGenericType;
#else
            return type.GetTypeInfo().IsGenericType;
#endif
        }
    }
}