using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class CompletedTargetInfo : BlobType
{
    public Dictionary<int, bool>? CompletedTargetList;

    public CompletedTargetInfo()
    {
    }

    public CompletedTargetInfo(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.CompletedTargetInfo.CompletedTargetListFieldNumber:
                CompletedTargetList = blob.ReadHashMap<int, bool>();
                return true;
            default:
                return false;
        }
    }
}