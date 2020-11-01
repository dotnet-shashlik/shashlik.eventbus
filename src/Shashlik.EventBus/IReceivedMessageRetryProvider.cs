namespace Shashlik.EventBus
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public interface IReceivedMessageRetryProvider
    {
        void DoRetry();
    }
}