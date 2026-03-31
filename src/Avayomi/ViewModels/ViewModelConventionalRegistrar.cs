using System;
using System.Collections.Generic;
using Avayomi.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Avayomi.ViewModels;

public sealed class ViewModelConventionalRegistrar : DefaultConventionalRegistrar
{
    protected override bool IsConventionalRegistrationDisabled(Type type) =>
        !type.IsAssignableTo<ViewModel>() || base.IsConventionalRegistrationDisabled(type);

    protected override List<Type> GetExposedServiceTypes(Type type)
    {
        var exposedServiceTypes = base.GetExposedServiceTypes(type).AsValueEnumerable();
        var viewModelBaseClasses = type.GetBaseClasses(typeof(ViewModel))
            .AsValueEnumerable()
            .Where(x => x.IsAssignableTo<ViewModel>());
        return exposedServiceTypes.Union(viewModelBaseClasses).Distinct().ToList();
    }
}
