using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class DungeonEventData : BlobType
{
    public int? EventId;
    public int? StartTime;
    public int? State;
    public int? Result;
    public Dictionary<int, DungeonTargetData>? DungeonTarget;

    public DungeonEventData()
    {
    }

    public DungeonEventData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonEventData.EventIdFieldNumber:
                EventId = blob.ReadInt();
                return true;
            case Zproto.DungeonEventData.StartTimeFieldNumber:
                StartTime = blob.ReadInt();
                return true;
            case Zproto.DungeonEventData.StateFieldNumber:
                State = blob.ReadInt();
                return true;
            case Zproto.DungeonEventData.ResultFieldNumber:
                Result = blob.ReadInt();
                return true;
            case Zproto.DungeonEventData.DungeonTargetFieldNumber:
                DungeonTarget = blob.ReadHashMap<int, DungeonTargetData>();
                return true;
            default:
                return false;
        }
    }
}
