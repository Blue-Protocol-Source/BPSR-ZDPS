namespace BPSR_ZDPSLib;

public class ProxyId(uint serviceId, uint methodId)
{
    public uint ServiceId { get; set; } = serviceId;
    public uint MethodId { get; set; } = methodId;

    public override bool Equals(object? obj)
    {
        return ServiceId == ((ProxyId)obj).ServiceId && MethodId == ((ProxyId)obj).MethodId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceId, MethodId);
    }
}