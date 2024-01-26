using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlsStayHealthy.CustomHealthChecks;
using PlsStayHealthy.Models;

namespace PlsStayHealthy.StartupExtensions;
public static class HealthCheckExtensions
{
    public static void SetupAppHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/api/health", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.UseHealthChecksUI(delegate (Options options) 
        {
            options.UIPath = "/healthcheck-ui";
            options.AddCustomStylesheet("././CustomHealthChecks/Custom.css");
        });
    }
    
    public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthCheckBuilder = services.AddHealthChecks()
            .AddCheck<FootballStandingsHealthCheck>(name: "Football Standings API Health Check",
                                                    failureStatus: HealthStatus.Unhealthy)
            .AddCheck<MemoryHealthCheck>(name: "API Memory Check",
                                         failureStatus: HealthStatus.Unhealthy,
                                         tags: ["Service"]);
        
        healthCheckBuilder.AddMongoDbHealthCheck(configuration);
        healthCheckBuilder.AddRabbitMqHealthCheck(configuration);
        healthCheckBuilder.AddSentinelRedisHealthCheck(configuration);
        healthCheckBuilder.AddHttpClientHealthChecks(configuration);
        
        services.AddHealthChecksUI(opt =>
        {
            opt.SetEvaluationTimeInSeconds(10); // Time in seconds between check
            opt.MaximumHistoryEntriesPerEndpoint(60); // Entry limit for each endpoint
            opt.SetApiMaxActiveRequests(1); // Limit number of concurrent requests
            opt.AddHealthCheckEndpoint("Health API", "/api/health"); // Map health check api
        })
        .AddInMemoryStorage();
    }

    private static void AddMongoDbHealthCheck(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["Databases:Mongo:ConnectionString"]))
            builder.AddMongoDb(mongodbConnectionString: configuration["Databases:Mongo:ConnectionString"],
                               name: "MongoDB",
                               failureStatus: HealthStatus.Unhealthy,
                               tags: ["Database", "MongoDB"],
                               timeout: TimeSpan.FromSeconds(10));
    }

    private static void AddRabbitMqHealthCheck(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["MessageQueue:RabbitMq:ConnectionString"]))
            builder.AddRabbitMQ(rabbitConnectionString: configuration["MessageQueue:RabbitMq:ConnectionString"],
                                sslOption: null,
                                name: "RabbitMQ",
                                failureStatus: HealthStatus.Unhealthy,
                                ["Message-Queue", "RabbitMQ"],
                                TimeSpan.FromSeconds(10));
    }

    private static void AddSentinelRedisHealthCheck(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["Caching:Redis:ConnectionString"]))
            builder.AddRedis(redisConnectionString: configuration["Caching:Redis:ConnectionString"],
                             name: "Redis",
                             failureStatus: HealthStatus.Unhealthy,
                             tags: ["Caching", "Redis", "Sentinel"],
                             timeout: TimeSpan.FromSeconds(10));
    }

    private static void AddHttpClientHealthChecks(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var serviceSettings = configuration.GetSection("Services").Get<ServiceSettings>();
        var props = serviceSettings.GetType().GetProperties().Where(p => p.GetValue(serviceSettings) != null);
        var dependencies = new List<string>();
        builder.AddUrlGroup(
            uriOptions: o =>
            {
                foreach (var propertyInfo in props)
                {
                    var service = (ServiceSettingsModel)propertyInfo.GetValue(serviceSettings);
                    if (!string.IsNullOrEmpty(service?.Health))
                    {
                        o.AddUri(new Uri($"{service.Base}{service.Health}"), s => { s.UseGet(); });
                        dependencies.Add(propertyInfo.Name);
                    }
                }
            },
            name: "Dependent Http Clients",
            failureStatus: HealthStatus.Unhealthy,
            tags: dependencies,
            timeout: TimeSpan.FromSeconds(60));
    }
}
