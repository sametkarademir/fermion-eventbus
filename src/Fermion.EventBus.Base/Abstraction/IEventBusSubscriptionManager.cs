using Fermion.EventBus.Base.Events;

namespace Fermion.EventBus.Base.Abstraction;

/// <summary>
/// Manages subscriptions for integration events, providing functionality to add, remove, and query event subscriptions.
/// </summary>
public interface IEventBusSubscriptionManager
{
    /// <summary>
    /// Gets a value indicating whether there are any subscriptions.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Event that is raised when an event is removed from the subscription manager.
    /// </summary>
    event EventHandler<string> OnEventRemoved;

    /// <summary>
    /// Adds a subscription for a specific event type with its handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to subscribe to.</typeparam>
    /// <typeparam name="TH">The type of handler that will process the event.</typeparam>
    void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    /// <summary>
    /// Removes a subscription for a specific event type and its handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to unsubscribe from.</typeparam>
    /// <typeparam name="TH">The type of handler to remove from the subscription.</typeparam>
    void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    /// <summary>
    /// Checks if there are any subscriptions for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of integration event to check.</typeparam>
    /// <returns>True if there are subscriptions for the event type; otherwise, false.</returns>
    bool HasSubscriptionForEvent<T>() where T : IntegrationEvent;

    /// <summary>
    /// Checks if there are any subscriptions for the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event to check.</param>
    /// <returns>True if there are subscriptions for the event name; otherwise, false.</returns>
    bool HasSubscriptionForEvent(string eventName);

    /// <summary>
    /// Gets the event type associated with the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>The Type of the event.</returns>
    Type GetEventTypeByName(string eventName);

    /// <summary>
    /// Removes all subscriptions from the manager.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all handlers registered for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of integration event.</typeparam>
    /// <returns>A collection of subscription information for the event type.</returns>
    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;

    /// <summary>
    /// Gets all handlers registered for the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>A collection of subscription information for the event name.</returns>
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    /// <summary>
    /// Gets the unique key for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of integration event.</typeparam>
    /// <returns>The unique key for the event type.</returns>
    string GetEventKey<T>();
}