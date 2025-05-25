namespace Fermion.EventBus.AzureServiceBus.HealthCheck;

public class AzureServiceBusHealthCheckOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
} 