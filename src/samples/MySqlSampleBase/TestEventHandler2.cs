using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace MySqlSampleBase
{
    public class TestEventHandler2 : IEventHandler<DelayEvent>
    {
        public async Task Execute(DelayEvent @event, IDictionary<string, string> items)
        {
            Console.WriteLine(@event.ToJson());
            Console.WriteLine(items.ToJson());
            await Task.CompletedTask;
        }
    }
}