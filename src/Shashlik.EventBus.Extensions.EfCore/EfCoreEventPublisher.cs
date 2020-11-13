#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;

namespace Shashlik.EventBus.Extensions.EfCore
{
    public class EfCoreEventPublisher : DefaultEventPublisher
    {
        public EfCoreEventPublisher(
            IMessageStorage messageStorage,
            IMessageSerializer messageSerializer,
            IEventNameRuler eventNameRuler,
            IOptionsMonitor<EventBusOptions> options,
            IMessageSendQueueProvider messageSendQueueProvider,
            IOptions<EventBusEfCoreOptions> options1,
            IServiceProvider serviceProvider) : base(messageStorage, messageSerializer,
            eventNameRuler, options, messageSendQueueProvider)
        {
            Options = options1;
            ServiceProvider = serviceProvider;
        }

        public IOptions<EventBusEfCoreOptions> Options { get; }
        private IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// 发布事件, 自动从注册的DbContext中获取事务上下文
        /// </summary>
        /// <param name="event"></param>
        /// <param name="items"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public async Task PublishAsync<TEvent>(
            TEvent @event,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            if (!(ServiceProvider.GetRequiredService(Options.Value.DbContextType) is DbContext dbContext))
                throw new InvalidCastException($"Invalid DbContextType of {Options.Value.DbContextType}");

            await base.PublishAsync(@event, new TransactionContext(dbContext), items, cancellationToken);
        }

        /// <summary>
        /// 发布延迟事件, 自动从注册的DbContext中获取事务上下文
        /// </summary>
        /// <param name="event"></param>
        /// <param name="delayAt"></param>
        /// <param name="items"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public async Task PublishAsync<TEvent>(
            TEvent @event,
            DateTimeOffset delayAt,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default) where TEvent : IDelayEvent
        {
            if (!(ServiceProvider.GetRequiredService(Options.Value.DbContextType) is DbContext dbContext))
                throw new InvalidCastException($"Invalid DbContextType of {Options.Value.DbContextType}");

            await base.PublishAsync(@event, new TransactionContext(dbContext), delayAt, items, cancellationToken);
        }
    }
}