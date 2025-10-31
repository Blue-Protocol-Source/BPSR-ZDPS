using System.Numerics;
using Hexa.NET.ImGui;

namespace BPSR_ZDPS.Windows;

public static class NetDebug
{
    public const string LAYER = "NetDebugLayer";
    public static bool IsOpened = false;

    public static void Draw()
    {
        if (!IsOpened)
            return;
        
        ImGui.SetNextWindowSize(new Vector2(1000, 600), ImGuiCond.FirstUseEver);
        ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

        if (ImGui.Begin("Network Debug"u8, ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking)) {
            ImGui.Text($"Packets in queue: {MessageManager.netCap.RawPacketQueue.Count}");
            
            if (ImGui.CollapsingHeader("Seen Connections")) {
                if (ImGui.BeginTable("SeenConnectionsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame)) {
                    ImGui.TableSetupColumn("IP Address");
                    ImGui.TableSetupColumn("First Seen");
                    ImGui.TableSetupColumn("Is Game Connection");
                    ImGui.TableHeadersRow();

                    lock (MessageManager.netCap.TcpReassempler.Connections) {
                        foreach (var seenCon in MessageManager.netCap.SeenConnectionStates) {
                            ImGui.TableNextRow();

                            ImGui.TableNextColumn();
                            ImGui.Text(seenCon.Key.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(seenCon.Value.FirstSeenAt.ToString("HH:mm:ss"));

                            ImGui.TableNextColumn();
                            ImGui.Text(seenCon.Value.IsGameConnection.HasValue
                                ? (seenCon.Value.IsGameConnection.Value ? "Yes" : "No")
                                : "Unknown");
                        }
                    }

                    ImGui.EndTable();
                }
            }
            
            if (ImGui.CollapsingHeader("Active TCP Connections")) {
                if (ImGui.BeginTable("TcpConnectionsTable", 8, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame)) {
                    ImGui.TableSetupColumn("Endpoint", ImGuiTableColumnFlags.WidthFixed, 180.0f);
                    ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 60.0f);
                    ImGui.TableSetupColumn("Next Expected Seq", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Last Seq", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Cached", ImGuiTableColumnFlags.WidthFixed, 50.0f);
                    ImGui.TableSetupColumn("Bytes Sent", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Packets Seen", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Last Packet At", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    lock (MessageManager.netCap.TcpReassempler.Connections) {
                        foreach (var conn in MessageManager.netCap.TcpReassempler.Connections) {
                            ImGui.TableNextRow();

                            ImGui.TableNextColumn();
                            ImGui.Text(conn.Key.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(conn.Value.IsAlive ? "Alive" : "Dead");

                            ImGui.TableNextColumn();
                            ImGui.Text(conn.Value.NextExpectedSeq.HasValue
                                ? conn.Value.NextExpectedSeq.Value.ToString()
                                : "N/A");

                            ImGui.TableNextColumn();
                            ImGui.Text(conn.Value.LastSeq.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(conn.Value.Packets.Count.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(FormatBytes(conn.Value.NumBytesSent));

                            ImGui.TableNextColumn();
                            ImGui.Text($"{conn.Value.NumPacketsSeen:###,###}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{(DateTime.Now - conn.Value.LastPacketAt).TotalSeconds:0.0}s ago");
                        }
                    }

                    ImGui.EndTable();
                }
            }
            
            ImGui.End();
        }
        
        ImGui.PopID();
    }
    
    public static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}