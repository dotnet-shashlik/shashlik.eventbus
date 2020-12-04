using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 对象序列化化器
    /// </summary>
    public interface IMessageSerializer
    {
        string Serialize(object instance);

        object? Deserialize(string str, Type type);
    }
}