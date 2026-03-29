using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Avayomi.Core.Options;

/// <summary>
/// Implementation for <see cref="IOptionsMutable{T}"/>, use registered <see cref="IOptionsMutableStore{T}"/> to save change
/// </summary>
/// <typeparam name="T"></typeparam>
internal class OptionsMutable<T> : IOptionsMutable<T>
    where T : class, new()
{
    private const string OptionsSuffix = "Options";
    private const string OptionSuffix = "Option";

    private readonly IOptionsMonitor<T> _options;
    private readonly IOptionsMutableStore<T> _store;

    private T? _updatedValue;
    private bool _valueUpdated;

    public OptionsMutable(IServiceProvider provider)
    {
        _store =
            provider.GetService<IOptionsMutableStore<T>>()
            ?? provider.GetRequiredService<IOptionsMutableStore<T>>();
        _options = provider.GetRequiredService<IOptionsMonitor<T>>();
    }

    public T Value
    {
        get
        {
            if (_valueUpdated)
            {
                return _updatedValue!;
            }

            var options = _options.CurrentValue;
            return options;
        }
    }

    public T Get(string? name) => _options.Get(name);

    public IDisposable? OnChange(Action<T, string?> listener) => _options.OnChange(listener);

    public T CurrentValue => _options.CurrentValue;

    public async ValueTask<bool> UpdateAsync(Action<T> applyChanges)
    {
        var section = string.Empty;
        var optionsType = typeof(T);
        if (optionsType.GetCustomAttribute<OptionAttribute>() is { } optionAttribute)
        {
            section = optionAttribute.Section ?? string.Empty;
        }

        if (section.IsNullOrEmpty() || section.IsNullOrWhiteSpace())
        {
            section = optionsType.Name.RemovePostFix(
                StringComparison.InvariantCultureIgnoreCase,
                OptionsSuffix,
                OptionSuffix
            );
        }

        try
        {
            var sectionObject = Value;
            applyChanges(sectionObject);
            await _store.UpdateAsync(section, sectionObject);
            _updatedValue = sectionObject;
            _valueUpdated = true;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
