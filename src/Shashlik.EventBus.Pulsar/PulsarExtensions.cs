using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.Pulsar;

namespace Shashlik.EventBus.Pulsar
{
    public static class PulsarExtensions
    {
        /// <summary>
        /// add pulsar mq services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="configurationSection"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddPulsar(this IEventBusBuilder eventBusBuilder,
            IConfigurationSection configurationSection)
        {
            eventBusBuilder.Services.Configure<EventBusPulsarOptions>(configurationSection);

            return eventBusBuilder.AddPulsarCore();
        }

        /// <summary>
        /// add pulsar mq services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddPulsar(this IEventBusBuilder eventBusBuilder,
            Action<EventBusPulsarOptions> action)
        {
            eventBusBuilder.Services.Configure(action);

            return eventBusBuilder.AddPulsarCore();
        }

        /// <summary>
        /// add pulsar mq core services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddPulsarCore(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddOptions<EventBusPulsarOptions>();
            eventBusBuilder.Services.AddSingleton<IMessageSender, PulsarMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, PulsarEventSubscriber>();
            eventBusBuilder.Services.AddSingleton<IPulsarConnection, DefaultPulsarConnection>();

            return eventBusBuilder;
        }
    }
}