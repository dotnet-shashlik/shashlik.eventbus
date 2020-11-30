using System.Linq;
using Shashlik.EventBus.DefaultImpl;
using Shouldly;
using Xunit;

namespace Shashlik.EventBus.Tests
{
    public class Tests : TestBase
    {
        public Tests(TestWebApplicationFactory<TestStartup> factory) : base(factory)
        {
        }

        [Fact]
        public void EventHandlerFindProviderTests()
        {
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            eventHandlerFindProvider.ShouldBeOfType<DefaultEventHandlerFindProvider>();

            var handlers = eventHandlerFindProvider.LoadAll().ToList();

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestEventHandler)}.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestEvent)}.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestDelayEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestDelayEventHandler)}.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestDelayEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeTrue();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestCustomNameEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestCustomNameEventHandler)}_Test.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestCustomNameEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }
        }
    }
}