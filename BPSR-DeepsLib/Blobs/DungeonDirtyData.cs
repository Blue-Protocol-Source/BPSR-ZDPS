using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonDirtyData(BlobReader blob) : BlobType(ref blob)
{
    public uint? SceneUuid;
    public DungeonFlowInfo? FlowInfo;
    public DungeonTarget? Target;
    public DungeonDamage? Damage;
    public DungeonPioneer? DungeonPioneer;
    public DungeonVar? DungeonVar;
    public DungeonAffixData? DungeonAffixData;
    public DungeonEvent? DungeonEvent;
    public DungeonScore? Score;
    public DungeonTimerInfo? TimerInfo;
    public DungeonPlayerList? PlayerList;
    public DungeonReviveInfo? ReviveInfo;
    public DungeonVarAll? DungeonVarAll;

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case DungeonSyncData.SceneUuidFieldNumber:
                SceneUuid = blob.ReadUInt();
                return true;
            case DungeonSyncData.FlowInfoFieldNumber:
                FlowInfo = new(blob);
                return true;
            case DungeonSyncData.TargetFieldNumber:
                Target = new(blob);
                return true;
            case DungeonSyncData.DamageFieldNumber:
                Damage = new(blob);
                return true;
            case DungeonSyncData.DungeonPioneerFieldNumber:
                DungeonPioneer = new(blob);
                return true;
            case DungeonSyncData.DungeonVarFieldNumber:
                DungeonVar = new(blob);
                return true;
            case DungeonSyncData.DungeonAffixDataFieldNumber:
                DungeonAffixData = new(blob);
                return true;
            case DungeonSyncData.DungeonEventFieldNumber:
                DungeonEvent = new(blob);
                return true;
            case DungeonSyncData.DungeonScoreFieldNumber:
                Score = new(blob);
                return true;
            case DungeonSyncData.TimerInfoFieldNumber:
                TimerInfo = new(blob);
                return true;
            case DungeonSyncData.DungeonPlayerListFieldNumber:
                PlayerList = new(blob);
                return true;
            case DungeonSyncData.ReviveInfoFieldNumber:
                ReviveInfo = new(blob);
                return true;
            case DungeonSyncData.DungeonVarAllFieldNumber:
                DungeonVarAll = new(blob);
                return true;
            default:
                return false;
        }
    }
}