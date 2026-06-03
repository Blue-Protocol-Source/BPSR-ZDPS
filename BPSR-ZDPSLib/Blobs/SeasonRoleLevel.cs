using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPSLib.Blobs;

public class SeasonRoleLevel : BlobType
{
    public int? Level;
    public long? CurLevelExp;

    public SeasonRoleLevel()
    {
    }

    public SeasonRoleLevel(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.SeasonRoleLevel.LevelFieldNumber:
                Level = blob.ReadInt();
                return true;
            case Zproto.SeasonRoleLevel.CurLevelExpFieldNumber:
                CurLevelExp = blob.ReadLong();
                return true;
            default:
                return false;
        }
    }
}
