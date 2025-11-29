namespace BPSR_DeepsLib;

public class NotifyId(ulong serviceId, uint methoidId)
{
    public ulong ServiceId { get; set; } = serviceId;
    public uint MethodId { get; set; } = methoidId;

    public override bool Equals(object? obj)
    {
        return ServiceId == ((NotifyId)obj).ServiceId && MethodId == ((NotifyId)obj).MethodId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceId, MethodId);
    }
}