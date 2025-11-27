using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class DungeonPioneer : BlobType
{
    public Dictionary<int, CompletedTargetInfo>? CompletedTargetThisTime;

    public DungeonPioneer()
    {
    }

    public DungeonPioneer(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonPioneer.CompletedTargetThisTimeFieldNumber:
                CompletedTargetThisTime = blob.ReadHashMap<int, CompletedTargetInfo>();
                return true;
            default:
                return false;
        }
    }
}