using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultMessageSendQueueProvider : IMessageSendQueueProvider
    {
        public DefaultMessageSendQueueProvider(IMessageSender messageSender, IMessageStorage messageStorage,
            IOptions<EventBusOptions> options, ILogger<DefaultMessageSendQueueProvider> logger, IHostedStopToken hostedStopToken)
        {
            MessageSender = messageSender;
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            HostedStopToken = hostedStopToken;
        }

        private IMessageSender MessageSender { get; }
        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultMessageSendQueueProvider> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }

        public void Enqueue(
            ITransactionContext? transactionContext,
            MessageTransferModel messageTransferModel,
            MessageStorageModel messageStorageModel,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            _ = Task.Run(async () =>
            {
                // 等待事务完成，循环间隔10毫秒
                // 存在事务上下文并且没完成，一直等待事务完成
                //TODO: 加入事务提交超时机制
                while (!cancellationToken.IsCancellationRequested && transactionContext != null && !transactionContext.IsDone())
                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(10);

                try
                {
                    // 消息未提交, 不执行任何操作
                    if (!await MessageStorage
                        .IsCommittedAsync(messageStorageModel.MsgId, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        Logger.LogDebug($"[EventBus] message \"{messageStorageModel.MsgId}\" has been rollback.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, $"[EventBus] query message \"{messageStorageModel.MsgId}\" commit state occur error.");
                    // 查询异常，将由重试器处理
                    return;
                }

                // 执行失败的次数
                var failCount = 0;
                while (!cancellationToken.IsCancellationRequested && !HostedStopToken.StopCancellationToken.IsCancellationRequested)
                {
                    if (messageStorageModel.CreateTime <= DateTime.Now.AddSeconds(-Options.Value.ConfirmTransactionSeconds))
                        // 超过时间了,就不管了,状态还是SCHEDULED
                        return;

                    try
                    {
                        if (failCount > 4)
                            // 最多失败5次就不再重试了,如果消息已经写入那么5分钟后由重试器执行,如果没写入那就撒事也没有
                        {
                            // 消息发送没问题就更新数据库状态
                            try
                            {
                                await MessageStorage.UpdatePublishedAsync(
                                        messageStorageModel.Id,
                                        MessageStatus.Failed,
                                        failCount,
                                        null,
                                        cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, $"[EventBus] update published message error.");
                            }

                            return;
                        }

                        // 这里可能存在的是消息发送成功,数据库更新失败,那么就可能存在重复发送的情况,这个需要消费方自行冥等处理
                        // 事务已提交,执行消息发送和更新状态
                        await MessageSender.SendAsync(messageTransferModel).ConfigureAwait(false);
                        // 消息发送没问题就更新数据库状态
                        await MessageStorage.UpdatePublishedAsync(
                                messageStorageModel.Id,
                                MessageStatus.Succeeded,
                                0,
                                DateTime.Now.AddHours(Options.Value.SucceedExpireHour),
                                cancellationToken)
                            .ConfigureAwait(false);

                        return;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Logger.LogError(ex,
                            $"[EventBus] message publish error, will try again later, event: {messageStorageModel.EventName},  msgId: {messageStorageModel.MsgId}.");
                    }
                }
            }, HostedStopToken.StopCancellationToken).ConfigureAwait(false);
        }
    }
}