namespace BPSR_DeepsServ.Models
{
    public struct ReportTeamState(DateTime time)
    {
        public DateTime ReportedAt { get; set; } = time;
    }
}
