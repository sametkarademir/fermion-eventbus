using Fermion.EventBus.Base.Events;

namespace Fermion.EventBus.Base.Abstraction;

/// <summary>
/// Represents a handler for processing integration events of a specific type.
/// </summary>
/// <typeparam name="TIntegrationEvent">The type of integration event this handler can process.</typeparam>
public interface IIntegrationEventHandler<TIntegrationEvent> : IntegrationEventHandler where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Handles the specified integration event.
    /// </summary>
    /// <param name="event">The integration event to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Handle(TIntegrationEvent @event);
}

/// <summary>
/// Marker interface for integration event handlers.
/// </summary>
public interface IntegrationEventHandler
{
}