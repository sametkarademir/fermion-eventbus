using Fermion.EventBus.Base;
using Fermion.EventBus.Base.Abstraction;
using Fermion.EventBus.InMemory.Options;
using Fermion.EventBus.InMemory.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fermion.EventBus.InMemory.DependencyInjection;

/// <summary>
/// Extension methods for configuring the in-memory event bus in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory event bus to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action to configure the event bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBusInMemory(
        this IServiceCollection services,
        Action<EventBusInMemoryOptions> configureOptions)
    {
        var options = new EventBusInMemoryOptions();
        configureOptions.Invoke(options);

        // Register all event handlers
        foreach (var handlerType in options.EventHandlers)
        {
            services.AddTransient(handlerType);
        }

        var config = EventBusConfig.CreateBuilder()
            .WithSubscriberClientAppName(options.SubscriberClientAppName)
            .WithEventNameSuffix(options.EventNameSuffix)
            .WithEventNamePrefix(options.EventNamePrefix)
            .Build();

        services.AddSingleton<IEventBus>(sp => new EventBusInMemory(config, sp));

        // Add hosted service to handle subscriptions
        services.AddHostedService(sp => new EventBusSubscriptionService(
            sp.GetRequiredService<IEventBus>(),
            options.Subscriptions
        ));

        return services;
    }
}