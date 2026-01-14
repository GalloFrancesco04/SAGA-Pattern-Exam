using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SaaS.Billing.Shared.DTOs;

namespace SaaS.Orchestrator.ClientHttp.Clients;

public class BillingClient : IBillingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BillingClient> _logger;

    public BillingClient(HttpClient httpClient, ILogger<BillingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching subscription status for {SubscriptionId} via HTTP", subscriptionId);

            var response = await _httpClient.GetAsync($"api/subscriptions/{subscriptionId}/status", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch subscription status for {SubscriptionId}: {StatusCode}", subscriptionId, response.StatusCode);
                return null;
            }

            var status = await response.Content.ReadFromJsonAsync<SubscriptionStatusDto>(cancellationToken);

            _logger.LogInformation("Successfully fetched subscription status for {SubscriptionId}: {Status}", subscriptionId, status?.Status);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription status for {SubscriptionId}", subscriptionId);
            return null;
        }
    }
}
