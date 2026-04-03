using Avayomi.Core.Providers.Anime;
using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Avayomi.Providers;

public sealed class AnimeProviderConventionalRegistrar : DefaultConventionalRegistrar
{
    protected override bool IsConventionalRegistrationDisabled(Type type) =>
        !type.IsAssignableTo<IAnimeProvider>() || base.IsConventionalRegistrationDisabled(type);

    protected override List<Type> GetExposedServiceTypes(Type type)
    {
        var exposedServiceTypes = base.GetExposedServiceTypes(type).AsValueEnumerable();
        return exposedServiceTypes.Union([typeof(IAnimeProvider)]).Distinct().ToList();
    }
}
