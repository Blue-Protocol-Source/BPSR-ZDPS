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
}