using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Redis
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class RedisMQMessageSender : IMessageSender
    {
        public RedisMQMessageSender(
            IOptionsMonitor<EventBusRedisMQOptions> options,
            ILogger<RedisMQMessageSender> logger,
            IMessageSerializer messageSerializer, IServiceProvider serviceProvider)
        {
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            ServiceProvider = serviceProvider;
        }

        private IOptionsMonitor<EventBusRedisMQOptions> Options { get; }
        private ILogger<RedisMQMessageSender> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IServiceProvider ServiceProvider { get; }

        public async Task SendAsync(MessageTransferModel message)
        {
            var redisClient = Options.CurrentValue.RedisClientFactory?.Invoke(ServiceProvider);
            if (redisClient is null)
                throw new InvalidOperationException("invalid redis client factory");
            var maxLen = Options.CurrentValue.MaxLength;
            if (Options.CurrentValue.MaxLengthFactory is not null)
            {
                maxLen = Options.CurrentValue.MaxLengthFactory.Invoke(message);
            }

            var msgId = await redisClient.XAddAsync(message.EventName, maxLen, "*", RedisConstants.StreamBodyFieldName,
                MessageSerializer.Serialize(message));

            Logger.LogDebug($"[EventBus-RedisMQ] send msg success: {message}, msgId: {msgId}");
        }
    }
}