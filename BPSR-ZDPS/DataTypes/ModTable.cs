namespace BPSR_ZDPS.DataTypes
{
    public class ModTable
    {
        public Dictionary<int, ModuleData> Data = [];
    }

    public class ModuleData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ModType { get; set; }
        public List<int> EffectLibId { get; set; }
        public bool IsOnly { get; set; }
        public int SimilarId { get; set; }
        public List<List<int>> DecomposeAwardPackID { get; set; }
        public int InitializationId { get; set; }
        public bool IsCanLink { get; set; }
    }
}
