using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class EquipBreakThroughTable
    {
        public Dictionary<string, EquipBreakThrough> Data = new();
    }

    public class EquipBreakThrough
    {
        public int Id { get; set; }
        public int EquipId { get; set; }
        public int BreakThroughTime { get; set; }
        public int EquipGs { get; set; }
        public List<int> BasicAttrLibId { get; set; }
        public List<int> AdvancedAttrLibId { get; set; }
        public List<int> QualityChildAttrLibId { get; set; }
        public List<List<int>> Condition { get; set; }
        public List<List<int>> Consume { get; set; }
        public List<float> ModelPos { get; set; }
        public List<float> ModelRot { get; set; }
        public float TimelineScale { get; set; }
        public List<float> ModelScale { get; set; }
        public float SsprHeight { get; set; }
        public int TimelineId { get; set; }
    }
}
