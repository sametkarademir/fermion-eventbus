using Fermion.EventBus.Base.Abstraction;
using Fermion.EventBus.Base.Events;

namespace Fermion.EventBus.AzureServiceBus.Options;

/// <summary>
/// Configuration options for the Azure Service Bus event bus.
/// </summary>
public class EventBusAzureServiceBusOptions
{
    /// <summary>
    /// Gets or sets the number of connection retry attempts.
    /// </summary>
    public int ConnectionRetryCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the default topic name for event publishing.
    /// </summary>
    public string DefaultTopicName { get; set; } = "DefaultTopic";

    /// <summary>
    /// Gets or sets the subscriber client application name.
    /// </summary>
    public string SubscriberClientAppName { get; set; } = AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// Gets or sets the prefix for event names.
    /// </summary>
    public string EventNamePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suffix for event names.
    /// </summary>
    public string EventNameSuffix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Service Bus connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether health checks are enabled.
    /// </summary>
    public bool EnableHealthCheck { get; set; } = true;

    /// <summary>
    /// Gets or sets the health check interval.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the list of event handler types to register.
    /// </summary>
    public List<Type> EventHandlers { get; } = new();

    /// <summary>
    /// Gets the list of event and handler type pairs to subscribe.
    /// </summary>
    public List<(Type EventType, Type HandlerType)> Subscriptions { get; } = new();

    /// <summary>
    /// Adds an event handler type to the registration list.
    /// </summary>
    /// <typeparam name="THandler">The type of the event handler.</typeparam>
    /// <returns>The options instance for chaining.</returns>
    public EventBusAzureServiceBusOptions AddEventHandler<THandler>() where THandler : IntegrationEventHandler
    {
        EventHandlers.Add(typeof(THandler));
        return this;
    }

    /// <summary>
    /// Adds an event and handler type pair to the subscription list.
    /// </summary>
    /// <typeparam name="TEvent">The type of the integration event.</typeparam>
    /// <typeparam name="THandler">The type of the event handler.</typeparam>
    /// <returns>The options instance for chaining.</returns>
    public EventBusAzureServiceBusOptions AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        Subscriptions.Add((typeof(TEvent), typeof(THandler)));
        return this;
    }
}