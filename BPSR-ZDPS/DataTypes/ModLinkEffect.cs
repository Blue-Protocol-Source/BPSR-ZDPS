namespace BPSR_ZDPS.DataTypes
{
    public class ModLinkEffectTable
    {
        public Dictionary<int, ModLinkEffect> Data = new Dictionary<int, ModLinkEffect>();
    }

    public class ModLinkEffect
    {
        public string Id { get; set; }
        public int LinkTime { get; set; }
        public List<List<int>> LinkLevelEffect { get; set; }
        public int FightValue { get; set; }
    }
}
