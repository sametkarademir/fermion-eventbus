namespace Fermion.EventBus.Base.Exceptions;

/// <summary>
/// Exception thrown when an error occurs in the event bus system.
/// </summary>
public class EventBusException : Exception
{
    /// <summary>
    /// Gets the name of the event that caused the exception.
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Gets the type of the handler that was involved in the exception.
    /// </summary>
    public string HandlerType { get; }

    /// <summary>
    /// Gets the type of error that occurred.
    /// </summary>
    public EventBusErrorTypes ErrorType { get; }

    /// <summary>
    /// Gets additional data associated with the exception.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; }

    /// <summary>
    /// Initializes a new instance of the EventBusException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="eventName">The name of the event that caused the exception.</param>
    /// <param name="handlerType">The type of the handler that was involved in the exception.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    /// <param name="additionalData">Additional data associated with the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EventBusException(
        string message,
        string eventName,
        string handlerType,
        EventBusErrorTypes errorType,
        Dictionary<string, object>? additionalData = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        EventName = eventName;
        HandlerType = handlerType;
        ErrorType = errorType;
        AdditionalData = additionalData ?? new Dictionary<string, object>();
    }
}