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
        /// 从附加数据获取唯一消息传输 id。key 缺失返回 null,调用方应处理(非本系统消息)。
        /// </summary>
        public static string? GetMsgId(this IDictionary<string, string> additionalItems)
        {
            return additionalItems.GetOrDefault(MsgIdHeaderKey);
        }

        /// <summary>
        /// 从附加数据获取事件名称。key 缺失返回 null。
        /// </summary>
        public static string? GetEventName(this IDictionary<string, string> additionalItems)
        {
            return additionalItems.GetOrDefault(EventNameHeaderKey);
        }

        /// <summary>
        /// 从附加数据获取发送时间。key 缺失或值非法时返回 null。
        /// </summary>
        public static DateTimeOffset? GetSendAt(this IDictionary<string, string> additionalItems)
        {
            var raw = additionalItems.GetOrDefault(SendAtHeaderKey);
            if (string.IsNullOrEmpty(raw))
                return null;
            return raw.ParseTo<DateTimeOffset?>();
        }

        /// <summary>
        /// 从附加数据获取延迟时间。key 缺失或值非法时返回 null。
        /// </summary>
        public static DateTimeOffset? GetDelayAt(this IDictionary<string, string> additionalItems)
        {
            var raw = additionalItems.GetOrDefault(DelayAtHeaderKey);
            if (string.IsNullOrEmpty(raw))
                return null;
            return raw.ParseTo<DateTimeOffset?>();
        }
    }
}