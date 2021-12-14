using System;
using Newtonsoft.Json;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 默认json序列化器
    /// </summary>
    public class DefaultJsonSerializer : IMessageSerializer
    {
        public string Serialize(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);
            return JsonConvert.SerializeObject(instance);
        }

        public object? Deserialize(string str, Type type)
        {
            ArgumentNullException.ThrowIfNull(str);
            ArgumentNullException.ThrowIfNull(type);
            return JsonConvert.DeserializeObject(str, type);
        }
    }
}