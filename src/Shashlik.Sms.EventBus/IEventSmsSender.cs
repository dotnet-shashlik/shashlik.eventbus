using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace Shashlik.Sms.EventBus
{
    /// <summary>
    /// 事件短信发送器,保障最终一致性
    /// </summary>
    public interface IEventSmsSender
    {
        /// <summary>
        /// 批量短信发送(相同内容短信)
        /// </summary>
        /// <param name="phones">手机号码</param>
        /// <param name="subject">业务短信类型</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="args">模板参数,注意参数顺序</param>
        Task SendAsync(IEnumerable<string> phones, string subject, ITransactionContext? transactionContext, params string[] args);

        /// <summary>
        /// 业务短信发送
        /// </summary>
        /// <param name="phone">手机号码</param>
        /// <param name="subject">业务短信类型</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="args">模板参数,注意参数顺序</param>
        Task SendAsync(string phone, string subject, ITransactionContext? transactionContext, params string[] args);

        /// <summary>
        /// 验证码短信发送,会执行短信发送频率检查
        /// </summary>
        /// <param name="phone">手机号码</param>
        /// <param name="subject">验证码对应模板的类型</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="args"></param>
        Task SendCaptchaAsync(string phone, string subject, ITransactionContext? transactionContext, params string[] args);
    }
}