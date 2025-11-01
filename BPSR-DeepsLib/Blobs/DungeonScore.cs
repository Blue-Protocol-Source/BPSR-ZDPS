using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonScore(BlobReader blob) : BlobType(ref blob)
{
    public int? TotalScore;
    public int? CurRatio;

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index) {
            case 1:
                TotalScore = blob.ReadInt();
                return true;
            case 2:
                CurRatio = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }
}