namespace Relay.BulkSenderService.Classes
{
    public interface ILogManager
    {
        void Configure();
        ILog GetLogger(string name);
    }
}
