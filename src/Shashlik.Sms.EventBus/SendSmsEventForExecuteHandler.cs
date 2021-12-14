using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.Kernel.Attributes;
using Shashlik.Kernel.Dependency;

namespace Shashlik.Sms.EventBus
{
    /// <summary>
    /// 发送短信事件,执行真正的短信发送
    /// </summary>
    [ConditionDependsOn(typeof(IEventSmsSender), typeof(ISmsSender))]
    [ConditionOnProperty(typeof(bool), "Shashlik.Sms.EventBus.Enable", true, DefaultValue = true)]
    [Transient]
    public class SendSmsEventForExecuteHandler : IEventHandler<SendSmsEvent>
    {
        public SendSmsEventForExecuteHandler(ISmsSender smsSender, ILogger<SendSmsEventForExecuteHandler> logger)
        {
            SmsSender = smsSender;
            Logger = logger;
        }

        private ILogger<SendSmsEventForExecuteHandler> Logger { get; }
        private ISmsSender SmsSender { get; }

        public async Task Execute(SendSmsEvent @event, IDictionary<string, string> items)
        {
            try
            {
                await SmsSender.SendAsync(@event.Phones, @event.Subject, @event.Args.ToArray());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Sms send failed");
            }
        }
    }
}