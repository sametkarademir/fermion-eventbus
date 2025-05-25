# Fermion.EventBus.InMemory

Fermion.EventBus.InMemory is a NuGet package that provides an in-memory implementation of the event bus for .NET applications. It's designed for testing and single-instance applications where a distributed message broker is not required.

## Features

- 🚀 In-memory event processing using channels
- 🔄 Asynchronous event handling
- 📦 Easy integration with dependency injection
- 🔍 Automatic subscription management
- 🛡️ Error handling and logging
- 🏗️ Extensible architecture

## Installation

```bash
  dotnet add package Fermion.EventBus.InMemory
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
services.AddEventBusInMemory(options =>
{
    options.SubscriberClientAppName = AppDomain.CurrentDomain.FriendlyName;
    options.EventNamePrefix = string.Empty;
    options.EventNameSuffix = string.Empty;

    // Register event handlers
    options.AddEventHandler<UserCreatedEventHandler>();

    // Add subscriptions
    options.AddSubscription<UserCreatedEvent, UserCreatedEventHandler>();
});
```

### 4. Publish and Subscribe to Events

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

        await _eventBus.PublishAsync(new UserCreatedEvent 
        { 
            UserId = "123", 
            UserName = "John Doe" 
        });

        return Ok();
    }
}
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| SubscriberClientAppName | Subscriber client application name | Application name |
| EventNamePrefix | Prefix for event names | "" |
| EventNameSuffix | Suffix for event names | "" |

## Event Handler Registration

The package provides two methods for registering event handlers:

1. Using `AddEventHandler<T>()`:
   ```csharp
   options.AddEventHandler<UserCreatedEventHandler>();
   ```

2. Using `AddSubscription<TEvent, THandler>()`:
   ```csharp
   options.AddSubscription<UserCreatedEvent, UserCreatedEventHandler>();
   ```

## Error Handling

The in-memory event bus includes built-in error handling and logging:

- Event processing errors are caught and logged
- Failed event processing doesn't affect other events
- Detailed error information is available in logs

## Best Practices

1. Use for testing and single-instance applications
2. Register all event handlers during startup
3. Implement proper error handling in event handlers
4. Use logging for monitoring and debugging
5. Consider using the distributed event bus for production environments