using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;

namespace PlsStayHealthy.CustomHealthChecks;
public class FootballStandingsHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(configuration["Services:FootballStandingsApi:Base"], cancellationToken);
        if (!response.IsSuccessStatusCode)
            return HealthCheckResult.Unhealthy("Football standings API is unhealthy.");

        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonObj = JObject.Parse(resultJson);
        var status = jsonObj["status"]?.Value<bool>();

        return !status.HasValue || !status.Value 
                ? HealthCheckResult.Unhealthy("Football standings API is unhealthy.") 
                : HealthCheckResult.Healthy("Football standings API is healthy.");
    }
}
