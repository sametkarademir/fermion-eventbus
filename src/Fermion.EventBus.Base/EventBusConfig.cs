namespace Fermion.EventBus.Base;

/// <summary>
/// Configuration class for the Event Bus system that manages connection settings and event naming conventions.
/// </summary>
public class EventBusConfig
{
    /// <summary>
    /// Gets the number of times to retry the connection if it fails.
    /// </summary>
    public int ConnectionRetryCount { get; private set; } = 5;

    /// <summary>
    /// Gets the default topic name used for event publishing.
    /// </summary>
    public string DefaultTopicName { get; private set; } = "DefaultTopicName";

    /// <summary>
    /// Gets the name of the subscriber client application.
    /// </summary>
    public string SubscriberClientAppName { get; private set; } = AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// Gets the prefix to be used for event names.
    /// </summary>
    public string EventNamePrefix { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the suffix to be used for event names.
    /// </summary>
    public string EventNameSuffix { get; private set; } = "IntegrationEvent";

    /// <summary>
    /// Gets the connection object used for the event bus.
    /// </summary>
    public object? Connection { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the event prefix should be deleted.
    /// </summary>
    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);

    /// <summary>
    /// Gets a value indicating whether the event suffix should be deleted.
    /// </summary>
    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);

    /// <summary>
    /// Builder class for constructing EventBusConfig instances with fluent interface.
    /// </summary>
    public class Builder
    {
        private readonly EventBusConfig _config = new EventBusConfig();

        /// <summary>
        /// Sets the connection retry count.
        /// </summary>
        /// <param name="retryCount">The number of times to retry the connection.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public Builder WithConnectionRetryCount(int retryCount)
        {
            _config.ConnectionRetryCount = retryCount;
            return this;
        }

        /// <summary>
        /// Sets the default topic name.
        /// </summary>
        /// <param name="topicName">The name of the default topic.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public Builder WithDefaultTopicName(string topicName)
        {
            _config.DefaultTopicName = topicName;
            return this;
        }

        /// <summary>
        /// Sets the subscriber client application name.
        /// </summary>
        /// <param name="appName">The name of the subscriber client application.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public Builder WithSubscriberClientAppName(string appName)
        {
            _config.SubscriberClientAppName = appName;
            return this;
        }

        /// <summary>
        /// Sets the event name prefix.
        /// </summary>
        /// <param name="prefix">The prefix to be used for event names.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public Builder WithEventNamePrefix(string prefix)
        {
            _config.EventNamePrefix = prefix;
            return this;
        }

        /// <summary>
        /// Sets the event name suffix.
        /// </summary>
        /// <param name="suffix">The suffix to be used for event names.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public Builder WithEventNameSuffix(string suffix)
        {
            _config.EventNameSuffix = suffix;
            return this;
        }

        /// <summary>
        /// Sets the connection object.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public Builder WithConnection(object connection)
        {
            _config.Connection = connection;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured EventBusConfig instance.
        /// </summary>
        /// <returns>A new EventBusConfig instance with the configured settings.</returns>
        public EventBusConfig Build()
        {
            return _config;
        }
    }

    /// <summary>
    /// Creates a new builder instance for configuring EventBusConfig.
    /// </summary>
    /// <returns>A new Builder instance.</returns>
    public static Builder CreateBuilder()
    {
        return new Builder();
    }
}