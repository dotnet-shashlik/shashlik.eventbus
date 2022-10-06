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
        /// <param name="msgId">消息id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindPublishedByMsgIdAsync(string msgId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据id查找发布的消息
        /// </summary>
        /// <param name="storageId">消息存储id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindPublishedByIdAsync(string storageId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据msgId查找接收的消息
        /// </summary>
        /// <param name="msgId">消息id</param>
        /// <param name="eventHandlerDescriptor">事件处理类描述</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId, EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken);

        /// <summary>
        /// 根据id查找已接收的消息
        /// </summary>
        /// <param name="storageId">消息存储id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MessageStorageModel?> FindReceivedByIdAsync(string storageId, CancellationToken cancellationToken);

        /// <summary>
        /// 查询已发布的消息数据
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="status">状态</param>
        /// <param name="skip">skip</param>
        /// <param name="take">take</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> SearchPublishedAsync(string? eventName, string? status, int skip, int take,
            CancellationToken cancellationToken);

        /// <summary>
        /// 查询已接收的消息数据
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventHandlerName">事件处理类名称</param>
        /// <param name="status">状态</param>
        /// <param name="skip">skip</param>
        /// <param name="take">take</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> SearchReceived(string? eventName, string? eventHandlerName, string? status,
            int skip, int take,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息, 自动写入id到message
        /// </summary>
        /// <param name="message">消息存储模型</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="cancellationToken"></param>
        /// <returns>存储消息id</returns>
        Task<string> SavePublishedAsync(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// 保存发布消息, 自动写入id到message
        /// </summary>
        /// <param name="message">消息存储模型</param>
        /// <param name="cancellationToken"></param>
        /// <returns>存储消息id</returns>
        Task<string> SaveReceivedAsync(MessageStorageModel message, CancellationToken cancellationToken);

        /// <summary>
        /// 更新已发布消息数据
        /// </summary>
        /// <param name="storageId">消息存储id</param>
        /// <param name="status">消息处理状态</param>
        /// <param name="retryCount">已重试次数</param>
        /// <param name="expireTime">过期时间</param>
        /// <param name="cancellationToken"></param>
        Task UpdatePublishedAsync(string storageId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// 更新已接收消息数据
        /// </summary>
        /// <param name="storageId">消息存储id</param>
        /// <param name="status">消息处理状态</param>
        /// <param name="retryCount">已重试次数</param>
        /// <param name="expireTime">过期时间</param>
        /// <param name="cancellationToken"></param>
        Task UpdateReceivedAsync(string storageId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// 尝试锁定已接收消息
        /// </summary>
        /// <param name="storageId">消息存储id</param>
        /// <param name="lockEndAt">锁定结束时间</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> TryLockPublishedAsync(string storageId, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken);

        /// <summary>
        /// 尝试锁定已接收消息
        /// </summary>
        /// <param name="storageId">消息存储id</param>
        /// <param name="lockEndAt">锁定结束时间</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken);

        /// <summary>
        /// 删除已过期的数据
        /// </summary>
        Task DeleteExpiresAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 获取已发布的消息需要重试发送的数据
        /// </summary>
        /// <param name="count">获取数量</param>
        /// <param name="delayRetrySecond">重试延迟时间</param>
        /// <param name="maxFailedRetryCount">最大重试次数</param>
        /// <param name="environment">环境变量</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(int count, int delayRetrySecond,
            int maxFailedRetryCount, string environment, CancellationToken cancellationToken);

        /// <summary>
        /// 获取已接收的消息需要重试发送的数据
        /// </summary>
        /// <param name="count">获取数量</param>
        /// <param name="delayRetrySecond">重试延迟时间</param>
        /// <param name="maxFailedRetryCount">最大重试次数</param>
        /// <param name="environment">环境变量</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(int count, int delayRetrySecond,
            int maxFailedRetryCount, string environment, CancellationToken cancellationToken);

        /// <summary>
        /// 获取已发布的消息各种状态的数量
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Dictionary<string, int>> GetPublishedMessageStatusCountsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 获取已接收的消息各种状态的数量
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Dictionary<string, int>> GetReceivedMessageStatusCountAsync(CancellationToken cancellationToken);
    }
}