namespace BPSR_DeepsServ.Models
{
    public class EncounterReport
    {
        public ulong TeamID { get; set; } = 0;
        public string EncounterName { get; set; } = "";
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0);
        public string DiscordWebhookId { get; set; } = "";
        public string DiscordWebhookToken { get; set; } = "";
        public List<PartyMember> Party { get; set; } = [];
    }

    public class PartyMember
    {
        public string Name { get; set; } = "";
        public ulong CombatScore { get; set; }
        public ulong Dps { get; set; }
        public ulong Hps { get; set; }
        public ulong DamageTaken { get; set; }
        public float DamagePct { get; set; }
    }
}
