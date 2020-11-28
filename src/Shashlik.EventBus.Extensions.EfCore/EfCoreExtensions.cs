#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Shashlik.EventBus.RelationDbStorage;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    public static class EfCoreExtensions
    {
        /// <summary>
        /// 通过DbContext发布事件，自动使用DbContext事务上下文和连接信息
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="event"></param>
        /// <param name="items"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task PublishEventAsync<TEvent>(
            this DbContext dbContext,
            TEvent @event,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            var eventPublisher = dbContext.GetService<IEventPublisher>();
            if (eventPublisher is null)
                throw new InvalidOperationException($"Can't resolve service type of {typeof(IEventPublisher)} from DbContext {dbContext.GetType()}");

            if (dbContext.Database.CurrentTransaction is null)
                await eventPublisher.PublishAsync(@event, null, items, cancellationToken).ConfigureAwait(false);
            else
                await eventPublisher.PublishAsync(
                        @event,
                        new RelationDbStorageTransactionContext(dbContext.Database.CurrentTransaction.GetDbTransaction()),
                        items,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// 通过DbContext发布延迟事件，自动使用DbContext事务上下文和连接信息
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="event"></param>
        /// <param name="delayAt"></param>
        /// <param name="items"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public static async Task PublishEventAsync<TEvent>(
            this DbContext dbContext,
            TEvent @event,
            DateTimeOffset delayAt,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default) where TEvent : IDelayEvent
        {
            var eventPublisher = dbContext.GetService<IEventPublisher>();
            if (eventPublisher is null)
                throw new InvalidOperationException($"Can't resolve service type of {typeof(IEventPublisher)} from DbContext {dbContext.GetType()}");

            if (dbContext.Database.CurrentTransaction is null)
                await eventPublisher.PublishAsync(@event, delayAt, null, items, cancellationToken).ConfigureAwait(false);
            else
                await eventPublisher.PublishAsync(
                        @event,
                        delayAt,
                        new RelationDbStorageTransactionContext(dbContext.Database.CurrentTransaction.GetDbTransaction()),
                        items,
                        cancellationToken)
                    .ConfigureAwait(false);
        }
    }
}