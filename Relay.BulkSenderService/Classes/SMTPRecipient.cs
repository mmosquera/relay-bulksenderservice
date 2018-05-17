namespace Relay.BulkSenderService.Classes
{
    public class SMTPRecipient : Recipient
    {
        public string Body { get; set; }
        public string AttachFileName { get; set; }
    }
}
