using System;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 重试执行器
    /// </summary>
    public interface IRetryProvider
    {
        /// <summary>
        /// 执行重试
        /// </summary>
        /// <param name="id">消息存储id</param>
        /// <param name="retryAction">重试action,返回异步任务成功/失败</param>
        /// <returns></returns>
        public void Retry(long id, Func<Task<HandleResult>> retryAction);
    }
}