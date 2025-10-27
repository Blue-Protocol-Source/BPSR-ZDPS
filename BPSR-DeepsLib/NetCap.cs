using BPSR_DeepsLib.ServiceMethods;
using Microsoft.Extensions.ObjectPool;
using PacketDotNet;
using PacketDotNet.Connections;
using Serilog;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using Zproto;
using ZstdSharp;

namespace BPSR_DeepsLib;

public class NetCap
{
    private NetCapConfig Config;
    private ICaptureDevice CaptureDevice;
    private TcpConnectionManager TcpConnectionManager;
    private string LocalAddress;

    private CancellationTokenSource CancelTokenSrc = new();
    private ObjectPool<RawPacket> RawPacketPool = ObjectPool.Create(new DefaultPooledObjectPolicy<RawPacket>());
    private ConcurrentQueue<RawPacket> RawPacketQueue = new();
    private Task PacketParseTask;
    private Task ConnectionScanTask;
    private List<string> ServerConnections = [];
    private byte[] DecompressionScratchBuffer = new byte[1024 * 1024];
    private Dictionary<NotifyId, Action<ReadOnlySpan<byte>>> NotifyHandlers = new();
    private Dictionary<IPAddress, PendingConnState> SeenConnectionStates = [];

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
        //ConnectionScanTask = Task.Factory.StartNew(ScanForConnections, CancelTokenSrc.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        TcpConnectionManager = new TcpConnectionManager();
        TcpConnectionManager.ConnectionTimeout = TimeSpan.FromSeconds(5); // Timeout after 3 seconds (that seems like more than enough time)
        TcpConnectionManager.OnConnectionFound += TcpConnectionManagerOnOnConnectionFound;

        var conns = Utils.GetTCPConnectionsForExe(Config.ExeName)
                         .Where(x => x.RemoteAddress != "127.0.0.1" && x.RemotePort != 443 && x.RemotePort != 80);

        Log.Information("Found TCP connections: {Connections}", conns.Select(x => $"{x.RemoteAddress}:{x.RemotePort}"));
        LocalAddress = conns.Select(x => x.LocalAddress).FirstOrDefault();
        Log.Information("Local address: {LocalAddress}", LocalAddress);

        CaptureDevice.Filter = "tcp";
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

        if (!IsGamePacket(ipv4, tcpPacket))
            return;

