using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonTargetData : BlobType
{
    public int? TargetId;
    public int? Nums;
    public int? Complete;
    
    public DungeonTargetData()
    {
    }

    public DungeonTargetData(BlobReader blob) : base(ref blob)
    {
    }
    
    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index) {
            case Zproto.DungeonTargetData.TargetIdFieldNumber:
                TargetId = blob.ReadInt();
                return true;
            case Zproto.DungeonTargetData.NumsFieldNumber:
                Nums = blob.ReadInt();
                return true;
            case Zproto.DungeonTargetData.CompleteFieldNumber:
                Complete = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }

    public static implicit operator Zproto.DungeonTargetData(DungeonTargetData targetData)
    {
        var data = new Zproto.DungeonTargetData()
        {
            Complete = targetData.Complete ?? 0,
            Nums = targetData.Nums ?? 0,
            TargetId = targetData.TargetId ?? 0
        };
        return data;
    }
}