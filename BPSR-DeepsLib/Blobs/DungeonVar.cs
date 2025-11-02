using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonVar(BlobReader blob) : BlobType(ref blob)
{
    public List<DungeonVarData>? Data;

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonVar.DungeonVarDataFieldNumber:
                Data = blob.ReadList<DungeonVarData>();
                return true;
            default:
                return false;
        }
    }
}