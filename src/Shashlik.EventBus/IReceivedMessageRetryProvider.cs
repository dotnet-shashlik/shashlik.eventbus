using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public interface IReceivedMessageRetryProvider
    {
        Task DoRetry(CancellationToken cancellationToken);
    }
}