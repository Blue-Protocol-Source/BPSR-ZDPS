using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class SeasonGlobalTable
    {
        public Dictionary<string, SeasonGlobal> Data = new();
    }

    public class SeasonGlobal
    {
        public int SeasonId { get; set; }
        public string SeasonName { get; set; }
        public int SeasonTimeId { get; set; }
        public int MinEquipLevel { get; set; }
        public int MaxEquipLevel { get; set; }
        public int MaxWeaponLevel { get; set; }
        public List<List<int>> MasterScoreAward { get; set; }
        public string SeasonLevelName { get; set; }
        public string SeasonLevelIcon { get; set; }
        public int SeasonExpItemId { get; set; }
        public string SeasonIcon { get; set; }
    }
}
