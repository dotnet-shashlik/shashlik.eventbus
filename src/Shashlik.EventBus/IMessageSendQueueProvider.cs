using System.Threading;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息发送处理队列
    /// </summary>
    public interface IMessageSendQueueProvider
    {
        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="transactionContext">事务上下文信息</param>
        /// <param name="messageTransferModel">消息传输抹胸</param>
        /// <param name="messageStorageModel">消息存储模型</param>
        /// <param name="cancellationToken"></param>
        void Enqueue(
            ITransactionContext? transactionContext,
            MessageTransferModel messageTransferModel,
            MessageStorageModel messageStorageModel,
            CancellationToken cancellationToken);
    }
}