using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class TempAttrTable
    {
        public Dictionary<string, TempAttr> Data = new();
    }

    public class TempAttr
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public int AttrType { get; set; }
        public int LogicType { get; set; }
        public List<int> AttrParams { get; set; }
        public int LowerLimit { get; set; }
        public int UpperLimit { get; set; }
        public bool IsSyncClient { get; set; }
        public string AttrDesc { get; set; }
        public string AttrIcon { get; set; }

        public Zproto.ETempAttrEffectType GetProtoAttrType()
        {
            return (Zproto.ETempAttrEffectType)AttrType;
        }

        public Zproto.ETempAttrType GetProtoLogicType()
        {
            return (Zproto.ETempAttrType)LogicType;
        }
    }
}
