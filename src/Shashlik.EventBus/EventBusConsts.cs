using System;
using System.Collections.Generic;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus
{
    public static class EventBusConsts
    {
        public const string MsgIdHeaderKey = "eventbus-msg-id";
        public const string SendAtHeaderKey = "eventbus-send-at";
        public const string DelayAtHeaderKey = "eventbus-delay-at";
        public const string EventNameHeaderKey = "eventbus-event-name";

        /// <summary>
        /// 从附加数据获取唯一消息传输id
        /// </summary>
        /// <param name="additionalItems"></param>
        /// <returns></returns>
        public static string GetMsgId(this IDictionary<string, string> additionalItems)
        {
            return additionalItems[MsgIdHeaderKey];
        }

        /// <summary>
        /// 从附加数据获取事件名称
        /// </summary>
        /// <param name="additionalItems"></param>
        /// <returns></returns>
        public static string GetEventName(this IDictionary<string, string> additionalItems)
        {
            return additionalItems[EventNameHeaderKey];
        }

        /// <summary>
        /// 从附加数据过去发送时间
        /// </summary>
        /// <param name="additionalItems"></param>
        /// <returns></returns>
        public static DateTimeOffset GetSendAt(this IDictionary<string, string> additionalItems)
        {
            return additionalItems[SendAtHeaderKey].ParseTo<DateTimeOffset>();
        }

        /// <summary>
        /// 从附加数据获取延迟时间
        /// </summary>
        /// <param name="additionalItems"></param>
        /// <returns></returns>
        public static DateTimeOffset? GetDelayAt(this IDictionary<string, string> additionalItems)
        {
            return additionalItems.GetOrDefault(DelayAtHeaderKey)?.ParseTo<DateTimeOffset>();
        }
    }
}