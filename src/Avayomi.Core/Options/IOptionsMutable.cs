using Microsoft.Extensions.Options;

namespace Avayomi.Core.Options;

/// <summary>
/// Used to access or update the value of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Options type.</typeparam>
public interface IOptionsMutable<out T> : IOptionsMonitor<T>
    where T : class, new()
{
    /// <summary>
    /// Update options field by field
    /// </summary>
    /// <param name="applyChanges"></param>
    /// <returns></returns>
    ValueTask<bool> UpdateAsync(Action<T> applyChanges);
}
