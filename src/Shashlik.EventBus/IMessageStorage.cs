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
        /// 消息数据是否已提交
        /// </summary>
        /// <param name="msgId">消息id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> IsCommittedAsync(string msgId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据msgId查找发布的消息
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindPublishedByMsgIdAsync(string msgId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据id查找发布的消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindPublishedByIdAsync(long id, CancellationToken cancellationToken);

        /// <summary>
        /// 根据msgId查找接收的消息
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="eventHandlerDescriptor"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId, EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken);

        /// <summary>
        /// 根据id查找已接收的消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindReceivedByIdAsync(long id, CancellationToken cancellationToken);

        /// <summary>
        /// 查询已发布的消息数据
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="status">状态</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> SearchPublishedAsync(string eventName, string status, int skip, int take,
            CancellationToken cancellationToken);

        /// <summary>
        /// 查询已接收的消息数据
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventHandlerName">事件处理类名称</param>
        /// <param name="status">状态</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> SearchReceived(string eventName, string eventHandlerName, string status, int skip, int take,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息, 自动写入id到message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="transactionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>存储消息id</returns>
        Task<long> SavePublishedAsync(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息, 自动写入id到message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>存储消息id</returns>
        Task<long> SaveReceivedAsync(MessageStorageModel message, CancellationToken cancellationToken);

        /// <summary>
        /// 更新已发布消息数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="retryCount"></param>
        /// <param name="expireTime"></param>
        /// <param name="cancellationToken"></param>
        Task UpdatePublishedAsync(long id, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// 更新已接收消息数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="retryCount"></param>
        /// <param name="expireTime"></param>
        /// <param name="cancellationToken"></param>
        Task UpdateReceivedAsync(long id, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// 尝试锁定已接收消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lockEndAt">锁定结束时间</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> TryLockReceivedAsync(long id, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken);

        /// <summary>
        /// 删除已过期的数据
        /// </summary>
        Task DeleteExpiresAsync(CancellationToken cancellationToken);

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
        Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAndLockAsync(int count, int delayRetrySecond,
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
        Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLockAsync(int count, int delayRetrySecond,
            int maxFailedRetryCount, string environment, int lockSecond, CancellationToken cancellationToken);
    }
}