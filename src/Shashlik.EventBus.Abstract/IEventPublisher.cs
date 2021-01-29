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
        /// <param name="transactionContext">事务和连接信息</param>
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
        /// <param name="transactionContext">事务和连接信息</param>
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
    }
}