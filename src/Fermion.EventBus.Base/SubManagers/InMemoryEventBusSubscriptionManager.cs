using Fermion.EventBus.Base.Abstraction;
using Fermion.EventBus.Base.Events;
using Fermion.EventBus.Base.Exceptions;

namespace Fermion.EventBus.Base.SubManagers;

/// <summary>
/// Manages event subscriptions in memory, providing functionality to add, remove, and query event subscriptions.
/// </summary>
public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
    private readonly List<Type> _eventTypes;
    private readonly Func<string, string> _eventNameGetter;
    public event EventHandler<string>? OnEventRemoved;

    /// <summary>
    /// Initializes a new instance of the InMemoryEventBusSubscriptionManager class.
    /// </summary>
    /// <param name="eventNameGetter">Function to process event names.</param>
    public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)
    {
        _handlers = new Dictionary<string, List<SubscriptionInfo>>();
        _eventTypes = new List<Type>();
        _eventNameGetter = eventNameGetter;
    }

    /// <summary>
    /// Gets a value indicating whether there are any subscriptions.
    /// </summary>
    public bool IsEmpty => !_handlers.Keys.Any();

    /// <summary>
    /// Removes all subscriptions from the manager.
    /// </summary>
    public void Clear() => _handlers.Clear();

    /// <summary>
    /// Gets the event key for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of integration event.</typeparam>
    /// <returns>The processed event key.</returns>
    public string GetEventKey<T>()
    {
        var eventName = typeof(T).Name;
        return _eventNameGetter(eventName);
    }

    /// <summary>
    /// Adds a subscription for a specific event type with its handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to subscribe to.</typeparam>
    /// <typeparam name="TH">The type of handler that will process the event.</typeparam>
    /// <exception cref="EventBusException">Thrown when the handler is already registered for the event.</exception>
    public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        AddSubscription(typeof(TH), eventName);

        if (!_eventTypes.Contains(typeof(T)))
        {
            _eventTypes.Add(typeof(T));
        }
    }

    /// <summary>
    /// Adds a subscription for a specific event with its handler type.
    /// </summary>
    /// <param name="handlerType">The type of the handler.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <exception cref="EventBusException">Thrown when the handler is already registered for the event.</exception>
    private void AddSubscription(Type handlerType, string eventName)
    {
        if (!HasSubscriptionForEvent(eventName))
        {
            _handlers.Add(eventName, new List<SubscriptionInfo>());
        }

        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new EventBusException(
                $"Handler Type {handlerType.Name} already registered for '{eventName}'",
                eventName,
                handlerType.Name,
                EventBusErrorTypes.SubscriptionError);
        }

        _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
    }

    /// <summary>
    /// Removes a subscription for a specific event type and its handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to unsubscribe from.</typeparam>
    /// <typeparam name="TH">The type of handler to remove from the subscription.</typeparam>
    public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var handlerToRemove = FindSubscriptionToRemove<T, TH>();
        var eventName = GetEventKey<T>();
        RemoveHandler(eventName, handlerToRemove);
    }

    /// <summary>
    /// Removes a handler from the subscription list for a specific event.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="subsToRemove">The subscription information to remove.</param>
    private void RemoveHandler(string eventName, SubscriptionInfo subsToRemove)
    {
        _handlers[eventName].Remove(subsToRemove);
        if (_handlers[eventName].Count != 0)
        {
            return;
        }

        _handlers.Remove(eventName);
        var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
        if (eventType != null)
        {
            _eventTypes.Remove(eventType);
        }

        RaiseOnEventRemoved(eventName);
    }

    /// <summary>
    /// Gets all handlers registered for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of integration event.</typeparam>
    /// <returns>A collection of subscription information for the event type.</returns>
    public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return GetHandlersForEvent(key);
    }

    /// <summary>
    /// Gets all handlers registered for the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>A collection of subscription information for the event name.</returns>
    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

    /// <summary>
    /// Raises the OnEventRemoved event.
    /// </summary>
    /// <param name="eventName">The name of the removed event.</param>
    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }

    /// <summary>
    /// Finds the subscription to remove for a specific event type and handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event.</typeparam>
    /// <typeparam name="TH">The type of handler.</typeparam>
    /// <returns>The subscription information to remove.</returns>
    /// <exception cref="EventBusException">Thrown when the subscription is not found.</exception>
    private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
        where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        return FindSubscriptionToRemove(eventName, typeof(TH));
    }

    /// <summary>
    /// Finds the subscription to remove for a specific event name and handler type.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="handlerType">The type of the handler.</param>
    /// <returns>The subscription information to remove.</returns>
    /// <exception cref="EventBusException">Thrown when the subscription is not found.</exception>
    private SubscriptionInfo FindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionForEvent(eventName))
        {
            throw new EventBusException(
                $"No subscription for event {eventName} exists",
                eventName,
                handlerType.Name,
                EventBusErrorTypes.SubscriptionError);
        }

        return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType)
               ?? throw new EventBusException(
                   $"Handler Type {handlerType.Name} not found for '{eventName}'",
                   eventName,
                   handlerType.Name,
                   EventBusErrorTypes.SubscriptionError);
    }

    /// <summary>
    /// Checks if there are any subscriptions for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of integration event to check.</typeparam>
    /// <returns>True if there are subscriptions for the event type; otherwise, false.</returns>
    public bool HasSubscriptionForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return HasSubscriptionForEvent(key);
    }

    /// <summary>
    /// Checks if there are any subscriptions for the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event to check.</param>
    /// <returns>True if there are subscriptions for the event name; otherwise, false.</returns>
    public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <summary>
    /// Gets the event type associated with the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>The Type of the event.</returns>
    /// <exception cref="EventBusException">Thrown when the event type is not found.</exception>
    public Type GetEventTypeByName(string eventName) =>
        _eventTypes.SingleOrDefault(t => t.Name == eventName)
        ?? throw new EventBusException(
            $"Event {eventName} not found",
            eventName,
            "Unknown",
            EventBusErrorTypes.SubscriptionError);
}