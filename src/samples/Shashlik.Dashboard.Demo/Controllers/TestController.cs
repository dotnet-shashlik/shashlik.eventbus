using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo.Controllers
{
    public class TestController
    {
        [AllowAnonymous]
        [HttpGet("/test")]
        public async Task<string> Test([FromServices] IEventPublisher publisher)
        {
            await publisher.PublishAsync(new TestEvent
            {
                Title = "普通事件"
            }, null);
            await publisher.PublishAsync(new TestEvent
            {
                Title = "延迟事件"
            }, DateTimeOffset.Now.AddSeconds(35), null);
            return "success";
        }
    }
}