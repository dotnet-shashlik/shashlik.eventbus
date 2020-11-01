// ReSharper disable CheckNamespace

using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 对象实例化器
    /// </summary>
    public interface IMessageSerializer
    {
        string Serialize(object instance);

        object Deserialize(string str, Type type);
    }
}