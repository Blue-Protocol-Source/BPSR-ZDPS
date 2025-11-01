using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using PacketDotNet;
using SharpPcap;

namespace BPSR_DeepsLib;

public class TcpReassembler
{
    public static TimeSpan ConnectionCleanUpInterval = TimeSpan.FromSeconds(60);
    public Action<TcpConnection> OnNewConnection;
    public ConcurrentDictionary<IPEndPoint, TcpConnection> Connections = new();
    public DateTime LastConnectionCleanUpTime = DateTime.Now;
    
    public void AddPacket(IPv4Packet ipPacket, TcpPacket tcpPacket, PosixTimeval timeval)
    {
        var ep = new IPEndPoint(ipPacket.SourceAddress, tcpPacket.SourcePort);
        if (!Connections.ContainsKey(ep))
        {
            var destEp = new IPEndPoint(ipPacket.DestinationAddress, tcpPacket.DestinationPort);
            var newConn = new TcpConnection(ep, destEp);
            Connections.TryAdd(ep, newConn);
            OnNewConnection?.Invoke(newConn);
            Debug.WriteLine($"Got a new connection {ep}");
        }

        var conn = Connections[ep];
        if (tcpPacket.Reset || tcpPacket.Finished || tcpPacket.Synchronize)
        {
            RemoveConnection(conn);
            Debug.WriteLine($"Removed connection {ep}, Reset: {tcpPacket.Reset}, Finished: {tcpPacket.Finished}, Synchronize: {tcpPacket.Synchronize}");
            return;
        }

        conn.AddPacket(tcpPacket);
        RemoveTimedOutConnections();
    }

    private void RemoveTimedOutConnections()
    {
        if (DateTime.Now - LastConnectionCleanUpTime >= ConnectionCleanUpInterval)
        {
            var toRemove = new List<TcpConnection>();
            foreach (var connection in Connections)
            {
                if ((DateTime.Now - connection.Value.LastPacketAt).TotalSeconds >= 60)
                {
                    toRemove.Add(connection.Value);
                }
            }

            foreach (var connection in toRemove)
            {
                RemoveConnection(connection);
            }

            LastConnectionCleanUpTime = DateTime.Now;
            Debug.WriteLine($"Removed {toRemove.Count} connections");
        }
    }

    public void RemoveConnection(TcpConnection conn)
    {
        conn.IsAlive = false;
        conn.CancelTokenSrc.Cancel();
        Connections.TryRemove(conn.EndPoint, out var _);
        conn.Pipe.Reader.CancelPendingRead();
        conn.Pipe.Writer.Complete();
    }

    public class TcpConnection(IPEndPoint endPoint, IPEndPoint destEndPoint)
    {
        public const int NUM_PACKETS_BEFORE_CLEAN_UP = 200;
        
        public IPEndPoint EndPoint = endPoint;
        public IPEndPoint DestEndPoint = destEndPoint;
        public Dictionary<uint, PacketFragment> Packets = new();
        public uint? NextExpectedSeq = null;
        public uint LastSeq = 0;
        public Pipe Pipe = new Pipe();
        public bool IsAlive = true;
        public DateTime LastPacketAt = DateTime.MinValue;
        public ulong NumBytesSent;
        public ulong NumPacketsSeen;
        public CancellationTokenSource CancelTokenSrc = new();

        public void AddPacket(TcpPacket tcpPacket)
        {
            if (Packets.ContainsKey(tcpPacket.SequenceNumber) || tcpPacket.SequenceNumber < LastSeq || !IsAlive)
            {
                Debug.WriteLine($"Got a duplicate packet or was older than read. NextExpectedSeq: {NextExpectedSeq}, SequenceNumber: {tcpPacket.SequenceNumber}");
                return;
            }
            
            if (NextExpectedSeq == null)
                NextExpectedSeq = tcpPacket.SequenceNumber;

            var fragment = new PacketFragment(tcpPacket.SequenceNumber, tcpPacket.PayloadData);
            Packets.TryAdd(tcpPacket.SequenceNumber, fragment);
            NumPacketsSeen++;
            LastPacketAt = DateTime.Now;
            CheckAndPushContinuesData();
        }

        private void CheckAndPushContinuesData()
        {
            while (NextExpectedSeq.HasValue && Packets.TryGetValue(NextExpectedSeq.Value, out var segment)) {
                Packets.Remove(NextExpectedSeq.Value);
                
                var mem = Pipe.Writer.GetMemory(segment.PayloadData.Length);
                segment.PayloadData.CopyTo(mem);
                Pipe.Writer.Advance(segment.PayloadData.Length);
                Pipe.Writer.FlushAsync();
                NumBytesSent += (ulong)segment.PayloadData.Length;
                
                NextExpectedSeq = segment.SequenceNumber + (uint)segment.PayloadData.Length;
                LastSeq = segment.SequenceNumber;
            }

            if (Packets.Count >= NUM_PACKETS_BEFORE_CLEAN_UP)
            {
                RemoveOldCachedPackets();
            }
        }

        public void RemoveOldCachedPackets()
        {
            var toRemove = Packets.Where(x => x.Value.SequenceNumber < LastSeq ||
                                x.Value.PayloadData.Length == 0 ||
                                (DateTime.Now - x.Value.ArriveTime).TotalSeconds >= 10);
            foreach (var item in toRemove)
            {
                Packets.Remove(item.Key);
            }

            Debug.WriteLine($"Cleaned up {toRemove.Count()} packets");
        }
    }
}

public class PacketFragment(uint seqNum, byte[] data)
{
    public uint SequenceNumber = seqNum;
    public byte[] PayloadData = data;
    public DateTime ArriveTime = DateTime.Now;
}