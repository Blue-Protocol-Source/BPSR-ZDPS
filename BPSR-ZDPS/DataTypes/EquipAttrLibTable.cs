using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class EquipAttrLibTable
    {
        public Dictionary<string, EquipAttrLib> Data = new();
    }

    public class EquipAttrLib
    {
        public int Id { get; set; }
        public int AttrLibId { get; set; }
        public List<List<int>> AttrEffect { get; set; }
        public List<List<string>> AttrEffectKey { get; set; }
        public List<List<int>> AttrEffectConfig { get; set; } // { Min, Max }
        public List<int> AllowPart { get; set; }
        public List<List<int>> FightValue { get; set; } // { Min, Max }
        public int ColorType { get; set; }
    }
}
