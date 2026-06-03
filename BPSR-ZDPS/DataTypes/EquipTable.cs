using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class EquipTable
    {
        public Dictionary<string, Equip> Data = new();
    }

    public class Equip
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public int EquipPart { get; set; }
        public List<int> EquipProfession { get; set; }
        public int EquipType { get; set; }
        public int FashionMId { get; set; }
        public int FashionFId { get; set; }
        public int WeaponSkinId { get; set; }
        public int EquipGs { get; set; }
        public List<List<int>> WearCondition { get; set; }
        public List<int> PerfectUpperLimit { get; set; }
        public int PerfectLibId { get; set; }
        public List<int> BasicAttrLibId { get; set; }
        public List<int> AdvancedAttrLibId { get; set; }
        public bool IsAllowAdvancedAttrSame { get; set; }
        public List<int> RecastingAttrLibId { get; set; }
        public int DecomposeId { get; set; }
        public int EnchantId { get; set; }
        public int EquipNameGroupId { get; set; }
        public int QualitychiIdType { get; set; }
        public List<int> QualityChildAttrLibId { get; set; }
        public List<int> RecastType { get; set; }
        public int SuitId { get; set; }
        public List<int> SeasonId { get; set; }
        public int TransformId { get; set; }
    }
}
