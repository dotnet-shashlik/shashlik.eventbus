using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已发送的消息重试提供类
    /// </summary>
    public interface IPublishedMessageRetryProvider
    {
        Task DoRetry(CancellationToken cancellationToken);
    }
}