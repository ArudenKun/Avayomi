using Microsoft.CodeAnalysis;

namespace Avayomi.Generators.Abstractions;

internal record GeneratorStepContext(GeneratorExecutionContext Context, Compilation Compilation);
