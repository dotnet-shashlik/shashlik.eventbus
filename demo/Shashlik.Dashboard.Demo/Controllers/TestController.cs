using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo.Controllers
{
    public class TestController
    {
        [AllowAnonymous]
        [HttpGet("/test")]
        public async Task Test([FromServices] IEventPublisher publisher)
        {
            await publisher.PublishAsync(new TestEvent(), null);
        }
    }

    public class TestEvent : IEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class TestEventHandler : IEventHandler<TestEvent>
    {
        public Task Execute(TestEvent @event, IDictionary<string, string> additionalItems)
        {
            var rand = new Random();
            if (rand.Next(5) == 0)
            {
                throw new Exception();
            }
            return Task.CompletedTask;
        }
    }
}
