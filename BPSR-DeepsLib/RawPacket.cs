using System.Buffers;

namespace BPSR_DeepsLib;

public class RawPacket
{
    public bool IsFromServer { get; set; }
    public byte[] Data { get; set; }
    public int Len { get; set; }

    public void Set(bool fromServer, int len)
    {
        IsFromServer = fromServer;
        Data = ArrayPool<byte>.Shared.Rent(len);
        Len = len;
    }
    
    public void Return()
    {
        ArrayPool<byte>.Shared.Return(Data);
    }
}