using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.Kernel.Attributes;
using Shashlik.Kernel.Dependency;
using Shashlik.Sms.Exceptions;
using Shashlik.Utils.Extensions;

namespace Shashlik.Sms.EventBus
{
    /// <summary>
    /// 发送短信事件,执行真正的短信发送
    /// </summary>
    [ConditionDependsOn(typeof(ISmsSender), typeof(ISms), ConditionType = ConditionType.ALL)]
    [Transient(typeof(IEventHandler<>))]
    public class SendSmsEventForExecuteHandler : IEventHandler<SendSmsEvent>
    {
        public SendSmsEventForExecuteHandler(ISms sms, ILogger<SendSmsEventForExecuteHandler> logger)
        {
            Sms = sms;
            Logger = logger;
        }

        private ISms Sms { get; }
        private ILogger<SendSmsEventForExecuteHandler> Logger { get; }

        public async Task Execute(SendSmsEvent @event, IDictionary<string, string> items)
        {
            try
            {
                Sms.Send(@event.Phones, @event.Subject, @event.Args.ToArray());
            }
            catch (SmsLimitException e)
            {
                Logger.LogError(e, "Sms send failed of limited.");
            }
            catch (SmsArgException e)
            {
                Logger.LogError(e, $"Sms send failed, arguments error: {@event.ToJson()}");
            }

            await Task.CompletedTask;
        }
    }
}