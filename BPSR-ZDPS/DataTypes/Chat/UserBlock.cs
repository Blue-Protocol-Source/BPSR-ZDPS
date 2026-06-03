namespace BPSR_ZDPS.DataTypes.Chat
{
    public class UserBlock
    {
        public long ID { get; set; } = -1;
        public string Name { get; set; } = "";
        public DateTime BlockedAt { get; set; } = DateTime.Now;
    }
}
