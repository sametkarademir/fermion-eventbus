using Fermion.EventBus.AzureServiceBus.HealthCheck;
using Fermion.EventBus.AzureServiceBus.Options;
using Fermion.EventBus.AzureServiceBus.Services;
using Fermion.EventBus.Base;
using Fermion.EventBus.Base.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Fermion.EventBus.AzureServiceBus.DependencyInjection;

/// <summary>
/// Extension methods for configuring the Azure Service Bus event bus in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Azure Service Bus event bus to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action to configure the event bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBusAzureServiceBus(
        this IServiceCollection services,
        Action<EventBusAzureServiceBusOptions> configureOptions)
    {
        var options = new EventBusAzureServiceBusOptions();
        configureOptions.Invoke(options);

        // Register all event handlers
        foreach (var handlerType in options.EventHandlers)
        {
            services.AddTransient(handlerType);
        }

        var config = EventBusConfig.CreateBuilder()
            .WithSubscriberClientAppName(options.SubscriberClientAppName)
            .WithDefaultTopicName(options.DefaultTopicName)
            .WithEventNameSuffix(options.EventNameSuffix)
            .WithEventNamePrefix(options.EventNamePrefix)
            .WithConnection(options.ConnectionString)
            .Build();

        services.AddSingleton<IEventBus>(sp => new EventBusServiceBus(config, sp));

        // Add hosted service to handle subscriptions
        services.AddHostedService(sp => new EventBusSubscriptionService(
            sp.GetRequiredService<IEventBus>(),
            options.Subscriptions
        ));

        // Add health check if enabled
        if (options.EnableHealthCheck)
        {
            services.AddSingleton(new AzureServiceBusHealthCheckOptions
            {
                ConnectionString = options.ConnectionString,
                TopicName = options.DefaultTopicName
            });

            services.AddHealthChecks()
                .AddCheck<AzureServiceBusEventBusHealthCheck>(
                    name: "azure_servicebus_eventbus",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["azure", "servicebus", "eventbus"],
                    timeout: options.HealthCheckInterval
                );
        }

        return services;
    }
}