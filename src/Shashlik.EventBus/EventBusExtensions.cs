using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus
{
    public static class EventBusExtensions
    {
        public static long GetLongDate(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        public static DateTimeOffset LongToDateTimeOffset(this long time)
        {
            return new DateTimeOffset(time.LongToDateTime());
        }

        public static T GetValue<T>(this DataRow row, string col)
        {
            var v = row[col];
            if (v == null || v == DBNull.Value)
                return default;
            return v.ParseTo<T>();
        }

        public static IServiceCollection AddEventBus(this IServiceCollection serviceCollection,
            Action<EventBusOptions> configure)
        {
            serviceCollection.Configure(configure);
            return serviceCollection.AddEventBus();
        }

        public static IServiceCollection AddEventBus(this IServiceCollection serviceCollection,
            IConfigurationSection configuration)
        {
            serviceCollection.Configure<EventBusOptions>(configuration);
            return serviceCollection.AddEventBus();
        }

        public static IServiceCollection AddEventBus(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions<EventBusOptions>();

            serviceCollection.TryAddSingleton<IEventPublisher, DefaultEventPublisher>();
            serviceCollection.TryAddSingleton<IMessageSerializer, DefaultJsonSerializer>();

            serviceCollection.TryAddSingleton<IReceivedMessageRetryProvider, DefaultReceivedMessageRetryProvider>();
            serviceCollection.TryAddSingleton<IPublishedMessageRetryProvider, DefaultPublishedMessageRetryProvider>();
            serviceCollection.TryAddSingleton<IMessageSendQueueProvider, DefaultMessageSendQueueProvider>();
            serviceCollection.TryAddSingleton<IMessageReceiveQueueProvider, DefaultMessageReceiveQueueProvider>();
            serviceCollection.TryAddSingleton<IEventHandlerInvoker, DefaultEventHandlerInvoker>();
            serviceCollection.TryAddSingleton<IEventNameRuler, DefaultEventNameRuler>();
            serviceCollection.TryAddSingleton<IEventHandlerNameRuler, DefaultEventHandlerNameRuler>();
            serviceCollection.TryAddSingleton<IEventHandlerFindProvider, DefaultEventHandlerFindProvider>();
            
            serviceCollection.AddSingleton<IHostedService, DefaultEventBusStartup>();
            return serviceCollection;
        }
    }
}