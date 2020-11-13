using System;
using System.Text.Json;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 默认json序列化器
    /// </summary>
    public class DefaultJsonSerializer : IMessageSerializer
    {
        public string Serialize(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            return JsonSerializer.Serialize(instance);
        }

        public object Deserialize(string str, Type type)
        {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(str));
            return JsonSerializer.Deserialize(str, type);
        }
    }
}