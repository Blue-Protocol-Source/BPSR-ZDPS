namespace BPSR_ZDPS.DataTypes
{
    public class SkillFightLevelTable
    {
        public Dictionary<string, SkillFightLevel> Data = [];
    }

    public class SkillFightLevel
    {
        public string Id { get; set; }
        public int SkillId { get; set; }
        public int Level { get; set; }
        public int SkillEffectId { get; set; }
        public string Name { get; set; }
        public List<List<int>> SkillCost { get; set; }
        public List<List<int>> SkillResCheck { get; set; }
        public float PVECoolTime { get; set; }
        public List<List<string>> FloatParameter { get; set; }
        public List<int> ShowParameter { get; set; }
        public bool ShowSkillCountCD { get; set; }
        public int FightValue { get; set; }
        public List<int> CoolTimeOnBattleStart { get; set; }
        public List<float> RandomPVECoolTime { get; set; }
        public List<List<int>> SkillConditionCheck { get; set; }
        public float AbortSingTimeCD { get; set; }
    }
}
