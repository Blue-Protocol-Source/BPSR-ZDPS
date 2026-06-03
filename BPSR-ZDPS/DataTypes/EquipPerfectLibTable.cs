using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class EquipPerfectLibTable
    {
        public Dictionary<string, EquipPerfectLib> Data = new();
    }

    public class EquipPerfectLib
    {
        public int Id { get; set; }
        public int PerfectLibId { get; set; }
        public int PartLevel { get; set; }
        public List<int> PerfectPart { get; set; }
        public List<List<int>> Probability { get; set; }
        public int MinimumGuarantee { get; set; }
        public List<int> PerfectType { get; set; }
    }
}
