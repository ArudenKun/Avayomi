using System.Collections.Generic;

namespace Avayomi.Generators.Abstractions;

internal abstract class StaticGenerator
{
	public abstract IEnumerable<(string FileName, string Source)> Generate();
}
