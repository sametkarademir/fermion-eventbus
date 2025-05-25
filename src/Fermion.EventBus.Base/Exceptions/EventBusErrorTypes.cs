namespace Fermion.EventBus.Base.Exceptions;

/// <summary>
/// Defines the types of errors that can occur in the event bus system.
/// </summary>
public enum EventBusErrorTypes
{
    /// <summary>
    /// Error related to event subscription management.
    /// </summary>
    SubscriptionError,

    /// <summary>
    /// Error related to event publishing.
    /// </summary>
    PublishingError,

    /// <summary>
    /// Error related to event handler execution.
    /// </summary>
    HandlerExecutionError,

    /// <summary>
    /// Error related to event bus connection.
    /// </summary>
    ConnectionError,

    /// <summary>
    /// Error related to event bus configuration.
    /// </summary>
    ConfigurationError
}