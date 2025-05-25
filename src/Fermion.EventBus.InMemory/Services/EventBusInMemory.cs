using System.Threading.Channels;
using Fermion.EventBus.Base;
using Fermion.EventBus.Base.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fermion.EventBus.InMemory.Services;

/// <summary>
/// In-memory implementation of the event bus that uses channels for event processing.
/// This implementation is suitable for testing and single-instance applications.
/// </summary>
public class EventBusInMemory : BaseEventBus
{
    private readonly ILogger<EventBusInMemory> _logger;
    private readonly Dictionary<string, Channel<IntegrationEvent>> _eventChannels;
    private readonly Dictionary<string, List<Task>> _eventHandlers;
    private readonly Dictionary<string, bool> _isProcessing;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusInMemory"/> class.
    /// </summary>
    /// <param name="config">The event bus configuration.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is not registered in the service provider.</exception>
    public EventBusInMemory(
        EventBusConfig config,
        IServiceProvider serviceProvider)
        : base(config, serviceProvider)
    {
        _logger = serviceProvider.GetService<ILogger<EventBusInMemory>>() ?? throw new ArgumentNullException(nameof(ILogger<EventBusInMemory>));
        _eventChannels = new Dictionary<string, Channel<IntegrationEvent>>();
        _eventHandlers = new Dictionary<string, List<Task>>();
        _isProcessing = new Dictionary<string, bool>();
    }

    /// <summary>
    /// Publishes an integration event to the in-memory channel.
    /// </summary>
    /// <param name="event">The integration event to publish.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        _logger.LogInformation("Publishing event: {EventName}", eventName);

        eventName = ProcessEventName(eventName);
        if (_eventChannels.TryGetValue(eventName, out var channel))
        {
            channel.Writer.TryWrite(@event);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of the integration event.</typeparam>
    /// <typeparam name="TH">The type of the event handler.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task SubscribeAsync<T, TH>()
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Subscribing to event: {EventName}", eventName);

        eventName = ProcessEventName(eventName);
        SubsManager.AddSubscription<T, TH>();
        StartEventProcessing<T>(eventName);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Unsubscribes from an integration event with a specific handler.
    /// </summary>
    /// <typeparam name="T">The type of the integration event.</typeparam>
    /// <typeparam name="TH">The type of the event handler.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task UnSubscribeAsync<T, TH>()
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Unsubscribing from event: {EventName}", eventName);

        eventName = ProcessEventName(eventName);
        SubsManager.RemoveSubscription<T, TH>();
        StopEventProcessing(eventName);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Starts processing events for a specific event type.
    /// </summary>
    /// <typeparam name="T">The type of the integration event.</typeparam>
    /// <param name="eventName">The name of the event.</param>
    private void StartEventProcessing<T>(string eventName) where T : IntegrationEvent
    {
        if (!_eventHandlers.TryGetValue(eventName, out var value))
        {
            value = [];
            _eventHandlers[eventName] = value;
            _isProcessing[eventName] = true;

            _eventChannels[eventName] = Channel.CreateUnbounded<IntegrationEvent>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
        }

        var task = Task.Run(async () =>
        {
            try
            {
                var channel = _eventChannels[eventName];
                await foreach (var @event in channel.Reader.ReadAllAsync())
                {
                    if (!_isProcessing[eventName])
                    {
                        break;
                    }

                    try
                    {
                        _logger.LogInformation("Processing event: {EventName}", eventName);
                        await ProcessEvent(eventName, JsonConvert.SerializeObject(@event));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event: {EventName}", eventName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in event processing for: {EventName}", eventName);
            }
        });
        value.Add(task);
    }

    /// <summary>
    /// Stops processing events for a specific event type.
    /// </summary>
    /// <param name="eventName">The name of the event to stop processing.</param>
    private void StopEventProcessing(string eventName)
    {
        if (!_eventHandlers.TryGetValue(eventName, out var value))
        {
            return;
        }

        if (_isProcessing.ContainsKey(eventName))
        {
            _isProcessing[eventName] = false;
        }

        if (_eventChannels.TryGetValue(eventName, out var channel))
        {
            channel.Writer.Complete();
        }

        foreach (var task in value)
        {
            if (task.IsCompleted)
            {
                continue;
            }

            _logger.LogInformation("Stopping event processing for: {EventName}", eventName);
            try
            {
                if (!task.Wait(TimeSpan.FromSeconds(5)))
                {
                    _logger.LogWarning("Task did not complete within timeout for: {EventName}", eventName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping event processing for: {EventName}", eventName);
            }
        }

        _eventHandlers.Remove(eventName);
        _isProcessing.Remove(eventName);
        _eventChannels.Remove(eventName);
    }

    /// <summary>
    /// Disposes the event bus and cleans up all resources.
    /// </summary>
    public override void Dispose()
    {
        foreach (var eventName in _eventHandlers.Keys.ToList())
        {
            StopEventProcessing(eventName);
        }

        _eventChannels.Clear();
        _eventHandlers.Clear();
        _isProcessing.Clear();

        base.Dispose();
    }
}