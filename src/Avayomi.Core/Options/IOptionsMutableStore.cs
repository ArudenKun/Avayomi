namespace Avayomi.Core.Options;

/// <summary>
/// Define store interface to save option provided by <see cref="IOptionsMutable{T}"/>
/// <para>You can implement for file/database or other store</para>
/// </summary>
public interface IOptionsMutableStore<in T>
{
    Task UpdateAsync(string section, T options);
}
