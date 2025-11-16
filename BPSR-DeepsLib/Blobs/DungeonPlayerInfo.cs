using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class DungeonPlayerInfo : BlobType
{
    public long? CharId;
    public SocialData? SocialData;

    public DungeonPlayerInfo()
    {
    }

    public DungeonPlayerInfo(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonPlayerInfo.CharIdFieldNumber:
                CharId = blob.ReadLong();
                return true;
            case Zproto.DungeonPlayerInfo.SocialDataFieldNumber:
                SocialData = new(blob);
                return true;
            default:
                return false;
        }
    }
}
