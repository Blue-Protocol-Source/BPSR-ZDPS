namespace BPSR_DeepsLib;

public class NotifyId(ulong serviceId, uint methoidId)
{
    public ulong ServiceId { get; set; } = serviceId;
    public uint MethoidId { get; set; } = methoidId;

    public override bool Equals(object? obj)
    {
        return ServiceId == ((NotifyId)obj).ServiceId && MethoidId == ((NotifyId)obj).MethoidId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceId, MethoidId);
    }
}