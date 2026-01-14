using Microsoft.Extensions.Configuration;
using SaaS.Orchestrator.ClientHttp.Clients;
using SaaS.Orchestrator.ClientHttp.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class BillingClientExtensions
{
    public static IServiceCollection AddBillingClient(this IServiceCollection services, IConfiguration configuration)
    {
        var confSection = configuration.GetSection(BillingClientOptions.SectionName);
        var options = confSection.Get<BillingClientOptions>() ?? new();

        services.AddHttpClient<IBillingClient, BillingClient>(o =>
        {
            o.BaseAddress = new Uri(options.BaseAddress);
            o.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
