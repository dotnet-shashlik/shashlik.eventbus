using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace SampleBase
{
    public class TestEventHandler2 : IEventHandler<DelayEvent>
    {
        public async Task Execute(DelayEvent @event, IDictionary<string, string> items)
        {
            Console.WriteLine();
            Console.WriteLine("#################################################################");
            Console.WriteLine($"Received Msg: {DateTime.Now}");
            Console.WriteLine(@event.ToJson());
            Console.WriteLine(items.ToJson());
            await Task.CompletedTask;
            Console.WriteLine("#################################################################");
            Console.WriteLine();
        }
    }
}