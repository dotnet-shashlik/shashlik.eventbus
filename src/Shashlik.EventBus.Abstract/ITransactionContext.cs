// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 当前事务上下文抽象
    /// </summary>
    public interface ITransactionContext
    {
        /// <summary>
        /// 当前事务是否已提交或回滚
        /// </summary>
        /// <returns></returns>
        bool IsDone();
    }
}