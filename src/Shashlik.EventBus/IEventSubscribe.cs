using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件订阅器
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// 注册事件订阅
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="token">取消token</param>
        Task SubscribeAsync(EventHandlerDescriptor descriptor, CancellationToken token);
    }
}