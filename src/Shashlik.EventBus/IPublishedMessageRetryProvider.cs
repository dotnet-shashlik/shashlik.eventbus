namespace Shashlik.EventBus
{
    /// <summary>
    /// 已发送的消息重试提供类
    /// </summary>
    public interface IPublishedMessageRetryProvider
    {
        void DoRetry();
    }
}