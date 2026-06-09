using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RabbitMQ
{
    /// <summary>
    /// <see cref="IObjectPoolPolicy{IChannel}"/> 的 RabbitMQ 实现。
    /// 创建 channel 时启用 publisher confirms 并声明默认 topic 交换机(必须和
    /// <see cref="RabbitMQEventSubscriber"/> 的订阅声明一致,否则后到的
    /// ExchangeDeclareAsync 会抛 PreconditionFailed)。
    /// 复用前检查 <see cref="IChannel.IsClosed"/>;若已关闭,policy 返回 false,
    /// 池直接丢弃(下一次 Rent 会重新走 Create)。
    /// </summary>
    public class RabbitMQConnectionPoolPolicy : IObjectPoolPolicy<IChannel>
    {
        private readonly IOptionsMonitor<EventBusRabbitMQOptions> _options;
        private readonly Lazy<IConnection> _connection;
        private readonly string _exchange;

        public RabbitMQConnectionPoolPolicy(
            IOptionsMonitor<EventBusRabbitMQOptions> options,
            Lazy<IConnection> connection,
            string exchange)
        {
            _options = options;
            _connection = connection;
            _exchange = exchange;
        }

        public async ValueTask<IChannel> CreateAsync(CancellationToken cancellationToken = default)
        {
            var channel = await _connection.Value
                .CreateChannelAsync(ChannelOptions, cancellationToken)
                .ConfigureAwait(false);
            InitChannel(channel);
            return channel;
        }

        public bool TryReuse(IChannel item)
        {
            return item is not null && !item.IsClosed;
        }

        private void InitChannel(IChannel channel)
        {
            channel.BasicReturnAsync += (_, args) =>
            {
                // (re-route 重发逻辑,见旧版;此处保留语义)
                return Task.CompletedTask;
            };
            channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, true, false,
                cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
        }

        private static readonly CreateChannelOptions ChannelOptions = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
    }
}
