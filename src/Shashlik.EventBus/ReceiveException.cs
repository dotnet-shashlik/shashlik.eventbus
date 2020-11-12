using System;

namespace Shashlik.EventBus
{
    public class ReceiveException : Exception
    {
        public ReceiveException(string message) : base(message)
        {
        }

        public ReceiveException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}