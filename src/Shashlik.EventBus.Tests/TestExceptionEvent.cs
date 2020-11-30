using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.Tests
{
    /// <summary>
    /// 异常事件测试
    /// </summary>
    public class TestExceptionEvent : IEvent
    {
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
    public class TestExceptionEventHandler : IEventHandler<TestExceptionEvent>
    {
        public static TestExceptionEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            throw new Exception();
        }
    }

    [Transient(typeof(IEventHandler<>))]
    public class TestExceptionEventGroup2Handler : IEventHandler<TestExceptionEvent>
    {
        public static TestExceptionEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            throw new Exception();
        }
    }
}