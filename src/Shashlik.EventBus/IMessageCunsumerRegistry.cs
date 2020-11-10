using System.Threading;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消费者注册
    /// </summary>
    public interface IMessageCunsumerRegistry
    {
        /// <summary>
        /// 注册消费者
        /// </summary>
        /// <param name="listener">消息监听器</param>
        /// <param name="token">取消token</param>
        void Subscribe(IMessageListener listener, CancellationToken token);
    }
}