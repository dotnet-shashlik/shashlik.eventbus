#nullable disable
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

            serviceCollection.TryAddSingleton<IMsgIdGenerator, GuidMsgIdGenerator>();
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
            serviceCollection.TryAddSingleton<IExpiredMessageProvider, DefaultExpiredMessageProvider>();
            serviceCollection.AddSingleton<IHostedService, EventBusStartup>();
            serviceCollection.AddSingleton<IHostedStopToken, EventBusStartup>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var eventHandlerFindProvider = serviceProvider.GetRequiredService<IEventHandlerFindProvider>();
            var handlers = eventHandlerFindProvider.FindAll();
            foreach (var eventHandlerDescriptor in handlers)
                serviceCollection.AddTransient(eventHandlerDescriptor.EventHandlerType);

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

        /// <summary>
        /// 获取column的值
        /// </summary>
        /// <param name="row">row</param>
        /// <param name="col">column name</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetRowValue<T>(this DataRow row, string col)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (string.IsNullOrWhiteSpace(col)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(col));
            var v = row[col];
            if (v == null || v == DBNull.Value)
                return default;
            return v.ParseTo<T>();
        }
    }
}