using System;
using System.Collections.Generic;
using System.Text;

namespace Rebus.Internals;

static class TypeExtensions
{
    public static bool IsAssignableTo(this Type sourceType, Type targetType)
    {
        if (sourceType == null) throw new ArgumentNullException(nameof(sourceType));
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));

        if (targetType.IsAssignableFrom(sourceType))
            return true;

        foreach (var baseType in sourceType.GetBaseTypes())
        {
            if (targetType.IsAssignableFrom(baseType))
                return true;

            if (baseType.IsGenericType && targetType.IsGenericTypeDefinition &&
                baseType.GetGenericTypeDefinition() == targetType)
                return true;
        }

        return false;
    }

    public static IEnumerable<Type> GetBaseTypes(this Type type, bool includeSelf = true, bool includeInterfaces = true)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (includeSelf)
            yield return type;
            
        for (var currentType = type.BaseType; currentType != null; currentType = currentType.BaseType)
        {
            yield return currentType;
        }
            
        if (includeInterfaces)
        {
            foreach (var implementedInterface in type.GetInterfaces())
                yield return implementedInterface;
        }
    }

    public static IReadOnlyList<Type> GetGenericTypeParameters(this Type genericType,
        Type genericTypeDefinition)
    {
        if (genericType == null) throw new ArgumentNullException(nameof(genericType));
        if (genericTypeDefinition == null) throw new ArgumentNullException(nameof(genericTypeDefinition));

        if (!genericTypeDefinition.IsGenericTypeDefinition)
        {
            var message = $"{genericTypeDefinition} is not a generic type definition.";
            throw new ArgumentException(message, nameof(genericTypeDefinition));
        }

        var results = new List<Type>();

        foreach (var baseType in genericType.GetBaseTypes())
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericTypeDefinition)
                results.Add(baseType);
        }

        if (results.Count == 0)
        {
            var message = $"{genericType} does not implement {genericTypeDefinition}.";
            throw new ArgumentException(message, nameof(genericType));
        }

        if (results.Count >= 2)
        {
            var message = new StringBuilder();
            message.AppendLine($"{genericType} implements {genericTypeDefinition} multiple times:");

            foreach (var result in results)
                message.AppendLine($"  - {result}");
                
            throw new ArgumentException(message.ToString().Trim(), nameof(genericType));
        }

        return results[0].GetGenericArguments();
    }
}