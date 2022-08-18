using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 对象序列化抽象
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        string Serialize(object instance);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        object? Deserialize(string str, Type type);
    }
}