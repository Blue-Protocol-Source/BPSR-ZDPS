using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class TargetTable
    {
        public Dictionary<string, Target> Data = new();
    }

    public class Target
    {
        public string Id { get; set; }
        public int TargetType { get; set; }
        public int Num { get; set; }
        public int SceneId { get; set; }
        public List<int> Param { get; set; }
        public List<List<int>> TargetPos { get; set; }
        public bool IsTeamShare { get; set; }
        public string TargetDes { get; set; }
        public bool IsShowProgress { get; set; }
        public List<List<string>> SpVariable { get; set; }
        public List<List<string>> SpVariableLimit { get; set; }
        public List<string> SpVariableName { get; set; }
        public List<int> IsShowSpVariableProgress { get; set; }
        public List<string> ShowChange { get; set; }
    }
}
