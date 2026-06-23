using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;

namespace Sample.KafkaMysql.SingleFile;

public class TestEventHandler : IEventHandler<TestEvent>
{
    public TestEventHandler(ILogger<TestEventHandler> logger)
    {
        Logger = logger;
    }

    private ILogger<TestEventHandler> Logger { get; }

    public Task Execute(TestEvent @event, IDictionary<string, string> items)
    {
        TestRunner.RecordConsumed(@event.Id);
        Logger.LogWarning("[CONSUMED] Id={Id}, Message={Message}, Env={Env}", @event.Id, @event.Message,
            items.TryGetValue("env", out var env) ? env : "-");
        return Task.CompletedTask;
    }
}
