using Fermion.EventBus.Base.Abstraction;
using Fermion.EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Fermion.EventBus.Base.Events;

/// <summary>
/// Base class for implementing event bus functionality with common event processing logic.
/// </summary>
public abstract class BaseEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    protected readonly IEventBusSubscriptionManager SubsManager;
    protected EventBusConfig EventBusConfig { get; set; }

    /// <summary>
    /// Initializes a new instance of the BaseEventBus class.
    /// </summary>
    /// <param name="config">The event bus configuration.</param>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    protected BaseEventBus(EventBusConfig config, IServiceProvider serviceProvider)
    {
        EventBusConfig = config;
        _serviceProvider = serviceProvider;

        SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
    }

    /// <summary>
    /// Processes the event name by removing configured prefix and suffix.
    /// </summary>
    /// <param name="eventName">The original event name.</param>
    /// <returns>The processed event name.</returns>
    protected string ProcessEventName(string eventName)
    {
        if (EventBusConfig.DeleteEventPrefix)
        {
            eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());
        }

        if (EventBusConfig.DeleteEventSuffix)
        {
            var array = EventBusConfig.EventNameSuffix.ToArray();
            eventName = eventName.TrimEnd(array);
        }

        return eventName;
    }

    /// <summary>
    /// Gets the subscription name for an event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <returns>The subscription name in the format "AppName.EventName".</returns>
    protected string GetSubName(string eventName)
    {
        return $"{EventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
    }

    /// <summary>
    /// Disposes of the event bus resources.
    /// </summary>
    public virtual void Dispose()
    {
        EventBusConfig = null;
        SubsManager.Clear();
    }

    /// <summary>
    /// Processes an event by finding and invoking all registered handlers.
    /// </summary>
    /// <param name="eventName">The name of the event to process.</param>
    /// <param name="message">The serialized event message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ProcessEvent(string eventName, string message)
    {
        eventName = ProcessEventName(eventName);
        if (SubsManager.HasSubscriptionForEvent(eventName))
        {
            var subscriptions = SubsManager.GetHandlersForEvent(eventName);
            using var scope = _serviceProvider.CreateScope();

            foreach (var subscription in subscriptions)
            {
                var handler = _serviceProvider.GetService(subscription.HandlerType);
                if (handler == null) continue;

                var eventType = SubsManager.GetEventTypeByName(
                    $"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");

                var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                await (Task)concreteType.GetMethod("Handle").Invoke(handler, [integrationEvent]);
            }
        }
    }

    /// <summary>
    /// Publishes an integration event to the event bus.
    /// </summary>
    /// <param name="event">The integration event to publish.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task PublishAsync(IntegrationEvent @event);

    /// <summary>
    /// Subscribes to an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to subscribe to.</typeparam>
    /// <typeparam name="TH">The type of handler that will process the event.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    /// <summary>
    /// Unsubscribes from an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to unsubscribe from.</typeparam>
    /// <typeparam name="TH">The type of handler to remove from the subscription.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task UnSubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
}