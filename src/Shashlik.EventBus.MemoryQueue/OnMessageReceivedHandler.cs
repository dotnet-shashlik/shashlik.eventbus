namespace Shashlik.EventBus.MemoryQueue
{
    public delegate void OnMessageReceivedHandler(object sender, OnMessageTransferEventArgs model);

    public class OnMessageTransferEventArgs
    {
        public MessageTransferModel MessageTransferModel { get; set; }
    }
}