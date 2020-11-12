using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace SampleBase
{
    public class TestEventHandler1 : IEventHandler<Event1>
    {
        public async Task Execute(Event1 @event, IDictionary<string, string> items)
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