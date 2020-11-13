using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace SampleBase
{
    public class TestEventHandler2 : IEventHandler<DelayEvent>
    {
        public TestEventHandler2(ILogger<TestEventHandler2> logger)
        {
            Logger = logger;
        }

        private ILogger<TestEventHandler2> Logger { get; }

        public async Task Execute(DelayEvent @event, IDictionary<string, string> items)
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