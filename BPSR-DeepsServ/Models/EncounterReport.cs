namespace BPSR_DeepsServ.Models
{
    public class EncounterReport
    {
        public ulong TeamID { get; set; } = 0;
        public string Payload { get; set; } = "";
        public string DiscordWebhookId { get; set; } = "";
        public string DiscordWebhookToken { get; set; } = "";
    }
}
