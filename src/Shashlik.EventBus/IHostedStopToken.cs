using System.Threading;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 应用程序退出token，用于取消异步、循环任务
    /// </summary>
    public interface IHostedStopToken
    {
        /// <summary>
        /// 应用程序退出token
        /// </summary>
        CancellationToken StopCancellationToken { get; }
    }
}