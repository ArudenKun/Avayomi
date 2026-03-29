using Avayomi.Core;
using Volo.Abp.Modularity;

namespace Avayomi.Hosting;

[DependsOn(typeof(AvayomiCoreModule))]
public sealed class AvayomiHostingModule : AbpModule { }
