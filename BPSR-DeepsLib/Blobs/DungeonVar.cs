using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonVar : BlobType
{
    public List<DungeonVarData>? Data;

    public DungeonVar()
    {
    }

    public DungeonVar(BlobReader blob) : base(ref blob)
    {
    }

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