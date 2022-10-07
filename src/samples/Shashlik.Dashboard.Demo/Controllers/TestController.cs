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
            await publisher.PublishAsync(new TestEvent(), null);

            return "success";
        }

        [AllowAnonymous]
        [HttpGet("/test1")]
        public async Task<string> Test()
        {
            return "success";
        }
    }
}