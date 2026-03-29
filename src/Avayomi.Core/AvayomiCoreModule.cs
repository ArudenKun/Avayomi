using Microsoft.Extensions.DependencyInjection;
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
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", HttpHelper.ChromeUserAgent());
            })
            .AddStandardResilienceHandler();
    }
}
