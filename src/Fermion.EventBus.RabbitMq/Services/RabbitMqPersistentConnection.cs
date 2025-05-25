using System.Net.Sockets;
using Fermion.EventBus.Base.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Fermion.EventBus.RabbitMq.Services;

/// <summary>
/// Manages a persistent connection to RabbitMQ with automatic reconnection capabilities.
/// </summary>
public class RabbitMqPersistentConnection : IAsyncDisposable
{
    private readonly ILogger<EventBusRabbitMq> _logger;
    private readonly IConnectionFactory? _connectionFactory;
    private IConnection? _connection;

    /// <summary>
    /// Gets a value indicating whether the connection is established and open.
    /// </summary>
    public bool IsConnected => _connection != null && _connection.IsOpen;

    private readonly int _retryCount;
    private readonly object _lockObject = new object();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqPersistentConnection"/> class.
    /// </summary>
    /// <param name="connectionFactory">The RabbitMQ connection factory.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="retryCount">The number of connection retry attempts.</param>
    public RabbitMqPersistentConnection(IConnectionFactory? connectionFactory, ILogger<EventBusRabbitMq> logger, int retryCount = 5)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _retryCount = retryCount;

        _logger.LogInformation("RabbitMqPersistentConnection initialized with retry count: {RetryCount}", retryCount);
    }

    /// <summary>
    /// Creates a new channel for RabbitMQ operations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created channel.</returns>
    /// <exception cref="EventBusException">Thrown when the connection is not established.</exception>
    public async Task<IChannel> CreateChannelAsync()
    {
        if (!IsConnected)
        {
            _logger.LogError("Cannot create channel: connection is not established.");
            throw new EventBusException(
                "No RabbitMQ connections are available to perform this action",
                "unknown",
                "unknown",
                EventBusErrorTypes.ConnectionError);
        }

        try
        {
            _logger.LogDebug("Creating RabbitMQ channel...");
            if (_connection == null)
            {
                _logger.LogError("Connection is null, cannot create RabbitMQ channel");
                throw new EventBusException(
                    "No RabbitMQ connections are available to perform this action",
                    "unknown",
                    "unknown",
                    EventBusErrorTypes.ConnectionError);
            }

            var channel = await _connection.CreateChannelAsync();
            _logger.LogDebug("Successfully created RabbitMQ model/channel");
            return channel;
        }
        catch (Exception? ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ channel");
            throw;
        }
    }

    /// <summary>
    /// Attempts to establish a connection to RabbitMQ with retry policy.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="EventBusException">Thrown when connection attempts fail.</exception>
    public async Task TryConnect()
    {
        _logger.LogInformation("Attempting to connect to RabbitMQ broker...");

        lock (_lockObject)
        {
            if (IsConnected)
            {
                _logger.LogDebug("Already connected to RabbitMQ broker, skipping connection attempt");
                return;
            }
        }

        var policy = Policy.Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetry(_retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time, retryCount, context) =>
                {
                    _logger.LogWarning(ex,
                        "RabbitMQ Client connection attempt {RetryCount} of {MaxRetryCount} failed after {TimeOut}s. Exception: {ExceptionMessage}",
                        retryCount,
                        _retryCount,
                        $"{time.TotalSeconds:n1}",
                        ex.Message);
                });

        try
        {
            await policy.Execute(async () =>
            {
                if (_connectionFactory == null)
                {
                    _logger.LogError("ConnectionFactory is null, cannot create connection");
                    throw new EventBusException(
                        "ConnectionFactory must not be null",
                        "unknown",
                        "unknown",
                        EventBusErrorTypes.ConnectionError);
                }

                _logger.LogDebug("Creating connection using provided connection factory");
                _connection = await _connectionFactory.CreateConnectionAsync();
            });

            if (!IsConnected)
            {
                throw new EventBusException(
                    "Failed to establish connection to RabbitMQ broker",
                    "unknown",
                    "unknown",
                    EventBusErrorTypes.ConnectionError);
            }

            if (_connection == null)
            {
                throw new EventBusException(
                    "Connection is null after successful connection attempt",
                    "unknown",
                    "unknown",
                    EventBusErrorTypes.ConnectionError);
            }

            _connection.ConnectionShutdownAsync += ConnectionOnConnectionShutdownAsync;
            _connection.CallbackExceptionAsync += ConnectionOnCallbackExceptionAsync;
            _connection.ConnectionBlockedAsync += ConnectionOnConnectionBlockedAsync;

            _logger.LogInformation(
                "RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events",
                _connection.Endpoint.HostName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All connection attempts to RabbitMQ failed");
            throw new EventBusException(
                "Failed to establish connection to RabbitMQ broker",
                "unknown",
                "unknown",
                EventBusErrorTypes.ConnectionError);
        }
    }

    /// <summary>
    /// Handles the connection blocked event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ConnectionOnConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);

        if (_disposed)
        {
            _logger.LogDebug("Connection is disposed, skipping reconnection attempt");
            return;
        }

        _logger.LogInformation("Attempting to reconnect to RabbitMQ after connection was blocked");
        await TryConnect();
    }

    /// <summary>
    /// Handles the callback exception event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ConnectionOnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ connection callback exception occurred. Details: {Details}", e.Detail);

        if (_disposed)
        {
            _logger.LogDebug("Connection is disposed, skipping reconnection attempt");
            return;
        }

        _logger.LogInformation("Attempting to reconnect to RabbitMQ after callback exception");
        await TryConnect();
    }

    /// <summary>
    /// Handles the connection shutdown event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ConnectionOnConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning(
            "RabbitMQ connection is shutdown. Initiator: {Initiator}, ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
            e.Initiator, e.ReplyCode, e.ReplyText);

        if (_disposed)
        {
            _logger.LogDebug("Connection is disposed, skipping reconnection attempt");
            return;
        }

        _logger.LogInformation("Attempting to reconnect to RabbitMQ after connection shutdown");
        await TryConnect();
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and cleans up resources.
    /// </summary>
    /// <returns>A value task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _logger.LogInformation("Disposing RabbitMQ connection");
        _disposed = true;

        try
        {
            if (_connection == null)
            {
                _logger.LogWarning("Connection is null, nothing to dispose");
                return ValueTask.CompletedTask;
            }

            _connection.ConnectionShutdownAsync -= ConnectionOnConnectionShutdownAsync;
            _connection.CallbackExceptionAsync -= ConnectionOnCallbackExceptionAsync;
            _connection.ConnectionBlockedAsync -= ConnectionOnConnectionBlockedAsync;

            _connection.Dispose();
            _logger.LogInformation("RabbitMQ connection disposed successfully");
        }
        catch (Exception? ex)
        {
            _logger.LogError(ex, "Error while disposing RabbitMQ connection");
        }

        return ValueTask.CompletedTask;
    }
}