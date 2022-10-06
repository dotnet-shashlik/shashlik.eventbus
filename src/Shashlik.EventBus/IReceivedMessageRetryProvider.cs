using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public interface IReceivedMessageRetryProvider
    {
        /// <summary>
        /// 启动重试器
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartupAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 重试已接收的消息, 一般用于手动执行某条消息的重试，将忽略重试次数
        /// </summary>
        /// <param name="storageId">存储id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HandleResult> RetryAsync(string storageId, CancellationToken cancellationToken);
    }
}