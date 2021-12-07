using Shashlik.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shashlik.Sms.EventBus
{
    public static class EventSmsExtensions
    {
        /// <summary>
        /// 快捷发送验证码方法,约定验证码subject为"Captcha"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="phone">手机号码</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="args">短信参数</param>
        /// <returns></returns>
        public static async Task SendCaptchaAsync(this IEventSmsSender sender, string phone, ITransactionContext? transactionContext, params string[] args)
        {
            await sender.SendWithCheckAsync(phone, SmsConstants.SubjectCaptcha, transactionContext, args);
        }
    }
}
