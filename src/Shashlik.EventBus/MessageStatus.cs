namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息状态
    /// </summary>
    public static class MessageStatus
    {
        public const string Failed = "FAILED";
        public const string Scheduled = "SCHEDULED";
        public const string Succeeded = "SUCCEEDED";
    }
}