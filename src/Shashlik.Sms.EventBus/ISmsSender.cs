using System.Collections.Generic;
using Shashlik.EventBus;

namespace Shashlik.Sms.EventBus
{
    /// <summary>
    /// 短信发送器
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// 短信发送,批量发送,手机数量大于一个时,不会检查发送频率
        /// </summary>
        /// <param name="phones">手机号码</param>
        /// <param name="subject">短信类型</param>
        /// <param name="transactionContext"></param>
        /// <param name="args">模板参数,注意参数顺序</param>
        void Send(IEnumerable<string> phones, string subject, ITransactionContext? transactionContext, params string[] args);

        /// <summary>
        /// 短信发送,单个手机发送,会检查发送频率
        /// </summary>
        /// <param name="phone">手机号码</param>
        /// <param name="subject">短信类型</param>
        /// <param name="transactionContext"></param>
        /// <param name="args">模板参数,注意参数顺序</param>
        void Send(string phone, string subject, ITransactionContext? transactionContext, params string[] args);
    }
}