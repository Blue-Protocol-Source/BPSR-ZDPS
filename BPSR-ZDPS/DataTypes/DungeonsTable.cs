using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class DungeonsTable
    {
        public Dictionary<string, Dungeons> Data = new();
    }

    public class Dungeons
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string DungeonTypeName { get; set; }
        public int FunctionID { get; set; }
        public int PlayType { get; set; }
        public int SceneID { get; set; }
        public List<List<int>> Condition { get; set; }
        public List<int> LimitedNum { get; set; }
        public int TeamType { get; set; }
        public List<List<int>> SingleAiCondition { get; set; }
        public List<int> SingleModeLimitNum { get; set; }
        public int SingleAiMode { get; set; }
        public int SingleModeDungeonId { get; set; }
        public int SingleAwardCounterId { get; set; }
        //public List<object> ItemConsume { get; set; }
        //public List<object> DungeonsLimit { get; set; }
        public bool IgnoreDungeonCheck { get; set; }
        public int KickTime { get; set; }
        public int EndTime { get; set; }
        public int MonsterGS { get; set; }
        public List<int> MonsterLv { get; set; }
        public List<int> Season { get; set; }
        public List<int> SeasonLv { get; set; }
        public List<int> SeasonRank { get; set; }
        public bool IsLoadRank { get; set; }
        public int ShowResultHudType { get; set; }
        public Vector3 ResultCurscenePos { get; set; }
        public List<int> ExploreConfig { get; set; }
        public List<List<int>> ExploreAward { get; set; }
        public int HideQuest { get; set; }
        public List<List<int>> DungeonTarget { get; set; }
        public List<int> FirstPassAward { get; set; }
        public List<int> PassAward { get; set; }
        public int CountLimit { get; set; }
        public int ExtraAward { get; set; }
        public string FailTexture { get; set; }
        public int DisableTransport { get; set; }
        public int ActiveStateTime { get; set; }
        public int ReadyStateTime { get; set; }
        public int PlayingStateTime { get; set; }
        public int SettlementStateTime { get; set; }
        public int ExitTransferType { get; set; }
        public List<int> Affix { get; set; }
        public int AffixPool { get; set; }
        public bool AttrGrowRangeBuff { get; set; }
        public int DungeonsAttrGrowRange { get; set; }
        public int DeathReleaseTime { get; set; }
        public int RecommendFightValue { get; set; }
        public int AssessId { get; set; }
        public int AffixEntityAttrId { get; set; }
        public List<int> AssitNumber { get; set; }
        public bool IsShowFakeAttr { get; set; }
        public bool CanRide { get; set; }
        public bool IsDpsTrackerOn { get; set; }
        public int CanSummoned { get; set; }
    }
}
