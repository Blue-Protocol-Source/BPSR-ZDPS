using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using PacketDotNet;
using Serilog;
using SharpPcap;

namespace BPSR_ZDPSLib;

public class TcpReassembler
{
    public static ILogger Log = Serilog.Log.ForContext<TcpReassembler>();

    public static TimeSpan ConnectionCleanUpInterval = TimeSpan.FromSeconds(60);
    public Action<TcpConnection> OnNewConnection;
    public ConcurrentDictionary<IPEndPoint, TcpConnection> Connections = new();
    public DateTime LastConnectionCleanUpTime = DateTime.Now;
    
    public void AddPacket(IPv4Packet ipPacket, TcpPacket tcpPacket, PosixTimeval timeval)
    {
        try
        {
            var ep = new IPEndPoint(ipPacket.SourceAddress, tcpPacket.SourcePort);
            if (!Connections.ContainsKey(ep))
            {
                var destEp = new IPEndPoint(ipPacket.DestinationAddress, tcpPacket.DestinationPort);
                var newConn = new TcpConnection(ep, destEp, this);
                Connections.TryAdd(ep, newConn);
                OnNewConnection?.Invoke(newConn);
                Log.Information("Got a new connection {ep}", ep);
            }

            var conn = Connections[ep];
            if (tcpPacket.Reset || tcpPacket.Finished || tcpPacket.Synchronize)
            {
                RemoveConnection(conn);
                Log.Information($"Removed connection {ep}, Reset: {tcpPacket.Reset}, Finished: {tcpPacket.Finished}, Synchronize: {tcpPacket.Synchronize}");
                return;
            }

            conn.AddPacket(tcpPacket, timeval);
            RemoveTimedOutConnections();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding packet.");
        }
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
            if (toRemove.Count > 0)
            {
                Log.Information($"Removed {toRemove.Count} connections");
            }
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

    public class TcpConnection(IPEndPoint endPoint, IPEndPoint destEndPoint, TcpReassembler owner)
    {
        public const int NUM_PACKETS_BEFORE_CLEAN_UP = 200;
        public const int MAX_DUPE_PACKET_SEQ_DIFF = 1700;

        public IPEndPoint EndPoint = endPoint;
        public IPEndPoint DestEndPoint = destEndPoint;
        public SortedDictionary<uint, PacketFragment> Packets = new();
        public uint? NextExpectedSeq = null;
        public uint LastSeq = 0;
        public Pipe Pipe = new Pipe();
        public bool IsAlive = true;
        public DateTime LastPacketAt = DateTime.MinValue;
        public ulong NumBytesSent;
        public ulong NumPacketsSeen;
        public CancellationTokenSource CancelTokenSrc = new();
        public bool IsSynced = false;
        public TcpReassembler Owner = owner;
        public DateTime LastPacketTime;

        static bool SeqLt(uint a, uint b) => unchecked((int)(a - b)) < 0;
        static bool SeqGt(uint a, uint b) => unchecked((int)(a - b)) > 0;
        static bool SeqLe(uint a, uint b) => unchecked((int)(a - b)) <= 0;

        public void AddPacket(TcpPacket tcpPacket, PosixTimeval timeVal)
        {
            if (tcpPacket.Synchronize)
            {
                NextExpectedSeq = tcpPacket.SequenceNumber + 1;
                Log.Information("Got a Sync Tcp Packet.");
                Console.WriteLine("Got a Sync Tcp Packet.");
                return;
            }

            var payload = tcpPacket.PayloadData.ToArray();
            if (payload.Length <= 0)
            {
                return;
            }

            if (!IsSynced)
            {
                if (payload.Length >= 6 && BinaryPrimitives.ReadInt32BigEndian(payload) == payload.Length &&
                    (BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan()[4..]) & 0x7FFF) <= 9)
                {
                    IsSynced = true;
                    Log.Information($"Connection {EndPoint} is synced");
                }
                else {
                    return;
                }
            }

            if (Packets.ContainsKey(tcpPacket.SequenceNumber))
            {
                Log.Warning("{SrcEp} -> {DestEp} has duplicate packet {SequenceNumber}, LastSeq: {LastSeq}, Packets.Len: {numPackets}",
                    EndPoint, DestEndPoint, tcpPacket.SequenceNumber, LastSeq, Packets.Count);

                // I don't like this but its a catch for something going wrong
                if (unchecked(tcpPacket.SequenceNumber - LastSeq) >= MAX_DUPE_PACKET_SEQ_DIFF)
                {
                    Log.Error("{SrcEp} -> {DestEp} dupe exceeded {MAX_DUPE_PACKET_SEQ_DIFF}, removing stream to reset :<, SequenceNumber: {SequenceNumber}, LastSeq: {LastSeq}, Diff {Diff}",
                        EndPoint, DestEndPoint, MAX_DUPE_PACKET_SEQ_DIFF, tcpPacket.SequenceNumber, LastSeq, (tcpPacket.SequenceNumber - LastSeq));
                    
                    Owner.RemoveConnection(this);
                }
            }

            if (tcpPacket.SequenceNumber < LastSeq)
            {
                Log.Warning("{SrcEp} -> {DestEp} tcpPacket.SequenceNumber < LastSeq, {SequenceNumber} < {LastSeq}",
                    EndPoint, DestEndPoint, tcpPacket.SequenceNumber, LastSeq);
            }

            if (SeqLt(tcpPacket.SequenceNumber, NextExpectedSeq ?? 0) &&
                (tcpPacket.SequenceNumber + payload.Length) > NextExpectedSeq)
            {
                Log.Warning("{SrcEp} -> {DestEp} had overlap! NextExpectedSeq: {NextExpectedSeq}, SeqNumber: {SequenceNumber} to {PacketEndPos}",
                    EndPoint, DestEndPoint, NextExpectedSeq, tcpPacket.SequenceNumber, (tcpPacket.SequenceNumber + payload.Length));
            }
            
            if (NextExpectedSeq == null)
                NextExpectedSeq = tcpPacket.SequenceNumber;

            var fragment = new PacketFragment(tcpPacket.SequenceNumber, payload, timeVal.Date);
            Packets.TryAdd(tcpPacket.SequenceNumber, fragment);
            NumPacketsSeen++;
            LastPacketAt = DateTime.Now;
            CheckAndPushContinuesData();
        }

        private void CheckAndPushContinuesData()
        {
            bool consumedSomething = false;

            do
            {
                consumedSomething = false;

                var packetsToRemove = new List<uint>();

                if (Packets.TryGetValue(NextExpectedSeq.Value, out var exactFrag))
                {
                    Packets.Remove(NextExpectedSeq.Value);
                    PushPacketSegment(exactFrag);
                    consumedSomething = true;
                    continue;
                }

                foreach (var packet in Packets)
                {
                    if (SeqLt(packet.Value.SequenceNumber, NextExpectedSeq.Value) &&
                        SeqGt(packet.Value.SequenceNumber + (uint)packet.Value.PayloadData.Length, NextExpectedSeq.Value))
                    {
                        uint skip = NextExpectedSeq.Value - packet.Value.SequenceNumber;

                        var data = packet.Value.PayloadData[(int)skip..];
                        LastPacketTime = packet.Value.ArriveTime;
                        var mem = Pipe.Writer.GetMemory(data.Length);
                        data.CopyTo(mem);
                        Pipe.Writer.Advance(data.Length);
                        Pipe.Writer.FlushAsync();
                        NumBytesSent += (ulong)data.Length;

                        NextExpectedSeq = unchecked(packet.Value.SequenceNumber + (uint)packet.Value.PayloadData.Length);
                        LastSeq = unchecked(packet.Value.SequenceNumber + (uint)data.Length);

                        packetsToRemove.Add(packet.Value.SequenceNumber);

                        consumedSomething = true;

                        Log.Information("Had overlapping packet");
                        Console.WriteLine("Had overlapping packet");

                        break;
                    }
                }

                foreach (var packet in packetsToRemove)
                {
                    Packets.Remove(packet);
                }
            } while (consumedSomething);

                
            /*while (NextExpectedSeq.HasValue && Packets.TryGetValue(NextExpectedSeq.Value, out var segment))
            {
                Packets.Remove(NextExpectedSeq.Value);
                PushPacketSegment(segment);
            }*/

            if (Packets.Count >= NUM_PACKETS_BEFORE_CLEAN_UP)
            {
                RemoveOldCachedPackets();
            }
        }

        private void PushPacketSegment(PacketFragment segment)
        {
            LastPacketTime = segment.ArriveTime;
            var mem = Pipe.Writer.GetMemory(segment.PayloadData.Length);
            segment.PayloadData.CopyTo(mem);
            Pipe.Writer.Advance(segment.PayloadData.Length);
            Pipe.Writer.FlushAsync();
            NumBytesSent += (ulong)segment.PayloadData.Length;

            NextExpectedSeq = segment.SequenceNumber + (uint)segment.PayloadData.Length;
            LastSeq = segment.SequenceNumber;
        }

        public void RemoveOldCachedPackets()
        {
            var toRemove = Packets.Where(x => x.Value.SequenceNumber < LastSeq ||
                                x.Value.PayloadData.Length == 0 ||
                                (DateTime.Now - x.Value.ArriveTime).TotalSeconds >= 10).ToList();

            if (toRemove.Count() > 0)
                Log.Information($"{EndPoint} -> {DestEndPoint}, Cleaned up {toRemove.Count()} packets");

            foreach (var item in toRemove)
            {
                Packets.Remove(item.Key);
            }
        }
    }
}

public class PacketFragment(uint seqNum, byte[] data, DateTime arrivalTime)
{
    public uint SequenceNumber = seqNum;
    public byte[] PayloadData = data;
    public DateTime ArriveTime = arrivalTime;
}