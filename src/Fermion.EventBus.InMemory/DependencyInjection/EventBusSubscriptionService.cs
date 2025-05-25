using Fermion.EventBus.Base.Abstraction;
using Microsoft.Extensions.Hosting;

namespace Fermion.EventBus.InMemory.DependencyInjection;

/// <summary>
/// A hosted service that manages event bus subscriptions during application startup.
/// </summary>
internal class EventBusSubscriptionService : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly List<(Type EventType, Type HandlerType)> _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusSubscriptionService"/> class.
    /// </summary>
    /// <param name="eventBus">The event bus instance.</param>
    /// <param name="subscriptions">The list of event and handler type pairs to subscribe.</param>
    public EventBusSubscriptionService(
        IEventBus eventBus,
        List<(Type EventType, Type HandlerType)> subscriptions)
    {
        _eventBus = eventBus;
        _subscriptions = subscriptions;
    }

    /// <summary>
    /// Starts the subscription service and subscribes to all registered events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var (eventType, handlerType) in _subscriptions)
        {
            var subscribeMethod = typeof(IEventBus)
                .GetMethod(nameof(IEventBus.SubscribeAsync))
                ?.MakeGenericMethod(eventType, handlerType);

            if (subscribeMethod != null)
            {
                await (Task)subscribeMethod.Invoke(_eventBus, [])!;
            }
        }
    }

    /// <summary>
    /// Stops the subscription service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}