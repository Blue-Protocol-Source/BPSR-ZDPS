using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class ReviveInfo(BlobReader blob) : BlobType(blob)
{
    public Dictionary<int, int>? ReviveMap;
    
    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index) {
            case DungeonReviveInfo.ReviveMapFieldNumber:
                ReviveMap = blob.ReadHashMap<int>();
                return true;
            default:
                return false;
        }
    }
}