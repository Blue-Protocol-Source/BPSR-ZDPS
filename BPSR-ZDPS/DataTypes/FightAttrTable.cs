using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class FightAttrTable
    {
        public Dictionary<string, FightAttr> Data = new();
    }

    public class FightAttr
    {
        public int Id { get; set; }
        public string EnumName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsClass { get; set; }
        public bool IsSyncMe { get; set; }
        public bool IsSyncAoi { get; set; }
        public int AttrLowerLimit { get; set; }
        public int AttrUpperLimit { get; set; }
        public int Level { get; set; }
        public int AttrFinal { get; set; }
        public int AttrTotal { get; set; }
        public int AttrAdd { get; set; }
        public int AttrExAdd { get; set; }
        public int AttrPer { get; set; }
        public int AttrExPer { get; set; }
        public int AttrNumType { get; set; }
        public string OfficialName { get; set; }
        public string TipTemplate { get; set; }
        public string AttrDes { get; set; }
        public int BuffShowAttrHUD { get; set; }
        
        //public object AttrIcon { get; set; }
        public string Icon { get; set; }
        public int BaseAttr;
        public List<int> RecomProfessionId { get; set; }
        public bool IsAssess { get; set; }
    }
}
