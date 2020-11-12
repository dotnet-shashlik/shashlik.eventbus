using System;

namespace Shashlik.EventBus
{
    public class PublishException : Exception
    {
        public PublishException(string message) : base(message)
        {
        }

        public PublishException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}