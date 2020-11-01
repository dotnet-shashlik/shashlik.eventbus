using NodeCommon;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Node1
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
