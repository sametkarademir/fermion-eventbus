using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Fermion.EventBus.AzureServiceBus.HealthCheck;

public class AzureServiceBusEventBusHealthCheck : IHealthCheck
{
    private readonly AzureServiceBusHealthCheckOptions _options;
    private readonly ILogger<AzureServiceBusEventBusHealthCheck> _logger;

    public AzureServiceBusEventBusHealthCheck(
        AzureServiceBusHealthCheckOptions options,
        ILogger<AzureServiceBusEventBusHealthCheck> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var client = new ServiceBusClient(_options.ConnectionString);
            await using var sender = client.CreateSender(_options.TopicName);

            // Try to send a test message to check the connection
            var message = new ServiceBusMessage("Health Check")
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = "HealthCheck"
            };

            await sender.SendMessageAsync(message, cancellationToken);
            
            return HealthCheckResult.Healthy("Azure Service Bus is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Service Bus health check failed");
            return HealthCheckResult.Unhealthy("Azure Service Bus is unhealthy", ex);
        }
    }
} 