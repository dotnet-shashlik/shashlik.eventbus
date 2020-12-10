using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已过期的消息处理
    /// </summary>
    public interface IExpiredMessageProvider
    {
        Task DoDelete(CancellationToken cancellationToken);
    }
}