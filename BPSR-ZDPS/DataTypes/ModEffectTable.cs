namespace BPSR_ZDPS.DataTypes
{
    public class ModEffectTable
    {
        public Dictionary<int, EffectData> Data = [];
    }

    public class EffectData
    {
        public int Id { get; set; }
        public int EffectID { get; set; }
        public string EffectName { get; set; }
        public int Level { get; set; }
        public string EffectConfigIcon { get; set; }
        public List<List<int>> EffectConfig { get; set; }
        public List<object> EffectKey { get; set; }
        public List<object> EffectValue { get; set; }
        public int EnhancementNum { get; set; }
        public int PlayerLevel { get; set; }
        public string EffectOverview { get; set; }
        public int EffectType { get; set; }
        public bool IsNegative { get; set; }
        public int FightValue { get; set; }
        public bool IsShowShield { get; set; }
    }
}
