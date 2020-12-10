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
            IOptionsMonitor<EventBusOptions> options, ILogger<DefaultMessageSendQueueProvider> logger, IHostedStopToken hostedStopToken)
        {
            MessageSender = messageSender;
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            HostedStopToken = hostedStopToken;
        }

        private IMessageSender MessageSender { get; }
        private IMessageStorage MessageStorage { get; }
        private IOptionsMonitor<EventBusOptions> Options { get; }
        private ILogger<DefaultMessageSendQueueProvider> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }

        public void Enqueue(ITransactionContext? transactionContext, MessageTransferModel messageTransferModel,
            MessageStorageModel messageStorageModel,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            // ReSharper disable once MethodSupportsCancellation
            Task.Delay(100).ConfigureAwait(false).GetAwaiter().GetResult();

            _ = Task.Run(async () =>
            {
                // 执行失败的次数
                var failCount = 0;
                while (!cancellationToken.IsCancellationRequested && !HostedStopToken.StopCancellationToken.IsCancellationRequested)
                {
                    if (messageStorageModel.CreateTime <= DateTime.Now.AddSeconds(-Options.CurrentValue.ConfirmTransactionSeconds))
                        // 超过时间了,就不管了,状态还是SCHEDULED
                        return;

                    try
                    {
                        // 确保消息已提交才进行消息发送
                        if (!await MessageStorage
                            .TransactionIsCommitted(messageStorageModel.MsgId, transactionContext, cancellationToken)
                            .ConfigureAwait(false))
                        {
                            // 还没提交? 延迟1秒继续查询是否提交
                            // ReSharper disable once MethodSupportsCancellation
                            await Task.Delay(1000).ConfigureAwait(false);
                            continue;
                        }

                        if (failCount > 4)
                            // 最多失败5次就不再重试了,如果消息已经写入那么5分钟后由重试器执行,如果没写入那就撒事也没有
                        {
                            // 消息发送没问题就更新数据库状态
                            try
                            {
                                await MessageStorage.UpdatePublished(
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
                        await MessageSender.Send(messageTransferModel).ConfigureAwait(false);
                        // 消息发送没问题就更新数据库状态
                        await MessageStorage.UpdatePublished(
                                messageStorageModel.Id,
                                MessageStatus.Succeeded,
                                0,
                                DateTime.Now.AddHours(Options.CurrentValue.SucceedExpireHour),
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