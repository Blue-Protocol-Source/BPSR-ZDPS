using System.Buffers;
using Microsoft.Extensions.ObjectPool;
using PacketDotNet;
using Serilog;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using ZstdSharp;

namespace BPSR_DeepsLib;

public class NetCap
{
    private NetCapConfig Config;
    public ICaptureDevice CaptureDevice;
    public TcpReassembler TcpReassempler;

    private CancellationTokenSource CancelTokenSrc = new();
    public ObjectPool<RawPacket> RawPacketPool = ObjectPool.Create(new DefaultPooledObjectPolicy<RawPacket>());
    public ConcurrentQueue<RawPacket> RawPacketQueue = new();
    private Task PacketParseTask;
    private byte[] DecompressionScratchBuffer = new byte[1024 * 1024];
    private Decompressor _decompressor = new();
    private Dictionary<NotifyId, Action<ReadOnlySpan<byte>>> NotifyHandlers = new();
    public ulong NumSeenPackets = 0;
    public DateTime LastPacketSeenAt = DateTime.MinValue;
    public int NumConnectionReaders = 0;
    public ConcurrentDictionary<ConnectionId, bool> ConnectionFilters = new();
    public ConcurrentBag<string> ImportantLogMsgs = [];

    private bool IsDebugCaptureFileMode = false;
    private string DebugCaptureFile = "";//@"C:\Users\Xennma\Documents\BPSR_PacketCapture.pcap";
    private DateTime LastDebugCapturePacketTime = DateTime.MinValue;

    public void Init(NetCapConfig config)
    {
        Config = config;
    }

