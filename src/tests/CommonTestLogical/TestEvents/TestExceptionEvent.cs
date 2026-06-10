using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;

namespace CommonTestLogical.TestEvents
{
    /// <summary>
    /// 异常事件测试 - 每次执行必抛异常,用于测试重试器
    /// </summary>
    public class TestExceptionEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class TestExceptionEventHandler : IEventHandler<TestExceptionEvent>
    {
        public TestExceptionEventHandler(ILogger<TestExceptionEventHandler> logger)
        {
            Logger = logger;
        }

        private static TestExceptionEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;
        private static int _counter;
        private ILogger<TestExceptionEventHandler> Logger { get; }

        public static TestExceptionEvent? LastInstance => _lastInstance;
        public static IDictionary<string, string>? LastItems => _lastItems;
        public static int Counter => _counter;

        public static void Reset()
        {
            Interlocked.Exchange(ref _lastInstance, null);
            Interlocked.Exchange(ref _lastItems, null);
            Interlocked.Exchange(ref _counter, 0);
        }

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;
            Interlocked.Increment(ref _counter);
            throw new Exception("intentional");
        }
    }

    public class TestExceptionEventGroup2Handler : IEventHandler<TestExceptionEvent>
    {
        public TestExceptionEventGroup2Handler(ILogger<TestExceptionEventHandler> logger)
        {
            Logger = logger;
        }

        private static TestExceptionEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;
        private static int _counter;
        private ILogger<TestExceptionEventHandler> Logger { get; }

        public static TestExceptionEvent? LastInstance => _lastInstance;
        public static IDictionary<string, string>? LastItems => _lastItems;
        public static int Counter => _counter;

        public static void Reset()
        {
            Interlocked.Exchange(ref _lastInstance, null);
            Interlocked.Exchange(ref _lastItems, null);
            Interlocked.Exchange(ref _counter, 0);
        }

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;
            Interlocked.Increment(ref _counter);
            throw new Exception("intentional");
        }
    }
}
