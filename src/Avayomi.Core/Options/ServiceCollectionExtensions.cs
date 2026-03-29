using Microsoft.Extensions.DependencyInjection;

namespace Avayomi.Core.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMutableOptions(
        this IServiceCollection services,
        string filePath,
        OptionsContainerType containerType = OptionsContainerType.Json
    )
    {
        services.Configure<MutableOptionsWrapper>(options => options.FilePath = filePath);
        switch (containerType)
        {
            case OptionsContainerType.Json:
                services.AddSingleton(
                    typeof(IOptionsMutableStore<>),
                    typeof(OptionsMutableJsonFileStore<>)
                );
                break;
            default:
                services.AddSingleton(
                    typeof(IOptionsMutableStore<>),
                    typeof(OptionsMutableJsonFileStore<>)
                );
                break;
        }

        services.AddSingleton(
            typeof(IOptionsMutableStore<>),
            typeof(OptionsMutableJsonFileStore<>)
        );
        services.AddScoped(typeof(IOptionsMutable<>), typeof(OptionsMutable<>));
        return services;
    }
}
