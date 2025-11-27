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
                TargetData = blob.ReadHashMap<int, DungeonTargetData>();
                return true;
            default:
                return false;
        }
    }

    public static implicit operator Zproto.DungeonTarget(DungeonTarget dungeonTarget)
    {
        var target = new Zproto.DungeonTarget();
        foreach (var item in dungeonTarget.TargetData)
        {
            target.TargetData.Add(item.Key, item.Value);
        }
        return target;
    }
}