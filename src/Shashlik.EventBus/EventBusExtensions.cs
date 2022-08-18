using System;
using System.Data;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.EventBus.Utils;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

// ReSharper disable ConditionIsAlwaysTrueOrFalse

// ReSharper disable MemberCanBePrivate.Global

namespace Shashlik.EventBus
{
    public static class EventBusExtensions
    {
        public static IEventBusBuilder AddEventBus(this IServiceCollection serviceCollection,
            Action<EventBusOptions> configure)
        {
            serviceCollection.Configure(configure);
            return serviceCollection.AddEventBus();
        }

        public static IEventBusBuilder AddEventBus(this IServiceCollection serviceCollection,
            IConfigurationSection configuration)
        {
            serviceCollection.Configure<EventBusOptions>(configuration);
            return serviceCollection.AddEventBus();
        }

        public static IEventBusBuilder AddEventBus(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IMsgIdGenerator, GuidMsgIdGenerator>();
            serviceCollection.TryAddSingleton<IEventPublisher, DefaultEventPublisher>();
            serviceCollection.TryAddSingleton<IMessageSerializer, DefaultJsonSerializer>();
            serviceCollection.TryAddSingleton<IReceivedMessageRetryProvider, DefaultReceivedMessageRetryProvider>();
            serviceCollection.TryAddSingleton<IPublishedMessageRetryProvider, DefaultPublishedMessageRetryProvider>();
            serviceCollection.TryAddSingleton<IEventHandlerInvoker, DefaultEventHandlerInvoker>();
            serviceCollection.TryAddSingleton<IEventNameRuler, DefaultNameRuler>();
            serviceCollection.TryAddSingleton<IEventHandlerNameRuler, DefaultNameRuler>();
            serviceCollection.TryAddSingleton<IEventHandlerFindProvider, DefaultEventHandlerFindProvider>();
            serviceCollection.TryAddSingleton<IExpiredMessageProvider, DefaultExpiredMessageProvider>();
            serviceCollection.TryAddSingleton<IMessageListener, DefaultMessageListener>();
            serviceCollection.TryAddSingleton<IRetryProvider, DefaultRetryProvider>();
            serviceCollection.TryAddSingleton<IPublishHandler, DefaultPublishHandler>();
            serviceCollection.TryAddSingleton<IReceivedHandler, DefaultReceivedHandler>();

            serviceCollection.AddSingleton<IHostedStopToken, InternalHostedStopToken>();
            serviceCollection.AddHostedService<EventBusStartup>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var eventHandlerFindProvider = serviceProvider.GetRequiredService<IEventHandlerFindProvider>();
            var handlers = eventHandlerFindProvider.FindAll();
            foreach (var eventHandlerDescriptor in handlers)
                serviceCollection.AddTransient(eventHandlerDescriptor.EventHandlerType);

            return new DefaultEventBusBuilder(serviceCollection);
        }


    }
}