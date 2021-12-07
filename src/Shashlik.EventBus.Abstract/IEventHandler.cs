using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 执行事件处理
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="additionalItems">附加数据</param>
        /// <returns></returns>
        Task Execute(TEvent @event, IDictionary<string, string> additionalItems);
    }
}