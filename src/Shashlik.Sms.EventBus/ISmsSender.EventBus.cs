using System;
using System.Collections.Generic;
using System.Linq;
using Shashlik.EventBus;
using Shashlik.Kernel.Attributes;
using Shashlik.Kernel.Dependency;

namespace Shashlik.Sms.EventBus
{
    [ConditionDependsOn(typeof(ISms))]
    [Singleton]
    public class EventBusSmsSender : ISmsSender
    {
        public EventBusSmsSender(
            IEventPublisher eventPublisher, ISms sms)
        {
            EventPublisher = eventPublisher;
            Sms = sms;
        }

        private ISms Sms { get; }
        private IEventPublisher EventPublisher { get; }

        public void Send(IEnumerable<string> phones, string subject, ITransactionContext? transactionContext,
            params string[] args)
        {
            var list = phones?.ToList();
            if (list is null || !list.Any())
                throw new ArgumentException($"phones can't be null or empty", nameof(phones));
            Sms.ValidSend(list, subject, args);
            EventPublisher.PublishAsync(new SendSmsEvent
            {
                Phones = list,
                Subject = subject,
                Args = args.ToList()
            }, transactionContext).Wait();
        }

        public void Send(string phone, string subject, ITransactionContext? transactionContext, params string[] args)
        {
            Send(new[] {phone}, subject, transactionContext, args);
        }
    }
}