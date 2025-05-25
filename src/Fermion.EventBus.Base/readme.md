# Fermion.EventBus.Base

Fermion.EventBus.Base is a NuGet package that provides the core infrastructure for implementing event-driven architecture in .NET applications. It offers a flexible and extensible foundation for managing integration events.

## Features

- 🚀 Easy integration event management
- 🔄 Asynchronous event processing
- 📦 Flexible configuration options
- 🔍 Event subscription management
- 🛡️ Error handling and monitoring
- 🏗️ Extensible architecture

## Installation

```bash
  dotnet add package Fermion.EventBus.Base
```

## Usage

### 1. Define Your Event

```csharp
public class UserCreatedEvent : IntegrationEvent
{
    public string UserId { get; set; }
    public string UserName { get; set; }
}
```

### 2. Create an Event Handler

```csharp
public class UserCreatedEventHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent @event)
    {
        _logger.LogInformation("User created: {UserId}, {UserName}",
            @event.UserId, @event.UserName);
        await Task.CompletedTask;
    }
}
```

### 3. Configure EventBus

```csharp
// Using the builder pattern
var config = EventBusConfig.CreateBuilder()
    .WithConnectionRetryCount(5)
    .WithDefaultTopicName("DefaultTopicName")
    .WithSubscriberClientAppName(AppDomain.CurrentDomain.FriendlyName)
    .WithEventNamePrefix(string.Empty)
    .WithEventNameSuffix(string.Empty)
    .Build();

// Register in DI container
services.AddSingleton<IEventBus>(sp => 
    new YourEventBusImplementation(config, sp));
```

### 4. Publish and Subscribe to Events

```csharp
// Publish an event
await _eventBus.PublishAsync(new UserCreatedEvent 
{ 
    UserId = "123", 
    UserName = "John Doe" 
});

// Subscribe to an event
await _eventBus.SubscribeAsync<UserCreatedEvent, UserCreatedEventHandler>();
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| ConnectionRetryCount | Number of connection retry attempts | 5 |
| DefaultTopicName | Default topic name for event publishing | "DefaultTopicName" |
| SubscriberClientAppName | Subscriber client application name | Application name |
| EventNamePrefix | Prefix for event names | "" |
| EventNameSuffix | Suffix for event names | "IntegrationEvent" |

## Error Handling

The package supports the following error types:

- `SubscriptionError`: Errors related to subscription management
- `PublishingError`: Errors related to event publishing
- `HandlerExecutionError`: Errors related to event handler execution
- `ConnectionError`: Errors related to event bus connection
- `ConfigurationError`: Errors related to event bus configuration

## Integration with Other Packages

Fermion.EventBus.Base can be extended with additional packages:

- `Fermion.EventBus.RabbitMq`: RabbitMQ implementation
- `Fermion.EventBus.InMemory`: In-memory implementation for testing

Example with RabbitMQ:

```csharp
services.AddEventBusRabbitMq(options =>
{
    options.ConnectionRetryCount = 5;
    options.DefaultTopicName = "DefaultTopicName";
    options.SubscriberClientAppName = AppDomain.CurrentDomain.FriendlyName;
    options.EventNamePrefix = string.Empty;
    options.EventNameSuffix = string.Empty;

    // RabbitMQ connection settings
    options.Host = "localhost";
    options.Port = 5672;
    options.UserName = "admin";
    options.Password = "password";

    // Register event handlers
    options.AddEventHandler<UserCreatedEventHandler>();

    // Add subscriptions
    options.AddSubscription<UserCreatedEvent, UserCreatedEventHandler>();
});
```