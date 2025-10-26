namespace BPSR_DeepsLib;

public enum MsgTypeId : ushort
{
    None      = 0,
    Call      = 1,
    Notify    = 2,
    Return    = 3,
    Echo      = 4,
    FrameUp   = 5,
    FrameDown = 6,
    UNK1      = 7,
    UNK2      = 8
}