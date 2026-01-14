using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SaaS.Provisioning.Shared.DTOs;

namespace SaaS.Orchestrator.ClientHttp.Clients;

public class ProvisioningClient : IProvisioningClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProvisioningClient> _logger;

    public ProvisioningClient(HttpClient httpClient, ILogger<ProvisioningClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TenantStatusDto?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching tenant status for {TenantId} via HTTP", tenantId);

            var response = await _httpClient.GetAsync($"api/tenants/{tenantId}/status", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch tenant status for {TenantId}: {StatusCode}", tenantId, response.StatusCode);
                return null;
            }

            var status = await response.Content.ReadFromJsonAsync<TenantStatusDto>(cancellationToken);

            _logger.LogInformation("Successfully fetched tenant status for {TenantId}: {Status}", tenantId, status?.Status);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant status for {TenantId}", tenantId);
            return null;
        }
    }
}
