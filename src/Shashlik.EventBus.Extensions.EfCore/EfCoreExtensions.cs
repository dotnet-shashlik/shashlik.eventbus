using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.Extensions.EfCore;
using Shashlik.EventBus.RelationDbStorage;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    public static class EfCoreExtensions
    {
        /// <summary>
        /// 使用 FreeSql 跨方言实现关系型数据库存储(MySQL/PG/SqlServer/Sqlite/...)。
        /// 应用层 ORM 自由(EF Core / Dapper / NHibernate 都能用),
        /// 只要能拿到 <see cref="System.Data.IDbTransaction"/> 即可包装成
        /// <see cref="ITransactionContext"/> 参与 EventBus 事务。
        /// </summary>
        public static IEventBusBuilder AddRelationDb<T>(
            this IEventBusBuilder eventBusBuilder, DataType dataType,
            Action<EventBusRelationDbOptions>? optionsAction = null)
            where T : DbContext
        {
            eventBusBuilder.Services.AddSingleton<EfCoreConnectionFactory>();
            eventBusBuilder.Services.AddSingleton<IConnectionFactory, EfCoreConnectionFactory>();
            eventBusBuilder.Services.AddOptions<EventBusEfCoreOptions>().Configure(r =>
            {
                r.DbContextType = typeof(T);
                r.DataType = dataType;
            });

            eventBusBuilder.AddRelationDb(opts =>
            {
                optionsAction?.Invoke(opts);
                opts.UseConnection(typeof(EfCoreConnectionFactory));
            });
            return eventBusBuilder;
        }


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
                await eventPublisher.PublishAsync(@event, null, additionalItems, cancellationToken)
                    .ConfigureAwait(false);
            else
                await eventPublisher.PublishAsync(
                        @event,
                        dbContext.GetTransactionContext(),
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
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(@event);
            var eventPublisher = dbContext.GetService<IEventPublisher>();
            if (eventPublisher is null)
                throw new InvalidOperationException(
                    $"Can't resolve service type of {typeof(IEventPublisher)} from DbContext {dbContext.GetType()}");

            if (dbContext.Database.CurrentTransaction is null)
                await eventPublisher.PublishAsync(@event, delayAt, null, additionalItems, cancellationToken)
                    .ConfigureAwait(false);
            else
                await eventPublisher.PublishAsync(
                        @event,
                        delayAt,
                        dbContext.GetTransactionContext(),
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