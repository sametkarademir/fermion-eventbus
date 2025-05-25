using System.Net.Sockets;
using System.Text;
using Fermion.EventBus.Base;
using Fermion.EventBus.Base.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Fermion.EventBus.RabbitMq.Services;

/// <summary>
/// RabbitMQ implementation of the event bus that provides distributed event processing capabilities.
/// This implementation is suitable for production environments and distributed systems.
/// </summary>
public class EventBusRabbitMq : BaseEventBus
{
    private readonly ILogger<EventBusRabbitMq> _logger;
    private readonly RabbitMqPersistentConnection _persistentConnection;
    private readonly IChannel _consumerChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusRabbitMq"/> class.
    /// </summary>
    /// <param name="config">The event bus configuration.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <exception cref="InvalidOperationException">Thrown when logger is not registered in the service provider.</exception>
    public EventBusRabbitMq(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
    {
        _logger = serviceProvider.GetService(typeof(ILogger<EventBusRabbitMq>)) as ILogger<EventBusRabbitMq> ?? throw new InvalidOperationException();
        _logger.LogInformation("Initializing EventBusRabbitMq with config: DefaultTopicName={DefaultTopicName}, ConnectionRetryCount={ConnectionRetryCount}",
            config.DefaultTopicName,
            config.ConnectionRetryCount);

        IConnectionFactory? connectionFactory;
        if (config.Connection != null)
        {
            _logger.LogDebug("Creating ConnectionFactory from provided configuration");
            try
            {
                var connJson = JsonConvert.SerializeObject(EventBusConfig.Connection, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                });

                connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore,
                });
                _logger.LogInformation("ConnectionFactory created successfully from configuration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ConnectionFactory from configuration");
                throw;
            }
        }
        else
        {
            _logger.LogInformation("No connection configuration provided, using default ConnectionFactory");
            connectionFactory = new ConnectionFactory();
        }

        _persistentConnection = new RabbitMqPersistentConnection(connectionFactory, _logger, config.ConnectionRetryCount);

        _consumerChannel = CreateConsumerChannelAsync().GetAwaiter().GetResult();
        SubsManager.OnEventRemoved += SubsManagerOnOnEventRemovedAsync;
        _logger.LogInformation("EventBusRabbitMq initialization completed successfully");
    }

    /// <summary>
    /// Gets the persistent connection instance.
    /// </summary>
    /// <returns>The RabbitMQ persistent connection.</returns>
    public RabbitMqPersistentConnection GetPersistentConnection() => _persistentConnection;

    /// <summary>
    /// Handles the event removal from the subscription manager.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="eventName">The name of the event being removed.</param>
    private async void SubsManagerOnOnEventRemovedAsync(object? sender, string eventName)
    {
        try
        {
            _logger.LogInformation("Event removed from subscription manager: {EventName}", eventName);
            eventName = ProcessEventName(eventName);

            _logger.LogDebug("Processed event name: {ProcessedEventName}", eventName);
            if (!_persistentConnection.IsConnected)
            {
                _logger.LogWarning("Connection lost, attempting to reconnect...");
                await _persistentConnection.TryConnect();
            }

            try
            {
                await _consumerChannel.QueueUnbindAsync(queue: eventName, exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName);
                _logger.LogInformation(
                    "Successfully unbound queue: {QueueName} from exchange: {Exchange} with routing key: {RoutingKey}",
                    eventName,
                    EventBusConfig.DefaultTopicName,
                    eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unbind queue: {QueueName} from exchange: {Exchange}",
                    eventName,
                    EventBusConfig.DefaultTopicName);
                throw;
            }

            if (SubsManager.IsEmpty)
            {
                _logger.LogInformation("No more subscriptions, closing consumer channel");
                await _consumerChannel.CloseAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling event removal for: {EventName}", eventName);
        }
    }

    /// <summary>
    /// Creates a new consumer channel for processing events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created channel.</returns>
    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            _logger.LogWarning("Connection not established, attempting to connect...");
            await _persistentConnection.TryConnect();
        }

        try
        {
            var channel = await _persistentConnection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(exchange: EventBusConfig.DefaultTopicName, type: ExchangeType.Direct);
            _logger.LogInformation("Successfully created consumer channel with exchange: {Exchange} of type: direct", EventBusConfig.DefaultTopicName);

            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create consumer channel");
            throw;
        }
    }

    /// <summary>
    /// Starts consuming events from the specified queue.
    /// </summary>
    /// <param name="eventName">The name of the event to consume.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task StartBaseConsumeAsync(string eventName)
    {
        _logger.LogInformation("Starting basic consume for event: {EventName}", eventName);

        try
        {
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.ReceivedAsync += ConsumerOnReceivedAsync;

            await _consumerChannel.BasicConsumeAsync(
                queue: GetSubName(eventName),
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("Basic consumer started for queue: {QueueName}", GetSubName(eventName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start basic consumer for event: {EventName}", eventName);
            throw;
        }
    }

    /// <summary>
    /// Handles the received event message.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments containing the received message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ConsumerOnReceivedAsync(object? sender, BasicDeliverEventArgs e)
    {
        var eventName = e.RoutingKey;
        var messageId = e.BasicProperties.MessageId ?? Guid.NewGuid().ToString();

        _logger.LogInformation("Received message with routing key: {RoutingKey}, delivery tag: {DeliveryTag}, message id: {MessageId}",
            eventName,
            e.DeliveryTag,
            messageId);

        eventName = ProcessEventName(eventName);
        var message = Encoding.UTF8.GetString(e.Body.Span);

        _logger.LogDebug("Processing message content: {MessageContent}", message.Length > 1000 ? message.Substring(0, 1000) + "..." : message);

        try
        {
            await ProcessEvent(eventName, message);
            await _consumerChannel.BasicAckAsync(e.DeliveryTag, multiple: false);
            _logger.LogInformation("Successfully processed message with delivery tag: {DeliveryTag}", e.DeliveryTag);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing message with delivery tag: {DeliveryTag}, will nack and requeue", e.DeliveryTag);
            await _consumerChannel.BasicNackAsync(e.DeliveryTag, multiple: false, requeue: true);
            throw new InvalidOperationException("Error processing message", exception);
        }
    }

    /// <summary>
    /// Publishes an integration event to RabbitMQ.
    /// </summary>
    /// <param name="event">The integration event to publish.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        _logger.LogInformation("Publishing event: {EventName}, event type: {EventType}", eventName, @event.GetType().FullName);

        if (!_persistentConnection.IsConnected)
        {
            _logger.LogWarning("Connection not established, attempting to connect before publishing...");
            await _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(EventBusConfig.ConnectionRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s",
                        $"{time.TotalSeconds:n1}");
                });

        eventName = ProcessEventName(eventName);

        await _consumerChannel.ExchangeDeclareAsync(exchange: EventBusConfig.DefaultTopicName, type: "direct");

        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);

        _logger.LogDebug("Serialized event to publish, message size: {MessageSize} bytes", body.Length);

        await policy.Execute(async () =>
        {
            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            _logger.LogDebug("Publishing to exchange: {Exchange} with routing key: {RoutingKey}, message id: {MessageId}",
                EventBusConfig.DefaultTopicName, eventName, properties.MessageId);

            await _consumerChannel.BasicPublishAsync(
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Successfully published event: {EventName} with message id: {MessageId}",
                eventName, properties.MessageId);
        });
    }

    /// <summary>
    /// Subscribes to an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of the integration event.</typeparam>
    /// <typeparam name="TH">The type of the event handler.</typeparam>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SubscribeAsync<T, TH>()
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Subscribing to event: {EventName} with handler: {HandlerType}",
            eventName, typeof(TH).FullName);

        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionForEvent(eventName))
        {
            _logger.LogInformation("No existing subscription for event: {EventName}, creating queue and binding", eventName);

            if (!_persistentConnection.IsConnected)
            {
                _logger.LogWarning("Connection not established, attempting to connect before subscribing...");
                await _persistentConnection.TryConnect();
            }

            try
            {
                await _consumerChannel.QueueDeclareAsync(
                    queue: GetSubName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                _logger.LogInformation("Queue declared: {QueueName}", GetSubName(eventName));

                await _consumerChannel.QueueBindAsync(
                    queue: GetSubName(eventName),
                    exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName
                );

                _logger.LogInformation("Queue bound: {QueueName} to exchange: {Exchange} with routing key: {RoutingKey}",
                    GetSubName(eventName),
                    EventBusConfig.DefaultTopicName, eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to event: {EventName}", eventName);
                throw;
            }
        }
        else
        {
            _logger.LogDebug("Subscription already exists for event: {EventName}", eventName);
        }

        SubsManager.AddSubscription<T, TH>();
        await StartBaseConsumeAsync(eventName);

        _logger.LogInformation("Successfully subscribed to event: {EventName}", eventName);
    }

    /// <summary>
    /// Unsubscribes from an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of the integration event.</typeparam>
    /// <typeparam name="TH">The type of the event handler.</typeparam>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task UnSubscribeAsync<T, TH>()
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Unsubscribing from event: {EventName} with handler: {HandlerType}",
            eventName,
            typeof(TH).FullName);

        try
        {
            SubsManager.RemoveSubscription<T, TH>();
            _logger.LogInformation("Successfully unsubscribed from event: {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from event: {EventName}", eventName);
            throw;
        }

        await Task.CompletedTask;
    }
}