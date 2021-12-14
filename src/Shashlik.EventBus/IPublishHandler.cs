using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息发布处理器
    /// </summary>
    public interface IPublishHandler
    {
        /// <summary>
        /// 执行发布操作
        /// </summary>
        /// <param name="messageTransferModel">消息传输模型</param>
        /// <param name="messageStorageModel">消息存储模型</param>
        /// <param name="cancellationToken">取消token</param>
        /// <returns></returns>
        public Task<HandleResult> HandleAsync(
            MessageTransferModel messageTransferModel,
            MessageStorageModel messageStorageModel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行发布操作
        /// </summary>
        /// <param name="id">消息id</param>
        /// <param name="cancellationToken">取消token</param>
        /// <returns></returns>
        public Task<HandleResult> HandleAsync(
            long id,
            CancellationToken cancellationToken = default);
    }
}