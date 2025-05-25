using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Fermion.EventBus.Base;
using Fermion.EventBus.Base.Events;
using Fermion.EventBus.Base.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fermion.EventBus.AzureServiceBus.Services;

public class EventBusServiceBus : BaseEventBus
{
    private ServiceBusSender? _sender;
    private readonly ServiceBusAdministrationClient _administrationClient;
    private readonly ILogger _logger;
    private readonly string? _connectionString;

    public EventBusServiceBus(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
    {
        _logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>
            ?? throw new ArgumentNullException(nameof(ILogger<EventBusServiceBus>));

        if (config.Connection == null)
        {
            throw new EventBusException("Connection cannot be null",
                "unknown",
                "unknown",
                EventBusErrorTypes.ConnectionError);
        }
        _connectionString = config.Connection.ToString();
        _administrationClient = new ServiceBusAdministrationClient(_connectionString);
        var client = new ServiceBusClient(_connectionString);
        _sender = client.CreateSender(EventBusConfig.DefaultTopicName);
    }

    private async Task<ServiceBusSender> CreateSenderAsync()
    {
        if (_sender == null)
        {
            var client = new ServiceBusClient(_connectionString);
            _sender = client.CreateSender(EventBusConfig.DefaultTopicName);
        }

        if (!await _administrationClient.TopicExistsAsync(EventBusConfig.DefaultTopicName))
        {
            await _administrationClient.CreateTopicAsync(EventBusConfig.DefaultTopicName);
        }

        return _sender;
    }

    private ServiceBusProcessor CreateProcessor(string eventName)
    {
        var client = new ServiceBusClient(_connectionString);
        return client.CreateProcessor(EventBusConfig.DefaultTopicName, GetSubName(eventName));
    }

    private async Task<ServiceBusProcessor> CreateProcessorIfNotExistsAsync(string eventName)
    {
        var exists = await _administrationClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName));
        if (!exists)
        {
            await _administrationClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName));
            await RemoveDefaultRuleAsync(GetSubName(eventName));
        }

        await CreateRuleIfNotExistsAsync(ProcessEventName(eventName), GetSubName(eventName));
        return CreateProcessor(eventName);
    }

    private async Task RemoveDefaultRuleAsync(string subscriptionName)
    {
        try
        {
            await _administrationClient.DeleteRuleAsync(EventBusConfig.DefaultTopicName, subscriptionName, "$Default");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "The messaging entity {DefaultTopicName} could not be found.", "$Default");
        }
    }

    private async Task CreateRuleIfNotExistsAsync(string eventName, string subscriptionName)
    {
        bool ruleExists;
        try
        {
            var rule = await _administrationClient.GetRuleAsync(EventBusConfig.DefaultTopicName, subscriptionName, eventName);
            ruleExists = rule != null;
        }
        catch (Exception)
        {
            ruleExists = false;
        }
        if (!ruleExists)
        {
            var ruleOptions = new CreateRuleOptions
            {
                Name = eventName,
                Filter = new CorrelationRuleFilter { Subject = eventName }
            };
            await _administrationClient.CreateRuleAsync(EventBusConfig.DefaultTopicName, subscriptionName, ruleOptions);
        }
    }

    private Task RegisterProcessorMessageHandlerAsync(ServiceBusProcessor processor)
    {
        processor.ProcessMessageAsync += async args =>
        {
            var eventName = args.Message.Subject;
            var messageData = Encoding.UTF8.GetString(args.Message.Body);

            if (await ProcessEvent(ProcessEventName(eventName), messageData))
            {
                await args.CompleteMessageAsync(args.Message);
            }
        };

        processor.ProcessErrorAsync += async args =>
        {
            _logger.LogError(args.Exception, "Error processing message: {Error}", args.Exception.Message);
            await Task.CompletedTask;
        };

        return processor.StartProcessingAsync();
    }

    public override void Dispose()
    {
        base.Dispose();
        _sender?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _sender = null;
    }

    public override async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        var eventStr = JsonConvert.SerializeObject(@event);
        var bodyArr = Encoding.UTF8.GetBytes(eventStr);

        var message = new ServiceBusMessage(bodyArr)
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = eventName
        };

        await _sender?.SendMessageAsync(message)!;
    }

    public override async Task SubscribeAsync<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionForEvent(eventName))
        {
            var processor = await CreateProcessorIfNotExistsAsync(eventName);
            await RegisterProcessorMessageHandlerAsync(processor);
        }

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
        SubsManager.AddSubscription<T, TH>();
    }

    public override async Task UnSubscribeAsync<T, TH>()
    {
        var eventName = typeof(T).Name;
        try
        {
            await _administrationClient.DeleteRuleAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName), eventName);
        }
        catch (Exception)
        {
            _logger.LogWarning("The messaging entity {eventName} Could not be found", eventName);
        }

        _logger.LogInformation("Unsibscribing from event {EventName}", eventName);
        SubsManager.RemoveSubscription<T, TH>();
    }
}