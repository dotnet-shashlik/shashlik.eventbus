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
        Task Startup(CancellationToken cancellationToken);

        /// <summary>
        /// 重试已接收的消息
        /// </summary>
        /// <param name="msgId">消息id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Retry(string msgId, CancellationToken cancellationToken);
    }
}