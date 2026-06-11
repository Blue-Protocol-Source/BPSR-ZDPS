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

namespace BPSR_ZDPSLib;

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
    private Dictionary<NotifyId, Action<ReadOnlySpan<byte>, ExtraPacketData>> NotifyHandlers = new();
    private Dictionary<ProxyId, Action<ReadOnlySpan<byte>, uint, ExtraPacketData>> ProxyHandlers = new();
    private Dictionary<ProxyId, Action<ReadOnlySpan<byte>, uint, ExtraPacketData>> ProxyReturnHandlers = new();
    private ConcurrentDictionary<uint, ProxyId> ProxyReturnsDictionary = new();
    private Action<NotifyId, ReadOnlySpan<byte>, ExtraPacketData>? UnhandledHandler = null;
    private Action<ProxyId, ReadOnlySpan<byte>, ExtraPacketData>? UnhandledProxyHandler = null;
    public ulong NumSeenPackets = 0;
    public DateTime LastPacketSeenAt = DateTime.MinValue;
    public int NumConnectionReaders = 0;
    public ConcurrentDictionary<ConnectionId, bool> ConnectionFilters = new();
    public ConcurrentBag<string> ImportantLogMsgs = [];
    public ulong NumGameMessagesSeen = 0;
    public ulong NumGameMessagesDequeued = 0;

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

    public void RegisterUnhandledHandler(Action<NotifyId, ReadOnlySpan<byte>, ExtraPacketData> handler)
    {
        UnhandledHandler = handler;
    }

    public void RegisterUnhandledProxyHandler(Action<ProxyId, ReadOnlySpan<byte>, ExtraPacketData> handler)
    {
        UnhandledProxyHandler = handler;
    }

    public void RegisterNotifyHandler(ulong serviceId, uint methodId, Action<ReadOnlySpan<byte>, ExtraPacketData> handler)
    {
        NotifyHandlers.Add(new NotifyId(serviceId, methodId), handler);
    }

    public void RegisterMatchNotifyHandler(ServiceMethods.MatchNtf methodId, Action<ReadOnlySpan<byte>, ExtraPacketData> handler)
    {
        NotifyHandlers.Add(new NotifyId((ulong)EServiceId.MatchNtf, (uint)methodId), handler);
    }

    public void RegisterWorldNotifyHandler(ServiceMethods.WorldNtf methodId, Action<ReadOnlySpan<byte>, ExtraPacketData> handler)
    {
        NotifyHandlers.Add(new NotifyId((ulong)EServiceId.WorldNtf, (uint)methodId), handler);
    }

    public void RegisterProxyHandler(uint serviceId, uint methodId, Action<ReadOnlySpan<byte>, uint, ExtraPacketData> handler)
    {
        ProxyHandlers.Add(new ProxyId(serviceId, methodId), handler);
    }

    public void RegisterProxyReturnHandler(uint serviceId, uint methodId, Action<ReadOnlySpan<byte>, uint, ExtraPacketData> handler)
    {
        ProxyReturnHandlers.Add(new ProxyId(serviceId, methodId), handler);
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

                /*
                if (msgType > 20)
                {
                    var msg = $"!! Message Type ({msgType}) Was not in expected range, maybe this is not a game connection! {conn.EndPoint} -> {conn.DestEndPoint}";
                    Debug.WriteLine(msg);
                    ImportantLogMsgs.Add(msg);
                    Log.Logger.Information(msg);
                    var connId = new ConnectionId(conn.EndPoint.Address.ToString(), (ushort)conn.EndPoint.Port, conn.DestEndPoint.Address.ToString(), (ushort)conn.DestEndPoint.Port);
                    //ConnectionFilters[connId] = false;
                    TcpReassempler.RemoveConnection(connId);
                    break;
                }*/

                var msgBuff = await conn.Pipe.Reader.ReadAtLeastAsync((int)len);
                if (msgBuff.IsCompleted || msgBuff.IsCanceled)
                    break;

                var rawPacket = RawPacketPool.Get();
                rawPacket.Set((int)len);
                rawPacket.LastPacketTime = conn.LastPacketTime;
                msgBuff.Buffer.Slice(0, len).CopyTo(rawPacket.Data.AsSpan()[..(int)len]);
                RawPacketQueue.Enqueue(rawPacket);
                conn.Pipe.Reader.AdvanceTo(msgBuff.Buffer.GetPosition(len));
                NumGameMessagesSeen++;
            }

            NumConnectionReaders--;
            Log.Logger.Information($"{conn.EndPoint} finished reading");
        }, CancelTokenSrc.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    
    private void ParsePacketsLoop()
    {
        while (!CancelTokenSrc.IsCancellationRequested)
        {
            if (RawPacketQueue.TryDequeue(out var rawPacket))
            {
                ParsePacket(rawPacket.Data[..rawPacket.Len], rawPacket.LastPacketTime);

                // Important to return the packet to the pool!
                rawPacket.Return();
                RawPacketPool.Return(rawPacket);
                NumGameMessagesDequeued++;
            }
            else
            {
                Task.Delay(10).Wait();
            }
        }
    }

    private void ParsePacket(ReadOnlySpan<byte> data, DateTime lastPacketTime)
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
                    ParseNotify(msgPayload, isCompressed, lastPacketTime);
                    break;
                case MsgTypeId.FrameDown:
                    ParseFrameDown(msgPayload, isCompressed, lastPacketTime);
                    break;
                case MsgTypeId.Call:
                    ParseCall(msgPayload, isCompressed, lastPacketTime);
                    break;
                case MsgTypeId.Return:
                    ParseReturn(msgPayload, isCompressed, lastPacketTime);
                    break;
                case MsgTypeId.FrameUp:
                    ParseFrameUp(msgPayload, isCompressed, lastPacketTime);
                    break;
                case MsgTypeId.None:
                    break;
                case MsgTypeId.Echo:
                    // Empty packets sent two at a time about once per second
                    break;
                case MsgTypeId.UNK1:
                    // Counter that increments once per 5 seconds
                    break;
                case MsgTypeId.UNK2:
                    // Counter that increments about once per second
                    break;
                default:
                    Log.Information("Got an unknown message type: {msgType}", msgType);
                    break;
            }
        }
    }

    private void ParseFrameDown(ReadOnlySpan<byte> data, bool isCompressed, DateTime lastPacketTime)
    {
        var seqNum = BinaryPrimitives.ReadUInt32BigEndian(data);

        if (isCompressed)
        {
            var decompressed = Decompress(data[4..]);
            if (!decompressed.IsEmpty)
            {
                ParsePacket(decompressed, lastPacketTime);
            }
        }
        else
        {
            ParsePacket(data[4..], lastPacketTime);
        }
    }

    private void ParseNotify(ReadOnlySpan<byte> data, bool isCompressed, DateTime lastPacketTime)
    {
        //byte[] debugHeaders = data.ToArray();

        var serviceUuid = BinaryPrimitives.ReadUInt64BigEndian(data);
        var stubId = BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
        var methodId = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);

        var msgData = data[16..];
        if (isCompressed)
        {
            msgData = Decompress(msgData);

            if (msgData.IsEmpty)
            {
                Log.Logger.Warning("Error decompressing data for {serviceUuid}, {stubId}, {methodId}", serviceUuid, stubId, methodId);
                return;
            }
        }

        if (!Enum.IsDefined(typeof(EServiceId), serviceUuid))
        {
            Log.Logger.Information($"Unknown ServiceId = {serviceUuid} MethodId = {methodId}");
        }
        else
        {
            //Log.Logger.Information($"ParseNotify: S:{serviceUuid}({(EServiceId)serviceUuid}) Stub:{stubId} M:{methodId} MsgDataLen:{msgData.Length} Len={data.Length}");
        }

        var id = new NotifyId(serviceUuid, methodId);
        if (NotifyHandlers.TryGetValue(id, out var handler))
        {
            var extraData = new ExtraPacketData(lastPacketTime);
            handler(msgData, extraData);
        }
        else if (UnhandledHandler != null)
        {
            var extraData = new ExtraPacketData(lastPacketTime);
            UnhandledHandler(id, msgData, extraData);
        }

        //Log.Information("Service UUID: {ServiceUuid}, Stub ID: {StubId}, Method ID: {MethodId}, IsCompressed: {IsCompressed}", serviceUuid, stubId, methodId, isCompressed);
    }

    private void ParseCall(ReadOnlySpan<byte> data, bool isCompressed, DateTime lastPacketTime)
    {
        //byte[] debugHeaders = data.ToArray();

        var proxyServiceId = BinaryPrimitives.ReadUInt64BigEndian(data);
        var subId = BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
        var returnUid = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);
        var proxyMethodId = BinaryPrimitives.ReadUInt32BigEndian(data[16..]);
        var msgData = data[20..];

        ProxyReturnsDictionary.AddOrUpdate(returnUid, new ProxyId((uint)proxyServiceId, proxyMethodId), (key, value) => new ProxyId((uint)proxyServiceId, proxyMethodId));

        string loggedMsg = "";
        if (msgData.Length > 0)
        {
            if (msgData.Length > 50)
            {
                loggedMsg = Convert.ToHexString(msgData[0..50]);
            }
            else
            {
                loggedMsg = Convert.ToHexString(msgData);
            }
        }

        //Log.Logger.Information($"ParseCall: I:{proxyServiceId} S:{subId} R:{returnUid} M:{proxyMethodId} Len={data.Length} IsCompressed={isCompressed}{(loggedMsg.Length > 0 ? $"\nData: [{loggedMsg}]" : "")}");

        var id = new ProxyId((uint)proxyServiceId, proxyMethodId);
        if (ProxyHandlers.TryGetValue(id, out var handler))
        {
            var extraData = new ExtraPacketData(lastPacketTime);
            handler(msgData, returnUid, extraData);
        }
        else if (UnhandledProxyHandler != null)
        {
            var extraData = new ExtraPacketData(lastPacketTime);
            UnhandledProxyHandler(id, msgData, extraData);
        }
    }

    private void ParseFrameUp(ReadOnlySpan<byte> data, bool isCompressed, DateTime lastPacketTime)
    {
        //byte[] debugHeaders = data.ToArray();

        if (data.Length < 26)
        {
            // FrameUp is unexpectedly too small
            byte[] debugHeaders = data.ToArray();
            Log.Logger.Information($"ParseFrameUp: [{Convert.ToHexString(debugHeaders)}] Len={data.Length}");
            return;
        }

        var uuid = BinaryPrimitives.ReadUInt32BigEndian(data);
        
        int offset = 4;
        int embeddedNum = 0;

        while (offset < data.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
            int endPos = offset + (int)length;
            offset += 4;
            var flags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var padding0 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
            offset += 4;
            var proxyServiceId = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
            offset += 4;

            if (flags == 2)
            {
                var returnUid = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
                offset += 4;
                var proxyMethodId = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
                offset += 4;
                // rest of msg data...
                ReadOnlySpan<byte> msgData = data[offset..(int)endPos];
                offset += endPos - offset;
                byte[] debugMsg = msgData.ToArray();
                // We don't store the returnUid in our dictionary because there's not going to be an actual returner for it

                //Log.Logger.Information($"ParseFrameUp: U:{uuid} L:{length} F:{flags} P0:{padding0} I:{proxyServiceId} R:{returnUid} M:{proxyMethodId} MsgDataLen={msgData.Length} Len={data.Length} IsCompressed={isCompressed}");

                var id = new ProxyId(proxyServiceId, proxyMethodId);
                if (ProxyHandlers.TryGetValue(id, out var handler))
                {
                    var extraData = new ExtraPacketData(lastPacketTime);
                    handler(msgData, returnUid, extraData);
                }
                else if (UnhandledProxyHandler != null)
                {
                    var extraData = new ExtraPacketData(lastPacketTime);
                    UnhandledProxyHandler(id, msgData, extraData);
                }
            }
            else
            {
                if (data.Length < 30)
                {
                    // FrameUp is too small for this type, log it and drop rest of packet as it's no longer safe to continue
                    byte[] debugHeaders = data.ToArray();
                    Log.Logger.Information($"ParseFrameUp: [{Convert.ToHexString(debugHeaders)}] Len={data.Length}; Dropping Packet");
                    return;
                }

                var padding1 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
                offset += 4;
                var returnUid = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
                offset += 4;
                var proxyMethodId = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
                offset += 4;

                // rest of msg data...
                ReadOnlySpan<byte> msgData = data[offset..endPos];
                offset += endPos - offset;

                ProxyReturnsDictionary.AddOrUpdate(returnUid, new ProxyId(proxyServiceId, proxyMethodId), (key, value) => new ProxyId(proxyServiceId, proxyMethodId));

                //Log.Logger.Information($"ParseFrameUp: U:{uuid} L:{length} F:{flags} P0:{padding0} I:{proxyServiceId} P1:{padding1} R:{returnUid} M:{proxyMethodId} Len={data.Length} IsCompressed={isCompressed}");

                var id = new ProxyId(proxyServiceId, proxyMethodId);
                if (ProxyHandlers.TryGetValue(id, out var handler))
                {
                    var extraData = new ExtraPacketData(lastPacketTime);
                    handler(msgData, returnUid, extraData);
                }
                else if (UnhandledProxyHandler != null)
                {
                    var extraData = new ExtraPacketData(lastPacketTime);
                    UnhandledProxyHandler(id, msgData, extraData);
                }
            }

            embeddedNum++;
        }
    }

    private void ParseReturn(ReadOnlySpan<byte> data, bool isCompressed, DateTime lastPacketTime)
    {
        //byte[] debugHeaders = data.ToArray();

        if (data.Length < 12)
        {
            byte[] debugHeaders = data.ToArray();
            Log.Logger.Information($"ParseReturn: [{Convert.ToHexString(debugHeaders)}] Len={data.Length}");
            return;
        }

        var subId = BinaryPrimitives.ReadUInt32BigEndian(data);
        var returnUid = BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
        var thirdId = BinaryPrimitives.ReadUInt32BigEndian(data[8..]);

        var msgData = data[12..];

        if (isCompressed)
        {
            msgData = Decompress(msgData);

            if (msgData.IsEmpty)
            {
                Log.Logger.Warning($"Error decompressing data for {subId}, {returnUid}, {thirdId}");
                return;
            }
        }

        int protoStart = 0;
        int range = Math.Min(msgData.Length, 4);
        for (int i = 0; i < range; i++)
        {
            if (msgData[i] == 0x0A)
            {
                // Found final potential start
                protoStart = i;
            }
        }

        //Log.Logger.Information($"ParseReturn: S:{subId} R:{returnUid} T:{thirdId} Start={protoStart} Len={data.Length} IsCompressed={isCompressed}");

        if (ProxyReturnsDictionary.TryRemove(returnUid, out var frameUp))
        {
            var id = new ProxyId(frameUp.ServiceId, frameUp.MethodId);
            if (ProxyReturnHandlers.TryGetValue(id, out var handler))
            {
                ReadOnlySpan<byte> msgDataFinal = msgData[protoStart..];
                var extraData = new ExtraPacketData(lastPacketTime);
                handler(msgDataFinal, returnUid, extraData);
            }
        }
    }

    private ReadOnlySpan<byte> Decompress(ReadOnlySpan<byte> data)
    {
        try
        {
            var decompressedLen = _decompressor.Unwrap(data, DecompressionScratchBuffer.AsSpan());
            return DecompressionScratchBuffer.AsSpan()[..decompressedLen];
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error decompressing data of Len: {Len}, DecompressionScratchBuffer Size: {ScratchSize}", data.Length, DecompressionScratchBuffer.Length);
            return [];
        }
    }

    private bool IsFromGame(IPv4Packet ip, TcpPacket tcp)
    {
        var sw = Stopwatch.StartNew();
        var conns = Utils.GetTCPConnectionsForExe(Config.ExeNames);
        var isGameConnection = conns.Any((x =>
            (x.LocalAddress == ip.SourceAddress.ToString() && x.LocalPort == tcp.SourcePort) ||
            (x.RemoteAddress == ip.SourceAddress.ToString() && x.RemotePort == tcp.SourcePort) ||
            (x.LocalAddress == ip.DestinationAddress.ToString() && x.LocalPort == tcp.DestinationPort) ||
            (x.RemoteAddress == ip.DestinationAddress.ToString() && x.RemotePort == tcp.DestinationPort)));

        sw.Stop();
        Log.Logger.Debug($"Checking {ip.SourceAddress}:{tcp.SourcePort} > {ip.DestinationAddress}:{tcp.DestinationPort} is game connection: {isGameConnection}, took {sw.ElapsedMilliseconds}ms");
        
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