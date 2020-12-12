using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryStorage
{
    public class MemoryStorageInitializer : IMessageStorageInitializer
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}