using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace PlsStayHealthy.CustomHealthChecks;
public class MemoryHealthCheck(IOptionsMonitor<MemoryCheckOptions> options) : IHealthCheck
{
    protected static string Name => "memory_check";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                                                    CancellationToken cancellationToken = default)
    {
        var option = options.Get(context.Registration.Name);

        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var data = new Dictionary<string, object>()
        {
            { "AllocatedBytes", allocated },
            { "Gen0Collections", GC.CollectionCount(0) },
            { "Gen1Collections", GC.CollectionCount(1) },
            { "Gen2Collections", GC.CollectionCount(2) },
        };
        
        if (option == null)
            return Task.FromResult(new HealthCheckResult(status: HealthStatus.Unhealthy,
                                                         description: $"Option data could not be fetched.",
                                                         exception: null,
                                                         data: data));
        var status = allocated < option.Threshold 
                    ? HealthStatus.Healthy 
                    : HealthStatus.Unhealthy;

        return Task.FromResult(new HealthCheckResult(status: status,
                                                     description: $"Reports degraded status if allocated bytes >= {option.Threshold} bytes.",
                                                     exception: null,
                                                     data: data));
    }
}

public class MemoryCheckOptions
{
    public string Memorystatus { get; set; }
    public long Threshold { get; set; } = 1024L * 1024L * 1024L;
}


