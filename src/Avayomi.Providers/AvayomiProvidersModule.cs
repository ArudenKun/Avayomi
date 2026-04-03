using Avayomi.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Avayomi.Providers;

[DependsOn(typeof(AvayomiCoreModule))]
public sealed class AvayomiProvidersModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConventionalRegistrar(new AnimeProviderConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context
            .Services.AddHttpClient("AllManga")
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { UseCookies = true, AllowAutoRedirect = true }
            )
            .AddStandardResilienceHandler();
    }
}
