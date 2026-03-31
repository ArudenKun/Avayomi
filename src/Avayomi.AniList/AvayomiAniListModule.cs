using Avayomi.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Avayomi.AniList;

[DependsOn(typeof(AvayomiCoreModule))]
public sealed class AvayomiAniListModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context
            .Services.AddAniListClient()
            .ConfigureHttpClient(client =>
                client.BaseAddress = new Uri("https://graphql.anilist.co")
            );
    }
}
