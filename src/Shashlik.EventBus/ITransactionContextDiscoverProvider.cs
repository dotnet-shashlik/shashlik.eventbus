namespace Shashlik.EventBus
{
    /// <summary>
    /// 自动发现当前环境的事务上下文提供器
    /// </summary>
    public interface ITransactionContextDiscoverProvider
    {
        /// <summary>
        /// 优先级,从小到大
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 获取当前运行环境的事务上下文 ,null表示没有事务
        /// </summary>
        /// <returns></returns>
        ITransactionContext? Current();
    }
}
