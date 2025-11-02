using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonFlowInfo(BlobReader blob) : BlobType(ref blob)
{
    public int? State;
    public int? ActiveTime;
    public int? ReadyTime;
    public int? PlayTime;
    public int? EndTime;
    public int? SettlementTime;
    public int? DungeonTimes;
    public int? Result;

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonFlowInfo.StateFieldNumber:
                State = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.ActiveTimeFieldNumber:
                ActiveTime = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.ReadyTimeFieldNumber:
                ReadyTime = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.PlayTimeFieldNumber:
                PlayTime = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.EndTimeFieldNumber:
                EndTime = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.SettlementTimeFieldNumber:
                SettlementTime = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.DungeonTimesFieldNumber:
                DungeonTimes = blob.ReadInt();
                return true;
            case Zproto.DungeonFlowInfo.ResultFieldNumber:
                Result = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }
}