        TcpConnectionManager.ProcessPacket(rawPacket.Timeval, tcpPacket);
    }

    private bool IsGameConnection(IPv4Packet ip, TcpPacket tcp)
    {
        var destAddr = $"{ip.DestinationAddress}:{tcp.DestinationPort}";
        var srcAddr = $"{ip.SourceAddress}:{tcp.SourcePort}";

        lock (ServerConnections)
        {
            for (int i = 0; i < ServerConnections.Count; i++)
            {
                if (destAddr == ServerConnections[i] || srcAddr == ServerConnections[i])
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Look for a packet that matches what we expect from a game packet
    // eg the len makes sense and the msg type is in a valid range
    private bool IsGamePacket(IPv4Packet ip, TcpPacket tcp)
    {
        if (tcp.DestinationPort <= 1000 || tcp.SourcePort <= 1000)
            return false;

        PendingConnState state;
        var hadState = SeenConnectionStates.TryGetValue(ip.DestinationAddress, out state);
        if (hadState && state.IsGameConnection.HasValue)
        {
            return state.IsGameConnection.Value;
        }
        else if (hadState && !state.IsGameConnection.HasValue)
        {
            var noPacketInTimeFrame = (DateTime.Now - state.FirstSeenAt) > TimeSpan.FromSeconds(10);
            if (noPacketInTimeFrame)
            {
                state.IsGameConnection = false;
                return false;
            }
        }
        else if (!hadState)
        {
            state = new PendingConnState(ip.DestinationAddress);
            SeenConnectionStates.TryAdd(ip.DestinationAddress, state);
        }

        var data = tcp.PayloadData;
        if (data.Length >= 6)
        {
            var len = BinaryPrimitives.ReadUInt32BigEndian(data);
            var rawMsgType = BinaryPrimitives.ReadInt16BigEndian(data[4..]);
            var msgType = (rawMsgType & 0x7FFF);
            if (len == data.Length && msgType >= 0 && msgType <= 8)
            {
                state.IsGameConnection = true;
                return true;
            }
        }

        return false;
    }

    private void TcpConnectionManagerOnOnConnectionFound(TcpConnection c)
    {
        var tcpGens = new Dictionary<TcpFlow, TcpStreamGenerator>();
        var fromFlow = c.Flows[0];
        var toFlow = c.Flows[1];
        var tcpGenFrom = new TcpStreamGenerator(fromFlow, TimeSpan.FromSeconds(5), null);
        var tcpGenTo = new TcpStreamGenerator(toFlow, TimeSpan.FromSeconds(5), null);
        var connStatus = new TcpConnStatus();

        var isFromLocal = fromFlow.address.ToString() == LocalAddress;
        tcpGenFrom.OnCallback += (gen, condition) => HandleFlowPacket(fromFlow, gen, toFlow, !isFromLocal, connStatus);
        tcpGenTo.OnCallback += (gen, condition) => HandleFlowPacket(toFlow, gen, fromFlow, isFromLocal, connStatus);

        tcpGens.Add(toFlow, tcpGenFrom);
        tcpGens.Add(fromFlow, tcpGenTo);
    }

    private TcpStreamGenerator.CallbackReturnValue HandleFlowPacket(TcpFlow fromFlow, TcpStreamGenerator gen, TcpFlow? toFlow, bool fromServer, TcpConnStatus connStatus)
    {
        var tcpPayloadSize = gen.tcpStream.Length - gen.tcpStream.Position;
        if (tcpPayloadSize >= 6)
        {
            Span<byte> header = stackalloc byte[6];
            gen.tcpStream.ReadExactly(header);
            gen.tcpStream.Seek(-6, SeekOrigin.Current);
            var len = BinaryPrimitives.ReadUInt32BigEndian(header);
            var opCode = BinaryPrimitives.ReadUInt16BigEndian(header[4..]);
            var canReadWholeMsg = tcpPayloadSize >= len;
            var flow = fromServer ? toFlow : fromFlow;

            if (!canReadWholeMsg)
            {
                return TcpStreamGenerator.CallbackReturnValue.ContinueMonitoring;
            }

            if (fromServer)
            {
                SplitIntoMessages(fromFlow, fromServer, gen.tcpStream);
                //return TcpStreamGenerator.CallbackReturnValue.ContinueMonitoring;
            }

            if (!fromServer)
            {
                SplitIntoMessages(fromFlow, fromServer, gen.tcpStream);
                //return TcpStreamGenerator.CallbackReturnValue.ContinueMonitoring;
            }

            // Try to sync to a message boundary
            if (fromServer)
            {
                if (!connStatus.IsServerSyncedUp)
                {
                    if (len == 6 && opCode == 4)
                    {
                        connStatus.IsServerSyncedUp = true;
                        Log.Information("Server is synced up");
                    }
                    else
                    {
                        // Not the keep alive? packet, so we keep looking on the next packet
                        gen.tcpStream.Seek(0, SeekOrigin.End);
                        return TcpStreamGenerator.CallbackReturnValue.ContinueMonitoring;
                    }
                }
            }
            else
            {
                if (!connStatus.IsClientSyncedUp)
                {
                    if (len == 6 && opCode == 4)
                    {
                        connStatus.IsClientSyncedUp = true;
                        Log.Information("Client is synced up");
                    }
                    else
                    {
                        // Not the keep alive? packet, so we keep looking on the next packet
                        gen.tcpStream.Seek(0, SeekOrigin.End);
                        return TcpStreamGenerator.CallbackReturnValue.ContinueMonitoring;
                    }
                }
            }

            if (gen.tcpStream.Position == gen.tcpStream.Length)
            {
                gen.FreePackets();
                var buffer = gen.tcpStream.GetBuffer();
                Array.Clear(buffer, 0, buffer.Length);
                gen.tcpStream.Position = 0;
                gen.tcpStream.SetLength(0);
                gen.tcpStream.Capacity = 0;
            }
        }

        return TcpStreamGenerator.CallbackReturnValue.ContinueMonitoring;
    }

    private void SplitIntoMessages(TcpFlow flow, bool isFromServer, TcpStream stream)
    {
        Span<byte> header = stackalloc byte[6];

        while (stream.Position + 6 <= stream.Length)
        {
            stream.ReadExactly(header);
            stream.Seek(-header.Length, SeekOrigin.Current);
            var len = BinaryPrimitives.ReadUInt32BigEndian(header);
            var msgType = BinaryPrimitives.ReadInt16BigEndian(header[4..]);
            var canReadWholeMsg = stream.Length - stream.Position >= len;

            if (!canReadWholeMsg)
            {
                return;
            }

            var rawPacket = RawPacketPool.Get();
            rawPacket.Set(isFromServer, (int)len);
            stream.ReadExactly(rawPacket.Data.AsSpan()[..(int)len]);
            RawPacketQueue.Enqueue(rawPacket);
        }
    }

    private void ParsePacketsLoop()
    {
        while (!CancelTokenSrc.IsCancellationRequested)
        {
            if (RawPacketQueue.TryDequeue(out var rawPacket))
            {
                ParsePacket(rawPacket.IsFromServer, rawPacket.Data[..rawPacket.Len]);

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

    private void ParsePacket(bool isFromServer, ReadOnlySpan<byte> data)
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

            // HACK: Fix crashing when in very high populated areas (like the town crafting spot)
            if ((int)len > msgData.Length)
            {
                System.Diagnostics.Debug.WriteLine("ParsePacket (len > msgData.Length) !! Skipping packet to recover");
                return;
            }

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
            ParsePacket(true, decompressed);
        }
        else
        {
            ParsePacket(true, data[4..]);
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
        // Seems to only work on streams
        var ms = new MemoryStream(data.ToArray());
        var stream = new DecompressionStream(ms);
        var decompressedLen = stream.Read(DecompressionScratchBuffer);

        return DecompressionScratchBuffer.AsSpan()[..decompressedLen];
    }

    private void ScanForConnections()
    {
        while (!CancelTokenSrc.IsCancellationRequested)
        {
            var conns = GetConnections();
            lock (ServerConnections)
            {
                ServerConnections.Clear();
                ServerConnections.AddRange(conns.Select(x => $"{x.RemoteAddress}:{x.RemotePort}"));
            }

            Task.Delay(Config.ConnectionScanInterval).Wait();
        }
    }

    private IEnumerable<TcpHelper.TcpRow> GetConnections()
    {
        var conns = Utils.GetTCPConnectionsForExe(Config.ExeName)
                         .Where(x => x.RemoteAddress != "127.0.0.1" && x.RemotePort != 443 && x.RemotePort != 80);

        return conns;
    }

    public void Stop()
    {
        CancelTokenSrc.Cancel();

        if (CaptureDevice != null)
        {
            CaptureDevice.StopCapture();
            CaptureDevice.Close();
            ServerConnections.Clear();
            SeenConnectionStates.Clear();

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

public class NetCapStats
{
    public ConcurrentDictionary<string, int> NumPacketsPerConnection { get; set; } = [];
}