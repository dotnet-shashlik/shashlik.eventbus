using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace SampleBase
{
    public class TestEventHandler1 : IEventHandler<Event1>
    {
        public TestEventHandler1(ILogger<TestEventHandler1> logger)
        {
            Logger = logger;
        }

        private ILogger<TestEventHandler1> Logger { get; }

        public async Task Execute(Event1 @event, IDictionary<string, string> items)
        {
            var sb = new StringBuilder();

            sb.AppendLine("#################################################################");
            sb.AppendLine($"Received Msg: {DateTime.Now}");
            sb.AppendLine(@event.ToJson());
            sb.AppendLine(items.ToJson());
            sb.AppendLine("#################################################################");

            Logger.LogWarning(sb.ToString());

            await Task.CompletedTask;
        }
    }
}