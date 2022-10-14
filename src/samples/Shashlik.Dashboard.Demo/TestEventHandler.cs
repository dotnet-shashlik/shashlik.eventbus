using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo;

public class TestEventHandler : IEventHandler<TestEvent>
{
    public Task Execute(TestEvent @event, IDictionary<string, string> additionalItems)
    {
        Console.WriteLine($"{DateTime.Now},Executing, Title: {@event.Title} ");

        if (DateTimeOffset.Now < additionalItems.GetSendAt().AddSeconds(30))
            throw new Exception();

        return Task.CompletedTask;
    }
}