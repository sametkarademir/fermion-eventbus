# Fermion.EventBus.AzureServiceBus

A robust Azure Service Bus implementation of the event bus pattern for .NET applications. This package provides distributed event processing capabilities using Azure Service Bus as the message broker.

## Features

- 🚀 **Distributed Event Processing**: Publish and subscribe to events across multiple applications
- 🔄 **Automatic Reconnection**: Built-in retry mechanism for handling connection issues
- 🔒 **Persistent Connections**: Reliable message delivery with persistent connections
- 🏥 **Health Checks**: Built-in health monitoring for Azure Service Bus connections
- ⚙️ **Flexible Configuration**: Easy configuration through options pattern
- 🔍 **Detailed Logging**: Comprehensive logging for debugging and monitoring
- 🛡️ **Error Handling**: Robust error handling with custom exceptions

## Installation

```bash
  dotnet add package Fermion.EventBus.AzureServiceBus
```

## Quick Start

1. Configure the Azure Service Bus event bus in your `Program.cs`:

```csharp
builder.Services.AddEventBusAzureServiceBus(options =>
{
    // Configure Azure Service Bus connection
    options.ConnectionString = "your_connection_string";
    options.DefaultTopicName = "MyApplication";
    options.ConnectionRetryCount = 5;
    
    // Register event handlers
    options.AddEventHandler<UserCreatedEventHandler>();
    
    // Subscribe to events
    options.AddSubscription<UserCreatedEvent, UserCreatedEventHandler>();
});
```

2. Define your integration event:

```csharp
public class UserCreatedEvent : IntegrationEvent
{
    public Guid UserId { get; }
    public string UserName { get; }

    public UserCreatedEvent(Guid userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }
}
```

3. Create an event handler:

```csharp
public class UserCreatedEventHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event)
    {
        _logger.LogInformation("User created: {UserId}, {UserName}", @event.UserId, @event.UserName);
        // Handle the event
    }
}
```

4. Publish events:

```csharp
public class UserController : ControllerBase
{
    private readonly IEventBus _eventBus;

    public UserController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        // Create user logic...

        var @event = new UserCreatedEvent(userId, request.UserName);
        await _eventBus.PublishAsync(@event);

        return Ok();
    }
}
```

## Configuration Options

| Option | Description | Default Value |
|--------|-------------|---------------|
| `ConnectionString` | Azure Service Bus connection string | "" |
| `DefaultTopicName` | Default topic name for event publishing | "DefaultTopic" |
| `ConnectionRetryCount` | Number of connection retry attempts | 5 |
| `SubscriberClientAppName` | Subscriber client application name | Current domain name |
| `EventNamePrefix` | Prefix for event names | "" |
| `EventNameSuffix` | Suffix for event names | "" |
| `EnableHealthCheck` | Enable health checks | true |
| `HealthCheckInterval` | Health check interval | 30 seconds |

## Health Checks

The package includes built-in health checks for Azure Service Bus connections. To enable health checks:

```csharp
builder.Services.AddEventBusAzureServiceBus(options =>
{
    options.EnableHealthCheck = true;
    options.HealthCheckInterval = TimeSpan.FromSeconds(30);
});
```

## Error Handling

The package provides custom exceptions for different error scenarios:

- `EventBusException`: Base exception for event bus errors
- `ConnectionError`: Raised when there are connection issues
- `PublishingError`: Raised when event publishing fails
- `SubscriptionError`: Raised when subscription operations fail

## Best Practices

1. **Connection Management**:
   - Use persistent connections for reliable message delivery
   - Configure appropriate retry counts for your environment
   - Monitor connection health through health checks

2. **Event Design**:
   - Keep events immutable
   - Include only necessary data in events
   - Use meaningful event names

3. **Error Handling**:
   - Implement proper error handling in event handlers
   - Use logging for debugging and monitoring
   - Handle connection issues gracefully

4. **Performance**:
   - Configure appropriate connection retry intervals
   - Monitor message processing times
   - Use appropriate topic and subscription configurations

5. **Security**:
   - Store connection strings securely (e.g., in Azure Key Vault)
   - Use managed identities when possible
   - Implement proper access control for topics and subscriptions 