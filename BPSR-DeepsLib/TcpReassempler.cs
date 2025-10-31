using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using PacketDotNet;
using SharpPcap;

namespace BPSR_DeepsLib;

public class TcpReassempler
{
    public Action<TcpConnection> OnNewConnection;
    public Dictionary<IPEndPoint, TcpConnection> Connections = new();
    
    public void AddPacket(IPv4Packet ipPacket, TcpPacket tcpPacket, PosixTimeval timeval)
    {
        var ep = new IPEndPoint(ipPacket.SourceAddress, tcpPacket.SourcePort);
        if (!Connections.ContainsKey(ep)) {
            var newConn = new TcpConnection(ep);
            lock (Connections) {
                Connections.Add(ep, newConn);
            }

            OnNewConnection?.Invoke(newConn);
            Debug.WriteLine($"Got a new connection {ep}");
        }

        var conn = Connections[ep]; 
        if (tcpPacket.Reset || tcpPacket.Finished || tcpPacket.Synchronize) {
            conn.IsAlive = false;
            lock (Connections) {
                Connections.Remove(conn.EndPoint);
            }
            conn.Pipe.Writer.Complete();
            Debug.WriteLine($"Removed connection {ep}, Reset: {tcpPacket.Reset}, Finished: {tcpPacket.Finished}, Synchronize: {tcpPacket.Synchronize}");
            return;
        }
        
        conn.AddPacket(tcpPacket);
    }

    public class TcpConnection(IPEndPoint endPoint)
    {
        public const int NUM_PACKETS_BEFORE_CLEAN_UP = 200;
        
        public IPEndPoint EndPoint = endPoint;
        public Dictionary<uint, TcpPacket> Packets = new();
        public uint? NextExpectedSeq = null;
        public uint LastSeq = 0;
        public Pipe Pipe = new Pipe();
        public bool IsAlive = true;
        public DateTime LastPacketAt = DateTime.MinValue;
        public ulong NumBytesSent;
        public ulong NumPacketsSeen;

        public void AddPacket(TcpPacket tcpPacket)
        {
            if (Packets.ContainsKey(tcpPacket.SequenceNumber) || tcpPacket.SequenceNumber < LastSeq || !IsAlive)
            {
                Debug.WriteLine($"Got a duplicate packet or was older than read. NextExpectedSeq: {NextExpectedSeq}, SequenceNumber: {tcpPacket.SequenceNumber}");
                return;
            }
            
            if (NextExpectedSeq == null)
                NextExpectedSeq = tcpPacket.SequenceNumber;
            
            Packets.Add(tcpPacket.SequenceNumber, tcpPacket);
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
                Pipe.Writer.FlushAsync().AsTask().GetAwaiter().GetResult();
                NumBytesSent += (ulong)segment.PayloadData.Length;
                
                NextExpectedSeq = segment.SequenceNumber + (uint)segment.PayloadData.Length;
                LastSeq         = segment.SequenceNumber;
            }

            if (Packets.Count >= NUM_PACKETS_BEFORE_CLEAN_UP) {
                var toRemove = Packets.Where(x => x.Value.SequenceNumber < LastSeq || x.Value.PayloadData.Length == 0);
                foreach (var item in toRemove) {
                    Packets.Remove(item.Key);
                }
                
                Debug.WriteLine($"Cleaned up {toRemove.Count()} packets");
            }
        }
    }
}