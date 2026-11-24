using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class FunctionTable
    {
        public Dictionary<string, Function> Data = new();
    }

    public class Function
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public string Atlas { get; set; }
        public string DialogIcon { get; set; }
        public string Icon { get; set; }
        public List<string> FunctionOrder { get; set; }
        public int ParentId { get; set; }
        public int Level { get; set; }
        public int RoleLevel { get; set; }
        public int QuestId { get; set; }
        public List<int> QuestStepId { get; set; }
        public int TimerId { get; set; }
        public int OnOff { get; set; }
        public int Preview { get; set; }
        public string PreviewText { get; set; }
        public int OpenTips { get; set; }
        public string PreviewPic { get; set; }
    }
}
