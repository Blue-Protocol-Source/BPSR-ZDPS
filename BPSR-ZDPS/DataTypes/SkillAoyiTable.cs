using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class SkillAoyiTable
    {
        public Dictionary<string, SkillAoyi> Data = new();
    }

    public class SkillAoyi
    {
        public int Id { get; set; }
        public List<List<int>> TransformationType { get; set; }
        public List<List<int>> BuffPar { get; set; }
        public List<int> ShowSkillType { get; set; }
        public int Index { get; set; }
        public int RarityType { get; set; }
        public int Classification { get; set; }
        public int SeasonId { get; set; }
        public int AoyiItemId { get; set; }
        public string ArtPreview { get; set; }
        public string ArtTips { get; set; }
        public int MonsterId { get; set; }
        public float ModelRatio { get; set; }
        public string ModelAnim { get; set; }
        public string ResonanceObject { get; set; }
        public string Dialogue { get; set; }
        public List<List<int>> SkillAdvancedItem { get; set; }
        public bool IsBlocked { get; set; }
        public int ResonanceMaxLv { get; set; }
        public int BondsFraction { get; set; }
    }
}
