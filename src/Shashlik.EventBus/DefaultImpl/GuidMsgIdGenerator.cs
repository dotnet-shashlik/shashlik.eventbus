using System;

namespace Shashlik.EventBus.DefaultImpl
{
    public class GuidMsgIdGenerator : IMsgIdGenerator
    {
        public string GenerateId()
        {
            return Guid.NewGuid().ToString("n");
        }
    }
}