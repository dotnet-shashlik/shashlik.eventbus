using System;
using System.Data;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Extensions;

// ReSharper disable MemberCanBePrivate.Global

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
            serviceCollection.TryAddSingleton<IMessageListenerFactory, DefaultMessageListenerFactory>();
            serviceCollection.TryAddSingleton<IReceivedDelayEventProvider, DefaultReceivedDelayEventProvider>();

            serviceCollection.AddSingleton<IHostedService, EventBusStartup>();
            return new DefaultEventBusBuilder(serviceCollection);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="messageSerializer"></param>
        /// <param name="text"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(this IMessageSerializer messageSerializer, string text)
        {
            return (T) messageSerializer.Deserialize(text, typeof(T));
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="messageSerializer"></param>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(this IMessageSerializer messageSerializer, byte[] bytes)
        {
            return (T) messageSerializer.Deserialize(Encoding.UTF8.GetString(bytes), typeof(T));
        }

        /// <summary>
        /// 序列化为bytes数组
        /// </summary>
        /// <param name="messageSerializer"></param>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static byte[] SerializeToBytes<T>(this IMessageSerializer messageSerializer, T obj)
        {
            return Encoding.UTF8.GetBytes(messageSerializer.Serialize(obj));
        }
    }
}