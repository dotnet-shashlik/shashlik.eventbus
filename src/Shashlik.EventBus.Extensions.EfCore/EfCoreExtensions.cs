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
        /// <param name="dbContext">DbContext上下文</param>
        /// <param name="event">事件实例</param>
        /// <param name="additionalItems">附加数据</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Can't resolve service of <see cref="IEventPublisher"/></exception>
        /// <exception cref="ArgumentNullException">DbContext/@event can't be null</exception>
        public static async Task PublishEventAsync<TEvent>(
            this DbContext dbContext,
            TEvent @event,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(@event);
            var eventPublisher = dbContext.GetService<IEventPublisher>();
            if (eventPublisher is null)
                throw new InvalidOperationException(
                    $"Can't resolve service type of {typeof(IEventPublisher)} from DbContext {dbContext.GetType()}");

            if (dbContext.Database.CurrentTransaction is null)
                await eventPublisher.PublishAsync(@event, null, additionalItems, cancellationToken).ConfigureAwait(false);
            else
                await eventPublisher.PublishAsync(
                        @event,
                        new RelationDbStorageTransactionContext(dbContext.Database.CurrentTransaction.GetDbTransaction()),
                        additionalItems,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// 通过DbContext发布延迟事件，自动使用DbContext事务上下文和连接信息
        /// </summary>
        /// <param name="dbContext">DbContext上下文</param>
        /// <param name="event">事件实例</param>
        /// <param name="delayAt">延迟执行时间</param>
        /// <param name="additionalItems">附加数据</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Can't resolve service of <see cref="IEventPublisher"/></exception>
        /// <exception cref="ArgumentNullException">DbContext/@event can't be null</exception>
        public static async Task PublishEventAsync<TEvent>(
            this DbContext dbContext,
            TEvent @event,
            DateTimeOffset delayAt,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));
            if (@event is null) throw new ArgumentNullException(nameof(@event));
            var eventPublisher = dbContext.GetService<IEventPublisher>();
            if (eventPublisher is null)
                throw new InvalidOperationException(
                    $"Can't resolve service type of {typeof(IEventPublisher)} from DbContext {dbContext.GetType()}");

            if (dbContext.Database.CurrentTransaction is null)
                await eventPublisher.PublishAsync(@event, delayAt, null, additionalItems, cancellationToken).ConfigureAwait(false);
            else
                await eventPublisher.PublishAsync(
                        @event,
                        delayAt,
                        new RelationDbStorageTransactionContext(dbContext.Database.CurrentTransaction.GetDbTransaction()),
                        additionalItems,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// 从DbContext中获取ITransactionContext
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>事务上下文，如果事务未开启，返回null</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ITransactionContext? GetTransactionContext(this DbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));
            if (dbContext.Database.CurrentTransaction is null)
                return null;

            return new RelationDbStorageTransactionContext(dbContext.Database.CurrentTransaction.GetDbTransaction());
        }
    }
}