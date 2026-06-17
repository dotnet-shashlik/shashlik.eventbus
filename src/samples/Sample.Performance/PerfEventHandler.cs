using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace Sample.Performance
{
    public class PerfEventHandler : IEventHandler<PerfEvent>
    {
        private readonly BenchmarkState _state;

        public PerfEventHandler(BenchmarkState state)
        {
            _state = state;
        }

        public Task Execute(PerfEvent @event, IDictionary<string, string> items)
        {
            _state.OnReceived();
            return Task.CompletedTask;
        }
    }
}
