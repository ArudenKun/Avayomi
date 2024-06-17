using Avayomi.Generators.Abstractions;
using Avayomi.Generators.Statics;
using Avayomi.Generators.Steps;
using Microsoft.CodeAnalysis;

namespace Avayomi.Generators;

[Generator]
internal class Generator : CombinedGenerator
{
    public Generator()
    {
        AddStep<AddViewModelsStep>();
        AddStep<StaticViewLocatorStep>();
        AddStatic<AttributesStatic>();
        AddStatic<DependencyInjectionStatic>();
        // AddStatic<GlobalsStatic>();
    }
}
