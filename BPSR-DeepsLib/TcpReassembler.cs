using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using PacketDotNet;
using Serilog;
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
            Log.Logger.Information("Got a new connection {ep}", ep);
        }

        var conn = Connections[ep];
        if (tcpPacket.Reset || tcpPacket.Finished || tcpPacket.Synchronize)
        {
            RemoveConnection(conn);
            Log.Logger.Information($"Removed connection {ep}, Reset: {tcpPacket.Reset}, Finished: {tcpPacket.Finished}, Synchronize: {tcpPacket.Synchronize}");
            return;
        }

        conn.AddPacket(tcpPacket);
        RemoveTimedOutConnections();
    }

    public void RemoveConnection(ConnectionId connId)
    {
        if (Connections.TryGetValue(connId.SrcEp, out var srcConn))
        {
            RemoveConnection(srcConn);
        }

        if (Connections.TryGetValue(connId.DestEp, out var destConn))
        {
            RemoveConnection(destConn);       
        }
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
            Log.Logger.Information($"Removed {toRemove.Count} connections");
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
        public bool IsSynced = false;

        public void AddPacket(TcpPacket tcpPacket)
        
        {
            if (!IsSynced)
            {
                if (tcpPacket.PayloadData.Length >= 6 && BinaryPrimitives.ReadInt32BigEndian(tcpPacket.PayloadData) == tcpPacket.PayloadData.Length &&
                    (BinaryPrimitives.ReadInt16BigEndian(tcpPacket.PayloadData.AsSpan()[4..]) & 0x7FFF) <= 8)
                {
                    IsSynced = true;
                    Log.Logger.Information($"Connection {EndPoint} is synced");
                }
                else {
                    return;
                }
            }

            var hasPacketWithSequence = Packets.ContainsKey(tcpPacket.SequenceNumber);
            var seqIsLessThanLast = tcpPacket.SequenceNumber < LastSeq;
            if (hasPacketWithSequence || seqIsLessThanLast || !IsAlive)
            {
                //Debug.WriteLine($"Got a duplicate packet or was older than read. NextExpectedSeq: {NextExpectedSeq}, SequenceNumber: {tcpPacket.SequenceNumber}, WasDupe: {hasPacketWithSequence}, WasLess Than Last: {seqIsLessThanLast}, IsAlive: {IsAlive}, Packet Data Len: {tcpPacket.PayloadData.Length}");
                //return;
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

            if (toRemove.Count() > 0)
                Log.Logger.Information($"Cleaned up {toRemove.Count()} packets");
        }
    }
}

public class PacketFragment(uint seqNum, byte[] data)
{
    public uint SequenceNumber = seqNum;
    public byte[] PayloadData = data;
    public DateTime ArriveTime = DateTime.Now;
}