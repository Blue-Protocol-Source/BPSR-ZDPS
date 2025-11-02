using System.Net;

namespace BPSR_DeepsLib
{
    public class ConnectionId(string srcIp, ushort srcPort, string dstIp, ushort dstPort)
    {
        public string SrcIP = srcIp;
        public ushort SrcPort = srcPort;
        public string DstIP = dstIp;
        public ushort DstPort = dstPort;

        public IPEndPoint SrcEp => IPEndPoint.Parse($"{SrcIP}:{SrcPort}");
        public IPEndPoint DestEp => IPEndPoint.Parse($"{DestEp}:{dstPort}");

        public string GetId()
        {
            return CreateId(SrcIP, SrcPort, DstIP, DstPort);
        }

        // Not the most efficient
        public bool HasEndPoint(IPEndPoint ep)
        {
            return ep == SrcEp || ep == DestEp;
        }

        public static string CreateId(string srcIp, int srcPort, string dstIp, int dstPort)
        {
            string[] segments = [srcIp, $"{srcPort}", dstIp, $"{dstPort}"];
            return string.Join("-", segments.Order());
        }

        public override bool Equals(object? obj)
        {
            if (obj is ConnectionId other)
            {
                return GetId() == other.GetId();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return GetId().GetHashCode();
        }
    }
}
