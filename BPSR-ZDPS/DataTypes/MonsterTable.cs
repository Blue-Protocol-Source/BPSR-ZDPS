using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class MonsterTable
    {
        public Dictionary<string, Monster> Data = new();
    }

    public class Monster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ModelId { get; set; }
        public int MonsterType { get; set; }
        public int MonsterSizeType { get; set; }
        public int BloodTubeCount { get; set; }
        public string MonsterRank { get; set; }
        public List<int> SkillIds { get; set; }
        public List<int> HatredBuildType { get; set; }
        public bool IsFlying { get; set; }
        public List<List<int>> SightConfig { get; set; }
        public bool IsAccurateSight { get; set; }
        public bool IsShowAlertOutCamera { get; set; }
        public List<int> HatredAway { get; set; }
        public int Score { get; set; }
        public List<int> HatredAwayType { get; set; }
        public int AttributeId { get; set; }
        public float WalkSpeed { get; set; }
        public float RunSpeed { get; set; }
        public float FRunSpeed { get; set; }
        public float AlertSpeed { get; set; }
        public float MinAlertDis { get; set; }
        public float MaxAlertDis { get; set; }
        public float BodyDuration { get; set; }
        public bool ExportVoxel { get; set; }
        public string VoxelPath { get; set; }
        public int DefaultCamp { get; set; }
        public List<int> HudShowParam { get; set; }
        public List<float> EntityTurnVelocity { get; set; }
        public int AITableReference { get; set; }
        public float AttackHeight { get; set; }
        public float DropHeight { get; set; }
        public float DashSpeed { get; set; }
        public float MonsterFightArea { get; set; }
        public List<int> HatredAwayShow { get; set; }
        public List<int> Tags { get; set; }
        public float TargetSelectionWeight { get; set; }
        public int BornDissolutionTime { get; set; }
        public int BornDuration { get; set; }
        public int InvincibleTimeWithBorn { get; set; }
        public float BreakingContinueTime { get; set; }
        public float WeeknessDuration { get; set; }
        public float FractureDuration { get; set; }
        public bool BehitLightIsOpen { get; set; }
        public List<int> BornClientBuffs { get; set; }
        public List<int> DeadClientBuffs { get; set; }
        public bool HudInScreen { get; set; }
        public List<int> BloodMark { get; set; }
        public float HudPosParam { get; set; }
        public int MonsterLogicLevel { get; set; }
        public int DropPackageID { get; set; }
        public List<float> DropPackageRange { get; set; }
        public List<List<int>> StatusInfo { get; set; }
        public List<List<int>> StatusTransition { get; set; }
        public List<List<int>> InteractionTemplate { get; set; }
        public List<List<int>> ShowStatusInfo { get; set; }
        public List<List<int>> ShowStatusTransition { get; set; }
        public List<Vector2> AppearDisAppearCfg { get; set; }
        public List<Vector2> DissolutionCfg { get; set; }
        public List<int> LifeInfo { get; set; }
        public float UIHiddenDis { get; set; }
        public bool IsNotGround { get; set; }
        public int BornSkillId { get; set; }
        public bool BkCanBeHit { get; set; }
        public List<int> DoubleBloodBar { get; set; }
    }
}
