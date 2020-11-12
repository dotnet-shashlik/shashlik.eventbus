namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息监听器创建工厂接口
    /// </summary>
    public interface IMessageListenerFactory
    {
        /// <summary>
        /// 根据消息处理描述器创建消息监听器
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public IMessageListener CreateMessageListener(EventHandlerDescriptor descriptor);
    }
}