using System.Threading;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件订阅器
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="listener">消息监听器</param>
        /// <param name="token">取消token</param>
        void Subscribe(IMessageListener listener, CancellationToken token);
    }
}