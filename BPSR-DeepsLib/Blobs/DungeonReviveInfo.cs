using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonReviveInfo(BlobReader blob) : BlobType(ref blob)
{
    public List<int>? ReviveIds;
    public Dictionary<int, int>? ReviveMap;
    
    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonReviveInfo.ReviveIdsFieldNumber:
                ReviveIds = blob.ReadList<int>();
                return true;
            case Zproto.DungeonReviveInfo.ReviveMapFieldNumber:
                ReviveMap = blob.ReadHashMap<int>();
                return true;
            default:
                return false;
        }
    }
}