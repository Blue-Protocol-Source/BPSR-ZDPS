using System.Numerics;
using Hexa.NET.ImGui;
using static BPSR_DeepsLib.TcpReassembler;

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


        var netCap = MessageManager.netCap;
        if (ImGui.Begin("Network Debug"u8, ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking)) {
            if (ImGui.BeginTable("ExampleTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame)) {
                ImGui.TableNextColumn();
                ImGui.Text($"Packets in queue: {MessageManager.netCap.RawPacketQueue.Count}");
                ImGui.TableNextColumn();
                ImGui.Text($"Num Seen Packets: {MessageManager.netCap.NumSeenPackets:##,##}");
                
                ImGui.TableNextColumn();
                ImGui.Text($"Last Packet Seen: {(DateTime.Now - MessageManager.netCap.LastPacketSeenAt).TotalSeconds:00.00}s ago");
                ImGui.TableNextColumn();
                ImGui.Text($"Num Active Stream Readers: {netCap.NumConnectionReaders}");

                ImGui.EndTable();
            }
            
            if (ImGui.CollapsingHeader("Connection Filters")) {
                if (ImGui.BeginTable("SeenConnectionsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame)) {
                    ImGui.TableSetupColumn("Source");
                    ImGui.TableSetupColumn("Destination");
                    ImGui.TableSetupColumn("Is Game");
                    ImGui.TableHeadersRow();

                    foreach (var seenCon in MessageManager.netCap.ConnectionFilters) {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text($"{seenCon.Key.SrcIP}:{seenCon.Key.SrcPort}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{seenCon.Key.DstIP}:{seenCon.Key.DstPort}");

                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, seenCon.Value ? new Vector4(0.25f, 0.85f, 0.35f, 1.0f) : new Vector4(0.85f, 0.25f, 0.25f, 1.0f));
                        ImGui.Text(seenCon.Value ? "Yes" : "No");
                        ImGui.PopStyleColor();
                    }

                    ImGui.EndTable();
                }
            }
            
            if (ImGui.CollapsingHeader("Active TCP Streams")) {
                if (ImGui.BeginTable("TcpConnectionsTable", 9, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame)) {
                    ImGui.TableSetupColumn("Endpoint", ImGuiTableColumnFlags.WidthFixed, 180.0f);
                    ImGui.TableSetupColumn("Next Expected Seq", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Last Seq", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Seq Diff", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Cached", ImGuiTableColumnFlags.WidthFixed, 50.0f);
                    ImGui.TableSetupColumn("Bytes Sent", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Packets Seen", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Last Packet At", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 80.0f);
                    ImGui.TableHeadersRow();

                    foreach (var conn in MessageManager.netCap.TcpReassempler.Connections) {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(conn.Key.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(conn.Value.NextExpectedSeq.HasValue
                            ? conn.Value.NextExpectedSeq.Value.ToString()
                            : "N/A");

                        ImGui.TableNextColumn();
                        ImGui.Text(conn.Value.LastSeq.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text((conn.Value.NextExpectedSeq - conn.Value.LastSeq).ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(conn.Value.Packets.Count.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(FormatBytes(conn.Value.NumBytesSent));

                        ImGui.TableNextColumn();
                        ImGui.Text($"{conn.Value.NumPacketsSeen:###,###}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{(DateTime.Now - conn.Value.LastPacketAt).TotalSeconds:0.0}s ago");

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Remove##{conn.Key}"))
                        {
                            MessageManager.netCap.TcpReassempler.RemoveConnection(conn.Value);
                        }
                    }

                    ImGui.EndTable();
                }
            }

            if (ImGui.CollapsingHeader("Important Logs"))
            {
                if (ImGui.BeginTable("ImportantLogs", 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
                {
                    foreach (var msg in MessageManager.netCap.ImportantLogMsgs)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(msg);
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