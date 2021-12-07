using System.Collections.Generic;
using System.Linq;
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
    [ConditionDependsOn(typeof(IEventSmsSender), typeof(ISmsSender))]
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
                if (@event.IsCaptcha)
                {
                    await SmsSender.SendCaptchaAsync(@event.Phones.First(), @event.Subject, @event.Args.ToArray());
                }
                else
                {
                    await SmsSender.SendAsync(@event.Phones, @event.Subject, @event.Args.ToArray());
                }
            }
            catch (SmsLimitException e)
            {
                Logger.LogError(e, "Sms send failed of limited.");
            }
            catch (SmsTemplateException e)
            {
                Logger.LogError(e, $"Sms send failed, arguments error: {@event.ToJson()}");
            }
            catch (SmsServerException e)
            {
                Logger.LogError(e, $"Sms send failed, sms server error: {@event.ToJson()}");
            }
            //TODO:  检查是否可重试
        }
    }
}