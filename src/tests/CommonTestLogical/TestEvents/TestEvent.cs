using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace CommonTestLogical.TestEvents
{
    public class TestEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class TestEventHandler : IEventHandler<TestEvent>
    {
        // 静态 LastInstance 便于从测试中读取,需要先 Reset 避免上一个测试残留。
        // 用 Interlocked 计数(没用 volatile 是因为引用赋值本身有 release barrier)。
        private static TestEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;
        private static int _counter;

        public static TestEvent? LastInstance => _lastInstance;
        public static IDictionary<string, string>? LastItems => _lastItems;
        public static int Counter => _counter;

        public static void Reset()
        {
            Interlocked.Exchange(ref _lastInstance, null);
            Interlocked.Exchange(ref _lastItems, null);
            Interlocked.Exchange(ref _counter, 0);
        }

        public static async Task WaitForInstance(TimeSpan timeout)
        {
            var begin = DateTimeOffset.Now;
            while (_lastInstance is null && (DateTimeOffset.Now - begin) < timeout)
                await Task.Delay(50);
        }

        public Task Execute(TestEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;
            Interlocked.Increment(ref _counter);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 组2测试
    /// </summary>
    public class TestEventGroup2Handler : IEventHandler<TestEvent>
    {
        private static TestEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;

        public static TestEvent? LastInstance => _lastInstance;
        public static IDictionary<string, string>? LastItems => _lastItems;

        public static void Reset()
        {
            Interlocked.Exchange(ref _lastInstance, null);
            Interlocked.Exchange(ref _lastItems, null);
        }

        public static async Task WaitForInstance(TimeSpan timeout)
        {
            var begin = DateTimeOffset.Now;
            while (_lastInstance is null && (DateTimeOffset.Now - begin) < timeout)
                await Task.Delay(50);
        }

        public Task Execute(TestEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;
            return Task.CompletedTask;
        }
    }
}
