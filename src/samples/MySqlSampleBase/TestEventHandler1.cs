using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace MySqlSampleBase
{
    public class TestEventHandler1 : IEventHandler<Event1>
    {
        public async Task Execute(Event1 @event, IDictionary<string, string> items)
        {
            Console.WriteLine(@event.ToJson());
            Console.WriteLine(items.ToJson());
            await Task.CompletedTask;
        }
    }
}
