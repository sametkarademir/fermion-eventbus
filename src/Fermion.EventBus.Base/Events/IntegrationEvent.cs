namespace Fermion.EventBus.Base.Events;

/// <summary>
/// Base class for all integration events in the system.
/// </summary>
public class IntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Gets the metadata associated with this event.
    /// </summary>
    public Dictionary<string, string> MetaData { get; private set; }

    /// <summary>
    /// Initializes a new instance of the IntegrationEvent class with default metadata.
    /// </summary>
    public IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        MetaData = new Dictionary<string, string>();

        WithMetaData("AppName", AppDomain.CurrentDomain.FriendlyName);
        WithMetaData("MachineName", System.Environment.MachineName);
        WithMetaData("Environment", System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
    }

    /// <summary>
    /// Adds a single metadata key-value pair to the event.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current IntegrationEvent instance for method chaining.</returns>
    public IntegrationEvent WithMetaData(string key, string value)
    {
        MetaData[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple metadata key-value pairs to the event.
    /// </summary>
    /// <param name="metaData">Dictionary containing the metadata key-value pairs to add.</param>
    /// <returns>The current IntegrationEvent instance for method chaining.</returns>
    public IntegrationEvent WithMetaData(Dictionary<string, string> metaData)
    {
        foreach (var kvp in metaData)
        {
            WithMetaData(kvp.Key, kvp.Value);
        }
        return this;
    }
}