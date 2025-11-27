using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class DungeonPlayerList : BlobType
{
    public Dictionary<uint, DungeonPlayerInfo> PlayerInfos;

    public DungeonPlayerList()
    {
    }

    public DungeonPlayerList(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonPlayerList.PlayerInfosFieldNumber:
                PlayerInfos = blob.ReadHashMap<uint, DungeonPlayerInfo>();
                return true;
            default:
                return false;
        }
    }
}
