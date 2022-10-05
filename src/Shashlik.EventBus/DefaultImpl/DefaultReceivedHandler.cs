using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus.Utils;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultReceivedHandler : IReceivedHandler
    {
        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultReceivedHandler> Logger { get; }
        private IEventHandlerInvoker EventHandlerInvoker { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IMessageSerializer MessageSerializer { get; }

        public DefaultReceivedHandler(IMessageStorage messageStorage, IOptions<EventBusOptions> options,
            ILogger<DefaultReceivedHandler> logger, IEventHandlerInvoker eventHandlerInvoker,
            IEventHandlerFindProvider eventHandlerFindProvider, IMessageSerializer messageSerializer)
        {
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            EventHandlerInvoker = eventHandlerInvoker;
            EventHandlerFindProvider = eventHandlerFindProvider;
            MessageSerializer = messageSerializer;
        }

        public async Task<HandleResult> HandleAsync(MessageStorageModel messageStorageModel,
            IDictionary<string, string> items,
            EventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        {
            return await HandleAsync(messageStorageModel.Id, messageStorageModel, items, descriptor, false,
                cancellationToken);
        }

        public async Task<HandleResult> HandleAsync(long id, CancellationToken cancellationToken = default)
        {
            return await HandleAsync(id, null, null, null, true, cancellationToken);
        }

        private async Task<HandleResult> HandleAsync(
            long id,
            MessageStorageModel? messageStorageModel,
            IDictionary<string, string>? items,
            EventHandlerDescriptor? descriptor,
            bool requireLock,
            CancellationToken cancellationToken)
        {
            try
            {
                // 尝试锁定
                if (!requireLock || await MessageStorage.TryLockReceivedAsync(
                        id, DateTimeOffset.Now.AddSeconds(Options.Value.LockTime),
                        cancellationToken).ConfigureAwait(false)
                   )
                {
                    messageStorageModel ??= await MessageStorage.FindReceivedByIdAsync(id, cancellationToken);
                    if (messageStorageModel is null)
                    {
                        Logger.LogDebug($"[EventBus] published message \"{id}\" not found");
                        return new HandleResult(true);
                    }

                    descriptor ??= EventHandlerFindProvider.GetByName(messageStorageModel.EventHandlerName);
                    if (descriptor is null)
                    {
                        Logger.LogWarning(
                            $"[EventBus] can not found event handler: {messageStorageModel.EventHandlerName}, but receive msg: {messageStorageModel.EventBody}");
                        return new HandleResult(true);
                    }

                    items ??= MessageSerializer.Deserialize<IDictionary<string, string>>(messageStorageModel.EventItems)
                              ?? new Dictionary<string, string>();

                    // 执行事件消费
                    await EventHandlerInvoker.InvokeAsync(messageStorageModel, items, descriptor).ConfigureAwait(false);
                    // 消息处理没问题就更新数据库状态
                    await MessageStorage.UpdateReceivedAsync(
                            messageStorageModel.Id,
                            MessageStatus.Succeeded,
                            ++messageStorageModel.RetryCount,
                            DateTimeOffset.Now.AddHours(Options.Value.SucceedExpireHour),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return new HandleResult(true);
            }
            catch (Exception ex)
            {
                if (messageStorageModel is null || descriptor is null)
                {
                    Logger.LogError(ex, $"[EventBus] {ex.Message}");
                    return new HandleResult(true);
                }

                try
                {
                    await MessageStorage.UpdateReceivedAsync(
                            messageStorageModel.Id,
                            MessageStatus.Failed,
                            ++messageStorageModel.RetryCount,
                            null,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex1)
                {
                    Logger.LogError(ex1, "[EventBus] update received message error");
                }

                Logger.LogError(ex,
                    $"[EventBus] message receive occur error, will try again later, event: {descriptor.EventName}, handler: {descriptor.EventHandlerName}, msgId: {messageStorageModel.MsgId}");

                return new HandleResult(false, messageStorageModel);
            }
        }
    }
}