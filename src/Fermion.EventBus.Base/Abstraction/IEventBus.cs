using Fermion.EventBus.Base.Events;

namespace Fermion.EventBus.Base.Abstraction;

/// <summary>
/// Defines the contract for an event bus that handles publishing and subscribing to integration events.
/// </summary>
public interface IEventBus : IDisposable
{
    /// <summary>
    /// Publishes an integration event to the event bus.
    /// </summary>
    /// <param name="event">The integration event to publish.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IntegrationEvent @event);

    /// <summary>
    /// Subscribes to an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to subscribe to.</typeparam>
    /// <typeparam name="TH">The type of handler that will process the event.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    /// <summary>
    /// Unsubscribes from an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of integration event to unsubscribe from.</typeparam>
    /// <typeparam name="TH">The type of handler to remove from the subscription.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnSubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
}