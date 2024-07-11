using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Generator.Extensions;

internal static class AttributeDataExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="attributeData"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ITypeSymbol? GetGenericTypeArgument(
        this AttributeData attributeData,
        int position
    )
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(position);
    }

    /// <summary>
    /// </summary>
    /// <param name="attributeData"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static TypedConstant GetNamedArgument(this AttributeData attributeData, string name)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.NamedArguments.FirstOrDefault(pair => pair.Key == name).Value;
    }

    /// <summary>
    ///     <para>Finds the argument with the given name and returns it's value.</para>
    ///     <para>If not found, it returns null.</para>
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static TypedConstant? GetArgument(
        this ImmutableArray<KeyValuePair<string, TypedConstant>> arguments,
        string name
    )
    {
        foreach (var t in arguments.Where(t => t.Key == name))
            return t.Value;

        return null;
    }

    /// <summary>
    ///     <para>Finds the argument with the given name and returns it's value.</para>
    ///     <para>If not found or value is not castable, it returns default.</para>
    /// </summary>
    /// <param name="attributeData"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static T? GetArgument<T>(this AttributeData attributeData, string name)
    {
        var arguments = attributeData.NamedArguments;

        return GetArgument(arguments, name) switch
        {
            { Value: T value } => value,
            _ => default
        };
    }

    /// <summary>
    ///     <para>Finds the argument with the given name and returns it's value as array.</para>
    ///     <para>If not found or any value is not castable, it returns an empty array.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="attributeData"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static T[] GetArgumentArray<T>(this AttributeData attributeData, string name)
    {
        var arguments = attributeData.NamedArguments;

        if (arguments.GetArgument(name) is not { Kind: TypedConstantKind.Array } typeArray)
            return [];

        var result = new T[typeArray.Values.Length];
        for (var i = 0; i < result.Length; i++)
        {
            if (typeArray.Values[i].Value is not T value)
                return [];
            result[i] = value;
        }

        return result;
    }
}
