using System;
using System.Threading.Tasks;

namespace Shashlik.EventBus.DefaultImpl
{
    internal class DefaultRetryProvider : IRetryProvider
    {
        public async Task Retry(string storageId, Func<Task<HandleResult>> retryAction)
        {
            // 简单化执行方式,不再精准循环,减少线程消耗
            await retryAction().ConfigureAwait(false);
        }
    }
}