using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Kafka
{
    public class KafkaMessageCunsumerRegistry : IMessageCunsumerRegistry
    {
        public void Subscribe(IMessageListener listener, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}