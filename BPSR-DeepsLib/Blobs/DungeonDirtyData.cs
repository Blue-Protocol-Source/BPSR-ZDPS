using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonDirtyData(BlobReader blob) : BlobType(ref blob)
{
    public uint?         SceneUuid;
    public DungeonScore? Score;
    public ReviveInfo?   ReviveInfo;
    public DungeonTarget? Target;

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index) {
            case DungeonSyncData.SceneUuidFieldNumber:
                SceneUuid = blob.ReadUInt();
                return true;
            case DungeonSyncData.DungeonScoreFieldNumber:
                Score = new(blob);
                return true;
            case DungeonSyncData.ReviveInfoFieldNumber:
                ReviveInfo = new(blob);
                return true;
            case DungeonSyncData.TargetFieldNumber:
                Target = new(blob);
                return true;
            default:
                return false;
        }
    }
}