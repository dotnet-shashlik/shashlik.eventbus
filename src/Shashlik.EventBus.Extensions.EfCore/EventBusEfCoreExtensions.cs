#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.Extensions.EfCore;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    public static class EventBusEfCoreExtensions
    {
        /// <summary>
        /// 注册event bus  ef core 扩展: 自动包装事务上下文
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <typeparam name="TDbContext"></typeparam>
        /// <returns></returns>
        public static IServiceCollection AddEventBusEfCoreExtensions<TDbContext>(
            this IServiceCollection serviceCollection)
            where TDbContext : DbContext
        {
            serviceCollection.Configure<EventBusEfCoreOptions>(r => { r.DbContextType = typeof(TDbContext); });
            // 这里要用Transient不能用单例
            serviceCollection.AddTransient<IEventPublisher, EfCoreEventPublisher>();

            return serviceCollection;
        }


        /// <summary>
        /// 发布事件，自动包装事务上下文
        /// </summary>
        /// <param name="eventPublisher"></param>
        /// <param name="event"></param>
        /// <param name="items"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public static async Task PublishAsync<TEvent>(
            this IEventPublisher eventPublisher,
            TEvent @event,
            IDictionary<string, string>? items = null
        ) where TEvent : IEvent
        {
            if (eventPublisher is EfCoreEventPublisher efCoreEventPublisher)
            {
                await efCoreEventPublisher.PublishAsync(@event, items).ConfigureAwait(false);
            }

            throw new InvalidCastException($"Make sure invoke AddEventBusEfCoreExtensions<>().");
        }

        /// <summary>
        /// 发布延迟事件，自动包装事务上下文
        /// </summary>
        /// <param name="eventPublisher"></param>
        /// <param name="event"></param>
        /// <param name="delayAt"></param>
        /// <param name="items"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public static async Task PublishAsync<TEvent>(
            this IEventPublisher eventPublisher,
            TEvent @event,
            DateTimeOffset delayAt,
            IDictionary<string, string>? items = null) where TEvent : IDelayEvent
        {
            if (eventPublisher is EfCoreEventPublisher efCoreEventPublisher)
            {
                await efCoreEventPublisher.PublishAsync(@event, delayAt, items).ConfigureAwait(false);
            }

            throw new InvalidCastException($"Make sure invoke AddEventBusEfCoreExtensions<>().");
        }
    }
}