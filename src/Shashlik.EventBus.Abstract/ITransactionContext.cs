// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 当前事务上下文抽象
    /// </summary>
    public interface ITransactionContext
    {
        /// <summary>
        /// 当前事务是否已完成(已提交/回滚/Disposed)
        /// </summary>
        /// <returns></returns>
        bool IsDone();
    }
}