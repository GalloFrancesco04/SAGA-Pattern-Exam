using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SaaS.Billing.Shared.DTOs;

namespace SaaS.Orchestrator.ClientHttp.Clients;

public class BillingClient(HttpClient httpClient, ILogger<BillingClient> logger) : IBillingClient
{
    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching subscription status for {SubscriptionId} via HTTP", subscriptionId);

            var response = await httpClient.GetAsync($"api/subscriptions/{subscriptionId}/status", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch subscription status for {SubscriptionId}: {StatusCode}", subscriptionId, response.StatusCode);
                return null;
            }

            var status = await response.Content.ReadFromJsonAsync<SubscriptionStatusDto>(cancellationToken);

            logger.LogInformation("Successfully fetched subscription status for {SubscriptionId}: {Status}", subscriptionId, status?.Status);

            return status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching subscription status for {SubscriptionId}", subscriptionId);
            return null;
        }
    }
}
