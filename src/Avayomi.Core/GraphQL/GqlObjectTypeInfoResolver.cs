using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Avayomi.Core.GraphQL;

// Note: Ensure you are targeting .NET 7 or higher.
internal class GqlObjectTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);

        // We only want to customize object serialization (skip primitives, arrays, etc.)
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return typeInfo;
        }

        // Clear the default properties STJ found so we have absolute control
        typeInfo.Properties.Clear();

        var members = GetSerializableMembers(type);

        foreach (var member in members)
        {
            var attribute = member.GetCustomAttribute<GqlSelectionAttribute>();

            // Equivalent to Newtonsoft's property.Ignored = true
            if (attribute is null)
            {
                continue;
            }

            JsonPropertyInfo jsonProperty;

            // Wire up getters and setters for both public and non-public members
            if (member is PropertyInfo pi)
            {
                jsonProperty = typeInfo.CreateJsonPropertyInfo(pi.PropertyType, pi.Name);
                jsonProperty.Get = pi.CanRead ? pi.GetValue : null;
                jsonProperty.Set = pi.CanWrite ? pi.SetValue : null;
            }
            else if (member is FieldInfo fi)
            {
                jsonProperty = typeInfo.CreateJsonPropertyInfo(fi.FieldType, fi.Name);
                jsonProperty.Get = fi.GetValue;
                jsonProperty.Set = fi.SetValue;
            }
            else
            {
                continue;
            }

            // Apply custom alias or name
            jsonProperty.Name = string.IsNullOrEmpty(attribute.Alias)
                ? attribute.Name
                : attribute.Alias;

            // System.Text.Json will throw an exception on duplicate JSON property names.
            // This check prevents crashes if a derived class shadows a base class member.
            if (typeInfo.Properties.All(p => p.Name != jsonProperty.Name))
            {
                typeInfo.Properties.Add(jsonProperty);
            }
        }

        return typeInfo;
    }

    private List<MemberInfo> GetSerializableMembers(Type objectType)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var properties = objectType
            .GetProperties(flags)
            .Where(property => property.CanWrite)
            .Cast<MemberInfo>();

        var fields = objectType.GetFields(flags).Cast<MemberInfo>();

        return (
            objectType.BaseType is null
                ? properties.Concat(fields)
                : properties.Concat(fields).Concat(GetSerializableMembers(objectType.BaseType))
        ).ToList();
    }
}
