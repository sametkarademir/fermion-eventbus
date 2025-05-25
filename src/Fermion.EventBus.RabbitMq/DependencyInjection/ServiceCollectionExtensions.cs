using Fermion.EventBus.Base;
using Fermion.EventBus.Base.Abstraction;
using Fermion.EventBus.RabbitMq.HealthCheck;
using Fermion.EventBus.RabbitMq.Options;
using Fermion.EventBus.RabbitMq.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Fermion.EventBus.RabbitMq.DependencyInjection;

/// <summary>
/// Extension methods for configuring the RabbitMQ event bus in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the RabbitMQ event bus to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action to configure the event bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBusRabbitMq(
        this IServiceCollection services,
        Action<EventBusRabbitMqOptions> configureOptions)
    {
        var options = new EventBusRabbitMqOptions();
        configureOptions.Invoke(options);

        // Register all event handlers
        foreach (var handlerType in options.EventHandlers)
        {
            services.AddTransient(handlerType);
        }

        var config = EventBusConfig.CreateBuilder()
            .WithConnectionRetryCount(options.ConnectionRetryCount)
            .WithSubscriberClientAppName(options.SubscriberClientAppName)
            .WithDefaultTopicName(options.DefaultTopicName)
            .WithEventNameSuffix(options.EventNameSuffix)
            .WithEventNamePrefix(options.EventNamePrefix)
            .WithConnection(new ConnectionFactory()
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password
            })
            .Build();

        services.AddSingleton<IEventBus>(sp => new EventBusRabbitMq(config, sp));

        // Add hosted service to handle subscriptions
        services.AddHostedService(sp => new EventBusSubscriptionService(
            sp.GetRequiredService<IEventBus>(),
            options.Subscriptions
        ));

        // Add health check if enabled
        if (options.EnableHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<RabbitMqEventBusHealthCheck>(
                    name: "rabbitmq_eventbus",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["rabbitmq", "eventbus"],
                    timeout: options.HealthCheckInterval
                );
        }

        return services;
    }
}