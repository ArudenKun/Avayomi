using Avayomi.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Avayomi.Providers;

[DependsOn(typeof(AvayomiCoreModule))]
public sealed class AvayomiProvidersModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context
            .Services.AddHttpClient("AllManga")
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", HttpHelper.ChromeUserAgent());
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { UseCookies = true, AllowAutoRedirect = true }
            )
            .AddStandardResilienceHandler();
    }
}
