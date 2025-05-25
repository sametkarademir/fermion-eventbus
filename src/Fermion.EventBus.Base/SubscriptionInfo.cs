namespace Fermion.EventBus.Base;

/// <summary>
/// Represents information about an event subscription, including the handler type that processes the event.
/// </summary>
public class SubscriptionInfo
{
    /// <summary>
    /// Gets the type of the handler that processes the event.
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// Initializes a new instance of the SubscriptionInfo class.
    /// </summary>
    /// <param name="handlerType">The type of the handler that will process the event.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlerType is null.</exception>
    private SubscriptionInfo(Type handlerType)
    {
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
    }

    /// <summary>
    /// Creates a new SubscriptionInfo instance with the specified handler type.
    /// </summary>
    /// <param name="handlerType">The type of the handler that will process the event.</param>
    /// <returns>A new SubscriptionInfo instance.</returns>
    public static SubscriptionInfo Typed(Type handlerType)
    {
        return new SubscriptionInfo(handlerType);
    }
}