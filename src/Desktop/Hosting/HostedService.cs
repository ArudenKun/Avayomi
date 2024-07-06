using Ardalis.GuardClauses;
using Microsoft.Extensions.Hosting;

namespace Desktop.Hosting;

public class HostedService
{
    public HostedService(IHostedService service, string friendlyName)
    {
        Service = Guard.Against.Null(service);
        FriendlyName = Guard.Against.Null(friendlyName);
    }

    public IHostedService Service { get; }
    public string FriendlyName { get; }
}