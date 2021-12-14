namespace Shashlik.EventBus
{
    public class HandleResult
    {
        public HandleResult(bool success)
        {
            Success = success;
        }

        public HandleResult(bool success, MessageStorageModel messageStorageModel)
        {
            MessageStorageModel = messageStorageModel;
            Success = success;
        }

        public bool Success { get; }

        public MessageStorageModel? MessageStorageModel { get; }
    }
}