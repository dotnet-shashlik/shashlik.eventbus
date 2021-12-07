using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shashlik.EventBus;
using Shashlik.Kernel.Attributes;
using Shashlik.Kernel.Dependency;
using Shashlik.Sms.Options;

namespace Shashlik.Sms.EventBus
{
    [ConditionDependsOn(typeof(ISmsSender))]
    [ConditionOnProperty(typeof(bool), "Shashlik.Sms." + nameof(SmsOptions.Enable), true, DefaultValue = true)]
    [Singleton]
    public class EventBusSmsSender : IEventSmsSender
    {

        public EventBusSmsSender(ISmsSender smsSender, IEventPublisher eventPublisher)
        {
            SmsSender = smsSender;
            EventPublisher = eventPublisher;
        }

        private IEventPublisher EventPublisher { get; }

        private ISmsSender SmsSender { get; }

        public async Task SendAsync(IEnumerable<string> phones, string subject, ITransactionContext? transactionContext, params string[] args)
        {
            await EventPublisher.PublishAsync(new SendSmsEvent
            {
                Phones = phones.ToList(),
                Subject = subject,
                Args = args.ToList()
            }, transactionContext);
        }

        public async Task SendAsync(string phone, string subject, ITransactionContext? transactionContext, params string[] args)
        {
            await SendAsync(new[] { phone }, subject, transactionContext, args);
        }

        public async Task SendWithCheckAsync(string phone, string subject, ITransactionContext? transactionContext, params string[] args)
        {
            await SmsSender.SendWithCheckAsync(phone, subject);
            await EventPublisher.PublishAsync(new SendSmsEvent
            {
                Phones = { phone },
                Subject = subject,
                Args = args.ToList()
            }, transactionContext);
        }
    }
}