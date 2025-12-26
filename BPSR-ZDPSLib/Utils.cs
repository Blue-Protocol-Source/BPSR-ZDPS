using System.Diagnostics;
using Serilog;

namespace BPSR_DeepsLib;

public class Utils
{
    private static Dictionary<string, ProcessCacheEntry> ProcessCache = [];

    public static List<TcpHelper.TcpRow> GetTCPConnectionsForExe(string[] filenames)
    {
        List<TcpHelper.TcpRow> tcpConns = [];

        var procs = GetProcessesFromList(filenames);
        var sw = Stopwatch.StartNew();
        List<int> pids = [];
        foreach (var filename in filenames)
        {
            if (ProcessCache.TryGetValue(filename, out var processCache))
            {
                // Check that it is sill running
                if (Kernel32.IsProcessAlive(processCache.ProcessId))
                {
                    pids.Add(processCache.ProcessId);
                    continue;
                }
                else
                {
                    ProcessCache.Remove(filename);
                }
            }

            var process = procs.TryGetValue(filename, out var tempProcess) ? tempProcess : null;
            if (process != null)
            {
                ProcessCache.Add(filename, new ProcessCacheEntry()
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                });

                pids.Add(process.Id);
            }
        }
        sw.Stop();
        Log.Information("GetProcesses took: {time}ms", sw.ElapsedMilliseconds);

        var tcpConnections = TcpHelper.GetExtendedTcpTable();
        foreach (var conn in tcpConnections) {
            if (pids.Contains(conn.owningPid)) {
                tcpConns.Add(conn);
            }
        }

        return tcpConns;
    }

    public static Dictionary<string, Process> GetProcessesFromList(string[] filenames)
    {
        var processesDict = new Dictionary<string, Process>();
        var processes = Process.GetProcesses();
        foreach (var process in processes)
        {
            if (filenames.Contains(process.ProcessName))
            {
                processesDict.TryAdd(process.ProcessName, process);
            }
        }

        return processesDict;
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

    public class ProcessCacheEntry
    {
        public int ProcessId;
        public string ProcessName;
    }
}