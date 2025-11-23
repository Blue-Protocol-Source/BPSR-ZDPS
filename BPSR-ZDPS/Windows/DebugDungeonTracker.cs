using Hexa.NET.ImGui;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS.Windows
{
    public static class DebugDungeonTracker
    {
        public const string LAYER = "DebugDungeonTrackerWindowLayer";
        public static string TITLE_ID = "###DebugDungeonTracker";
        public static bool IsOpened = false;
        static int RunOnceDelayed = 0;

        //public static ConcurrentQueue<KeyValuePair< int, BPSR_DeepsLib.Blobs.DungeonTargetData>> DungeonTargetDataTracker = new();
        public static ConcurrentQueue<KeyValuePair<int, Zproto.DungeonTargetData>> DungeonTargetDataTracker = new();

        public static void Open()
        {
            RunOnceDelayed = 0;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            //ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X, io.DisplaySize.Y), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(550, 550), ImGuiCond.FirstUseEver);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.Begin($"Debug Dungeon Target Tracker###{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();
                }

                ImGui.Separator();
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Tracker [{DungeonTargetDataTracker.Count}]:");
                ImGui.SameLine();
                if (ImGui.Button("Clear Trackers"))
                {
                    DungeonTargetDataTracker.Clear();
                }

                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginListBox("##TrackerListBox", new Vector2(-1, -1)))
                {
                    for (int i = DungeonTargetDataTracker.Count - 1; i >= 0; i--)
                    {
                        var tracker = DungeonTargetDataTracker.ElementAt(i);

                        HelperMethods.DataTables.Targets.Data.TryGetValue(tracker.Key.ToString(), out var target);

                        ImGui.Selectable($"TargetKey[{i + 1}]: {tracker.Key} - [{target?.TargetDes}]\nTargetId: {tracker.Value.TargetId}\nNums: {tracker.Value.Nums}\nComplete: {tracker.Value.Complete}##TrackerListEntry_{i}", false, ImGuiSelectableFlags.SpanAllColumns);
                    }

                    ImGui.EndListBox();
                }

                ImGui.End();
            }

            ImGui.PopID();
        }
    }
}
