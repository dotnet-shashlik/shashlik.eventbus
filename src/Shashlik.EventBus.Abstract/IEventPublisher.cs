using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件发布类
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// 普通事件发布
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="transactionContext">事务上下文,null则不使用事务</param>
        /// <param name="additionalItems">附加事件数据</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        Task PublishAsync<TEvent>(
            TEvent @event,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent;

        /// <summary>
        /// 延迟事件发布
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="transactionContext">事务上下文,null则不使用事务</param>
        /// <param name="delayAt">延迟执行时间</param>
        /// <param name="additionalItems">附加事件数据</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        Task PublishAsync<TEvent>(
            TEvent @event,
            DateTimeOffset delayAt,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default)
            where TEvent : IEvent;

        /// <summary>
        /// 普通事件发布[使用事务自动发现机制<see cref="ITransactionContextDiscoverProvider"/>]<para></para>
        /// 默认已注册XA事务发现
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="additionalItems">附加事件数据</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        Task PublishByAutoAsync<TEvent>(
            TEvent @event,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent;

        /// <summary>
        /// 延迟事件发布[使用事务自动发现机制<see cref="ITransactionContextDiscoverProvider"/>]<para></para>
        /// 默认已注册XA事务发现
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="delayAt">延迟执行时间</param>
        /// <param name="additionalItems">附加事件数据</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        Task PublishByAutoAsync<TEvent>(
            TEvent @event,
            DateTimeOffset delayAt,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default)
            where TEvent : IEvent;
    }
}