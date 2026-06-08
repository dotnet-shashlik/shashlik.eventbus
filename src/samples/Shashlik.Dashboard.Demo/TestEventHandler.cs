using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo;

public class TestEventHandler : IEventHandler<TestEvent>
{
    public Task Execute(TestEvent @event, IDictionary<string, string> additionalItems)
    {
        Console.WriteLine($"{DateTime.Now},Executing, Title: {@event.Title} ");

        var sendAt = additionalItems.GetSendAt();
        if (sendAt.HasValue && DateTimeOffset.Now < sendAt.Value.AddSeconds(30))
            throw new Exception();

        return Task.CompletedTask;
    }
}