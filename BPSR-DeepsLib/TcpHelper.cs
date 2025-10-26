using System.Net;
using System.Runtime.InteropServices;

namespace BPSR_DeepsLib;

public static class TcpHelper {

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, int ipVersion, int tcpTableType, int reserved);

    [StructLayout(LayoutKind.Sequential)]
    public struct TcpRow {
        public  uint   state;
        private uint   localAddr;
        private byte   localPort1, localPort2, localPort3, localPort4;
        private uint   remoteAddr;
        private byte   remotePort1, remotePort2, remotePort3, remotePort4;
        public  int    owningPid;
        public  string LocalAddress  {get {return new IPAddress(localAddr).ToString();}}
        public  string RemoteAddress {get {return new IPAddress(remoteAddr).ToString();}}
        public  int    LocalPort     {get {return (localPort1  << 8) + localPort2;}}
        public  int    RemotePort    {get {return (remotePort1 << 8) + remotePort2;}}
    }

    public static List<TcpRow> GetExtendedTcpTable() {
        List<TcpRow> tcpRows = new List<TcpRow>();

        IntPtr tcpTablePtr = IntPtr.Zero;
        try {
            int tcpTableLength = 0;
            if (GetExtendedTcpTable(tcpTablePtr, ref tcpTableLength, false, 2, 5, 0) != 0) {
                tcpTablePtr = Marshal.AllocHGlobal(tcpTableLength);
                if (GetExtendedTcpTable(tcpTablePtr, ref tcpTableLength, false, 2, 5, 0) == 0) {
                    TcpRow tcpRow     = new TcpRow();
                    IntPtr currentPtr = tcpTablePtr + Marshal.SizeOf(typeof(uint));

                    for (int i = 0; i < tcpTableLength / Marshal.SizeOf(typeof(TcpRow)); i++) {
                        tcpRow = (TcpRow)Marshal.PtrToStructure(currentPtr, typeof(TcpRow));
                        if (tcpRow.RemoteAddress != "0.0.0.0") {tcpRows.Add(tcpRow);}
                        currentPtr += Marshal.SizeOf(typeof(TcpRow));
                    }
                }
            }
        }
        finally {
            if (tcpTablePtr != IntPtr.Zero) {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
        }
        return tcpRows;
    }
}