    public void Start()
    {
        if (!string.IsNullOrEmpty(DebugCaptureFile) && IsDebugCaptureFileMode)
        {
            CaptureDevice = new CaptureFileReaderDevice(DebugCaptureFile);
            CaptureDevice.Open();
        }
        else
        {
            CaptureDevice = GetCaptureDevice();
            CaptureDevice.Open(DeviceModes.Promiscuous, 100);
        }
        

        PacketParseTask = Task.Factory.StartNew(ParsePacketsLoop, CancelTokenSrc.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        TcpReassempler = new TcpReassembler();
        TcpReassempler.OnNewConnection += OnNewConnection;

        CaptureDevice.Filter = "tcp and not portrange 0-1000";
        CaptureDevice.OnPacketArrival += DeviceOnOnPacketArrival;
        CaptureDevice.StartCapture();

        Log.Information("Capture device started");
    }

    public void RegisterNotifyHandler(ulong serviceId, uint methodId, Action<ReadOnlySpan<byte>> handler)
    {
        NotifyHandlers.Add(new NotifyId(serviceId, methodId), handler);
    }

    public void RegisterWorldNotifyHandler(ServiceMethods.WorldNtf methodId, Action<ReadOnlySpan<byte>> handler)
    {
        NotifyHandlers.Add(new NotifyId((ulong)EServiceId.WorldNtf, (uint)methodId), handler);
    }

    private void DeviceOnOnPacketArrival(object sender, PacketCapture e)
    {
        var rawPacket = e.GetPacket();

        if (IsDebugCaptureFileMode)
        {
            if (LastDebugCapturePacketTime == DateTime.MinValue)
            {
                LastDebugCapturePacketTime = rawPacket.Timeval.Date;
            }
            else
            {
                TimeSpan timeDiff = rawPacket.Timeval.Date.Subtract(LastDebugCapturePacketTime);
                if (timeDiff > TimeSpan.Zero)
                {
                    System.Threading.Thread.Sleep(timeDiff);
                }

                LastDebugCapturePacketTime = rawPacket.Timeval.Date;
            }
        }

        var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

        var ipv4 = packet?.Extract<IPv4Packet>();
        if (ipv4 == null)
            return;

        var tcpPacket = packet?.Extract<TcpPacket>();
        if (tcpPacket == null)
            return;

        NumSeenPackets++;
        LastPacketSeenAt = DateTime.Now;

        if (tcpPacket.DestinationPort <= 1000 || tcpPacket.SourcePort <= 1000)
            return;

        if (IsDebugCaptureFileMode) {
            TcpReassempler.AddPacket(ipv4, tcpPacket, rawPacket.Timeval);
            return;
        }

        var connId = new ConnectionId(ipv4.SourceAddress.ToString(), tcpPacket.SourcePort, ipv4.DestinationAddress.ToString(), tcpPacket.DestinationPort);
        if (!ConnectionFilters.TryGetValue(connId, out var allowed))
        {
            if (IsFromGame(ipv4, tcpPacket)) {
                ConnectionFilters.TryAdd(connId, true);
            }
            else {
                ConnectionFilters.TryAdd(connId, false);
                return;
            }
        }

        if (!allowed)
            return;

        TcpReassempler.AddPacket(ipv4, tcpPacket, rawPacket.Timeval);
    }

    private void OnNewConnection(TcpReassembler.TcpConnection conn)
    {
        var task = Task.Factory.StartNew(async () =>
        {
            NumConnectionReaders++;
            while (conn.IsAlive && !CancelTokenSrc.IsCancellationRequested && !conn.CancelTokenSrc.IsCancellationRequested) {
                var buff = await conn.Pipe.Reader.ReadAtLeastAsync(6);
                if (buff.IsCompleted || buff.IsCanceled)
                    break;

                Span<byte> header = new byte[6];
                buff.Buffer.Slice(0, 6).CopyTo(header);
                var len = BinaryPrimitives.ReadUInt32BigEndian(header);
                var rawMsgType = BinaryPrimitives.ReadInt16BigEndian(header[4..]);
                var msgType = (rawMsgType & 0x7FFF);
                conn.Pipe.Reader.AdvanceTo(buff.Buffer.Start);

                if (msgType > 8)
                {
                    var msg = $"!! Message Type ({msgType}) Was not in expected range, maybe this is not a game connection! {conn.EndPoint}";
                    Debug.WriteLine(msg);
                    ImportantLogMsgs.Add(msg);
                }

                var msgBuff = await conn.Pipe.Reader.ReadAtLeastAsync((int)len);
                if (msgBuff.IsCompleted || msgBuff.IsCanceled)
                    break;

                var rawPacket = RawPacketPool.Get();
                rawPacket.Set((int)len);
                msgBuff.Buffer.Slice(0, len).CopyTo(rawPacket.Data.AsSpan()[..(int)len]);
                RawPacketQueue.Enqueue(rawPacket);
                conn.Pipe.Reader.AdvanceTo(msgBuff.Buffer.GetPosition(len));
            }

            NumConnectionReaders--;
            Debug.WriteLine($"{conn.EndPoint} finished reading");
        }, CancelTokenSrc.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    
    private void ParsePacketsLoop()
    {
        while (!CancelTokenSrc.IsCancellationRequested)
        {
            if (RawPacketQueue.TryDequeue(out var rawPacket))
            {
                ParsePacket(rawPacket.Data[..rawPacket.Len]);

                // Important to return the packet to the pool!
                rawPacket.Return();
                RawPacketPool.Return(rawPacket);
            }
            else
            {
                Task.Delay(10).Wait();
            }
        }
    }

    private void ParsePacket(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            var msgData = data[offset..];
            if (data.Length < 6)
            {
                return;
            }

            var len = BinaryPrimitives.ReadUInt32BigEndian(msgData);
            var rawMsgType = BinaryPrimitives.ReadInt16BigEndian(msgData[4..]);
            var isCompressed = (rawMsgType & 0x8000) != 0;
            var msgType = (MsgTypeId)(rawMsgType & 0x7FFF);
            var msgPayload = msgData.Slice(6, (int)len - 6);
            offset += (int)len;

            switch (msgType)
            {
                case MsgTypeId.Notify:
                    ParseNotify(msgPayload, isCompressed);
                    break;
                case MsgTypeId.FrameDown:
                    ParseFrameDown(msgPayload, isCompressed);
                    break;
                case MsgTypeId.Call:
                    Log.Information("Call: {MsgPayload}", msgPayload.Length);
                    break;
                case MsgTypeId.Return:
                    Log.Information("Return: {MsgPayload}", msgPayload.Length);
                    break;
                case MsgTypeId.None:
                case MsgTypeId.Echo:
                case MsgTypeId.FrameUp:
                case MsgTypeId.UNK1:
                case MsgTypeId.UNK2:
                    break;
            }
        }
    }

    private void ParseFrameDown(ReadOnlySpan<byte> data, bool isCompressed)
    {
        var seqNum = BinaryPrimitives.ReadUInt32BigEndian(data);

        if (isCompressed)
        {
            var decompressed = Decompress(data[4..]);
            ParsePacket(decompressed);
        }
        else
        {
            ParsePacket(data[4..]);
        }
    }

    private void ParseNotify(ReadOnlySpan<byte> data, bool isCompressed)
    {
        var serviceUuid = BinaryPrimitives.ReadUInt64BigEndian(data);
        var stubId = BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
        var methodId = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);

        var msgData = data[16..];
        if (isCompressed)
        {
            msgData = Decompress(msgData);
            Log.Information("Compressed");
        }

        if (!Enum.IsDefined(typeof(EServiceId), serviceUuid))
        {
            System.Diagnostics.Debug.WriteLine($"Unknown ServiceId = {serviceUuid} MethodId = {methodId}");
        }

        var id = new NotifyId(serviceUuid, methodId);
        if (NotifyHandlers.TryGetValue(id, out var handler))
        {
            handler(msgData);
        }

        //Log.Information("Service UUID: {ServiceUuid}, Stub ID: {StubId}, Method ID: {MethodId}, IsCompressed: {IsCompressed}", serviceUuid, stubId, methodId, isCompressed);
    }
    
    private ReadOnlySpan<byte> Decompress(ReadOnlySpan<byte> data)
    {
        var decompressedLen = _decompressor.Unwrap(data, DecompressionScratchBuffer);
        return DecompressionScratchBuffer.AsSpan()[..decompressedLen];
    }

    private bool IsFromGame(IPv4Packet ip, TcpPacket tcp)
    {
        var sw = Stopwatch.StartNew();
        var conns = Utils.GetTCPConnectionsForExe(Config.ExeName);
        var isGameConnection = conns.Any((x =>
            (x.LocalAddress == ip.SourceAddress.ToString() && x.LocalPort == tcp.SourcePort) ||
            (x.RemoteAddress == ip.SourceAddress.ToString() && x.RemotePort == tcp.SourcePort) ||
            (x.LocalAddress == ip.DestinationAddress.ToString() && x.LocalPort == tcp.DestinationPort) ||
            (x.RemoteAddress == ip.DestinationAddress.ToString() && x.RemotePort == tcp.DestinationPort)));

        sw.Stop();
        Debug.WriteLine($"Checking {ip.SourceAddress}:{tcp.SourcePort} > {ip.DestinationAddress}:{tcp.DestinationPort} is game connection: {isGameConnection}, took {sw.ElapsedMilliseconds}ms");
        
        return isGameConnection;
    }

    public void Stop()
    {
        CancelTokenSrc.Cancel();

        if (CaptureDevice != null)
        {
            CaptureDevice.StopCapture();
            CaptureDevice.Close();
            ConnectionFilters.Clear();

            Log.Information("Capture device stopped");
        }
    }

    public void PrintCaptureDevices()
    {
        var devices = CaptureDeviceList.Instance;
        foreach (var liveDevice in devices)
        {
            var dev = (LibPcapLiveDevice)liveDevice;
            Log.Information("Device: {DeviceName}, {FriendlyName}", dev.Name, dev.Interface?.FriendlyName);
        }
    }

    private ICaptureDevice GetCaptureDevice()
    {
        var devices = CaptureDeviceList.Instance;

        try
        {
            foreach (var liveDevice in devices)
            {
                var dev = (LibPcapLiveDevice)liveDevice;
                if (dev.Name == Config.CaptureDeviceName)
                {
                    Log.Information("Matched capture device: {DeviceName}, {FriendlyName}", dev.Name, dev.Interface?.FriendlyName);
                    return dev;
                }
            }

            Log.Information("No matched capture device, trying to find Ethernet");
            var ethernet = devices.FirstOrDefault(x => ((LibPcapLiveDevice)x).Interface?.FriendlyName == "Ethernet");
            if (ethernet != null)
            {
                Log.Information("Found Ethernet named capture device, using it: {DeviceName}, {FriendlyName}", ethernet.Name, ((LibPcapLiveDevice)ethernet).Interface?.FriendlyName);
                return ethernet;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting capture device");
            throw;
        }

        var device = devices[0];
        Log.Information("No matched capture device, using first found: {DeviceName}, {FriendlyName}", device.Name, ((LibPcapLiveDevice)device).Interface?.FriendlyName);
        return device;
    }

    public string GetFilterString(IEnumerable<TcpHelper.TcpRow> conns)
    {
        var connLines = conns.DistinctBy(x => x.RemoteAddress).Select(x => $"(tcp and src host {x.RemoteAddress} or dst host {x.RemoteAddress})");
        var filterStr = string.Join(" or ", connLines);
        return filterStr;
    }
}

public class PendingConnState(IPAddress addr)
{
    public IPAddress IPAddress { get; set; } = addr;
    public DateTime FirstSeenAt { get; set; } = DateTime.Now;
    public bool? IsGameConnection = null;
}