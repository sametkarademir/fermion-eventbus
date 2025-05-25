using Fermion.EventBus.Base.Abstraction;
using Fermion.EventBus.RabbitMq.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Fermion.EventBus.RabbitMq.HealthCheck;

public class RabbitMqEventBusHealthCheck : IHealthCheck
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<RabbitMqEventBusHealthCheck> _logger;

    public RabbitMqEventBusHealthCheck(
        IEventBus eventBus,
        ILogger<RabbitMqEventBusHealthCheck> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_eventBus is not EventBusRabbitMq rabbitMqEventBus)
            {
                return HealthCheckResult.Unhealthy(
                    "Event bus is not RabbitMQ implementation",
                    null,
                    new Dictionary<string, object>
                    {
                        { "EventBusType", _eventBus.GetType().Name }
                    });
            }

            var persistentConnection = rabbitMqEventBus.GetPersistentConnection();

            if (!persistentConnection.IsConnected)
            {
                _logger.LogWarning("RabbitMQ connection is not established");
                return HealthCheckResult.Unhealthy(
                    "RabbitMQ connection is not established",
                    null,
                    new Dictionary<string, object>
                    {
                        { "IsConnected", false }
                    });
            }

            // Try to create a channel to verify connection is working
            await using var channel = await persistentConnection.CreateChannelAsync();
            if (channel == null)
            {
                _logger.LogWarning("Failed to create RabbitMQ channel");
                return HealthCheckResult.Unhealthy(
                    "Failed to create RabbitMQ channel",
                    null,
                    new Dictionary<string, object>
                    {
                        { "IsConnected", true },
                        { "ChannelCreated", false }
                    });
            }

            _logger.LogInformation("RabbitMQ Event Bus health check passed");
            return HealthCheckResult.Healthy(
                "RabbitMQ Event Bus is healthy",
                new Dictionary<string, object>
                {
                    { "IsConnected", true },
                    { "ChannelCreated", true }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ Event Bus health check failed");
            return HealthCheckResult.Unhealthy(
                "RabbitMQ Event Bus health check failed",
                ex,
                new Dictionary<string, object>
                {
                    { "Exception", ex.Message }
                });
        }
    }
}