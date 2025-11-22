using System.Buffers;

namespace BPSR_DeepsLib;

public class RawPacket
{
    public byte[] Data { get; set; }
    public int Len { get; set; }
    public DateTime LastPacketTime { get; set; } = DateTime.MinValue;

    public void Set(int len)
    {
        Data = ArrayPool<byte>.Shared.Rent(len);
        Len = len;
    }
    
    public void Return()
    {
        ArrayPool<byte>.Shared.Return(Data);
    }
}