using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息存储 
    /// </summary>
    public interface IMessageStorage
    {
        /// <summary>
        /// 确认事务是否已提交
        /// </summary>
        /// <param name="msgId">消息id</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> TransactionIsCommitted(string msgId, ITransactionContext? transactionContext, CancellationToken cancellationToken);

        /// <summary>
        /// 根据msgId查找发布的消息
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindPublishedByMsgId(string msgId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据msgId查找接收的消息
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="eventHandlerDescriptor"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindReceivedByMsgId(string msgId, EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息,需要写入存储id到message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="transactionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>存储消息id</returns>
        Task<long> SavePublished(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息,需要写入存储id到message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>存储消息id</returns>
        Task<long> SaveReceived(MessageStorageModel message, CancellationToken cancellationToken);

        /// <summary>
        /// 更新已发布消息数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="retryCount"></param>
        /// <param name="expireTime"></param>
        /// <param name="cancellationToken"></param>
        Task UpdatePublished(long id, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// 更新已接收消息数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="retryCount"></param>
        /// <param name="expireTime"></param>
        /// <param name="cancellationToken"></param>
        Task UpdateReceived(long id, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        // /// <summary>
        // /// 更新已发布消息的锁数据
        // /// </summary>
        // /// <param name="msgId"></param>
        // /// <param name="isLocking"></param>
        // /// <param name="lockEnd"></param>
        // /// <returns></returns>
        // Task<bool> TryLockPublished(string msgId, bool isLocking, long lockEnd);

        /// <summary>
        /// 尝试锁定已接收消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lockEndAt">锁定结束时间</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> TryLockReceived(long id, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken);

        /// <summary>
        /// 删除已过期的数据
        /// </summary>
        Task DeleteExpires(CancellationToken cancellationToken);

        /// <summary>
        /// 获取已发布的消息需要重试发送的数据
        /// </summary>
        /// <param name="count"></param>
        /// <param name="delayRetrySecond"></param>
        /// <param name="maxFailedRetryCount"></param>
        /// <param name="environment"></param>
        /// <param name="lockSecond"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond,
            int maxFailedRetryCount, string environment, int lockSecond, CancellationToken cancellationToken);

        /// <summary>
        /// 获取已接收的消息需要重试发送的数据
        /// </summary>
        /// <param name="count"></param>
        /// <param name="delayRetrySecond"></param>
        /// <param name="maxFailedRetryCount"></param>
        /// <param name="environment"></param>
        /// <param name="lockSecond"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond,
            int maxFailedRetryCount, string environment, int lockSecond, CancellationToken cancellationToken);
    }
}