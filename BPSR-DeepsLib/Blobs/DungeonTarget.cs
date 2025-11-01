namespace BPSR_DeepsLib.Blobs;

public class DungeonTarget : BlobType
{
    public Dictionary<int, DungeonTargetData> TargetData;

    public DungeonTarget()
    {
    }

    public DungeonTarget(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index) {
            case Zproto.DungeonTarget.TargetDataFieldNumber:
                TargetData = blob.ReadHashMap<DungeonTargetData>();
                return true;
            default:
                return false;
        }
    }
}