using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息接收处理器
    /// </summary>
    public interface IReceivedHandler
    {
        /// <summary>
        /// 事件处理执行,不管锁状态
        /// </summary>
        /// <param name="messageStorageModel">消息存储模型</param>
        /// <param name="items">附加数据</param>
        /// <param name="descriptor">事件处理描述器</param>
        /// <param name="cancellationToken">取消token</param>
        public Task<HandleResult> HandleAsync(
            MessageStorageModel messageStorageModel,
            IDictionary<string, string> items,
            EventHandlerDescriptor descriptor,
            CancellationToken cancellationToken);

        /// <summary>
        /// 锁定数据并事件处理执行
        /// </summary>
        /// <param name="storageId"></param>
        /// <param name="cancellationToken">取消token</param>
        /// <returns></returns>
        public Task<HandleResult> LockingHandleAsync(
            string storageId,
            CancellationToken cancellationToken = default);
    }
}