using Avayomi.Core.AniList;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.ObjectMapping;

namespace Avayomi.Core;

[DependsOn(
    typeof(AbpObjectExtendingModule),
    typeof(AbpObjectMappingModule),
    typeof(AbpMapperlyModule)
)]
public sealed class AvayomiCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context
            .Services.AddHttpClient(HttpHelper.ProviderHttpClientName)
            .ConfigureHttpClient(httpClient =>
                httpClient.DefaultRequestHeaders.Add("User-Agent", HttpHelper.ChromeUserAgent())
            )
            .AddStandardResilienceHandler();
        context
            .Services.AddHttpClient<IAniListClient, AniListClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<AniListClientOptions>>().Value;
                    client.BaseAddress = new Uri(options.Url);
                }
            )
            .AddStandardResilienceHandler();
    }
}
