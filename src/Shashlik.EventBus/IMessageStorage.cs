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
        /// 发布消息是否存在
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> ExistsPublishMessage(string msgId, CancellationToken cancellationToken);

        /// <summary>
        /// 接收的消息是否存在
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> ExistsReceiveMessage(string msgId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据id查找发布的消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindPublishedById(string id, CancellationToken cancellationToken);

        /// <summary>
        /// 根据id查找接收的消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindReceivedById(string id, CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="transactionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SavePublished(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveReceived(MessageStorageModel message, CancellationToken cancellationToken);

        /// <summary>
        /// 更新已发布消息数据
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="status"></param>
        /// <param name="retryCount"></param>
        /// <param name="expireTime"></param>
        /// <param name="cancellationToken"></param>
        Task UpdatePublished(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// 更新已接收消息数据
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="status"></param>
        /// <param name="retryCount"></param>
        /// <param name="expireTime"></param>
        /// <param name="cancellationToken"></param>
        Task UpdateReceived(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
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
        /// 更新已接收消息的锁数据
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="lockEndAt">锁定结束时间</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> TryLockReceived(string msgId, DateTimeOffset lockEndAt, CancellationToken cancellationToken);

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