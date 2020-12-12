using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息存储初始化器
    /// </summary>
    public interface IMessageStorageInitializer
    {
        /// <summary>
        /// 执行存储设施初始化
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}