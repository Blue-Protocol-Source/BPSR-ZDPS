using System.Diagnostics;
using Serilog;

namespace BPSR_DeepsLib;

public class Utils
{
    public static List<TcpHelper.TcpRow> GetTCPConnectionsForExe(string[] filenames)
    {
        List<TcpHelper.TcpRow> tcpConns = [];

        List<int> pids = [];
        foreach (var filename in filenames)
        {
            var process = Process.GetProcessesByName(filename).FirstOrDefault();
            if (process != null)
            {
                pids.Add(process.Id);
            }
        }
        
        var tcpConnections = TcpHelper.GetExtendedTcpTable();
        foreach (var conn in tcpConnections) {
            if (pids.Contains(conn.owningPid)) {
                tcpConns.Add(conn);
            }
        }
        
        return tcpConns;
    }

    /*
    public static void PrintExeTCPConnections(string filename = "BPSR")
    {
        Log.Information("TCP connections for {Filename}", filename);
        Log.Information("Pid, LocalAddress, LocalPort, RemoteAddress, RemotePort, State");
        foreach (var conn in GetTCPConnectionsForExe(filename)) {
            Log.Information("{Pid}, {LocalAddress}, {LocalPort}, {RemoteAddress}, {RemotePort}, {State}",
                conn.owningPid,
                conn.LocalAddress,
                conn.LocalPort,
                conn.RemoteAddress,
                conn.RemotePort,
                conn.state);
        }
    }
    */
}