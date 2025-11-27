using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class DungeonEvent : BlobType
{
    public Dictionary<int, DungeonEventData>? DungeonEventData;

    public DungeonEvent()
    {
    }

    public DungeonEvent(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonEvent.DungeonEventDataFieldNumber:
                DungeonEventData = blob.ReadHashMap<int, DungeonEventData>();
                return true;
            default:
                return false;
        }
    }
}
