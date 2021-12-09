using System;

namespace Shashlik.EventBus
{
    public class EventBusException : Exception
    {
        public EventBusException(string? message) : base(message)
        {
        }

        public EventBusException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
