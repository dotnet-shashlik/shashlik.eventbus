using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Shashlik.EventBus.StackExchangeRedis;

/// <summary>
/// 消息发送处理类
/// </summary>
public class StackExchangeRedisMQMessageSender : IMessageSender
{
    public StackExchangeRedisMQMessageSender(
        IOptionsMonitor<EventBusStackExchangeRedisOptions> options,
        ILogger<StackExchangeRedisMQMessageSender> logger,
        IMessageSerializer messageSerializer,
        IServiceProvider serviceProvider)
    {
        Options = options;
        Logger = logger;
        MessageSerializer = messageSerializer;
        ServiceProvider = serviceProvider;
    }

    private IOptionsMonitor<EventBusStackExchangeRedisOptions> Options { get; }
    private ILogger<StackExchangeRedisMQMessageSender> Logger { get; }
    private IMessageSerializer MessageSerializer { get; }
    private IServiceProvider ServiceProvider { get; }

        public async Task SendAsync(MessageTransferModel message)
        {
            var connection = Options.CurrentValue.ConnectionMultiplexerFactory?.Invoke(ServiceProvider)
                ?? throw new InvalidOperationException("EventBusStackExchangeRedisOptions.ConnectionMultiplexerFactory error");
            var database = connection.GetDatabase();

        var maxLen = Options.CurrentValue.MaxLength;
        if (Options.CurrentValue.MaxLengthFactory is not null)
        {
            maxLen = Options.CurrentValue.MaxLengthFactory.Invoke(message);
        }

        var msgId = await database.StreamAddAsync(
            message.EventName,
            StackExchangeRedisConstants.StreamBodyFieldName,
            MessageSerializer.Serialize(message),
            maxLength: maxLen,
            useApproximateMaxLength: true);

        Logger.LogDebug($"[EventBus-StackExchangeRedis] send msg success: {message}, msgId: {msgId}");
    }
}
