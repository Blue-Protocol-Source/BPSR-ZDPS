using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Windows
{
    public static class RaidManagerThreatWindow
    {
        public const string LAYER = "RaidManagerWindowLayer";
        public static string TITLE_ID = "###RaidManagerThreatTrackerWindow";
        public static string TITLE = "Threat Tracker";
        public static bool IsOpened = false;
        public static bool IsTopMost = false;
        public static bool CollapseToContentOnly = false;

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;

        static Dictionary<long, ThreatInfo> TrackedEntities = new();

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            InitializeBindings();
            ImGui.PopID();
        }

        public static void InitializeBindings()
        {
            if (HasInitBindings == false)
            {
                HasInitBindings = true;
                EncounterManager.EncounterStart += ThreatTracker_EncounterStart;
                EncounterManager.EncounterEndFinal += ThreatTracker_EncounterEndFinal;
            }
        }

        private static void ThreatTracker_EncounterEndFinal(EncounterEndFinalData e)
        {
            EncounterManager.Current.EntityThreatListUpdated -= ThreatTracker_Entity_ThreatListUpdated;
            System.Diagnostics.Debug.WriteLine("ThreatTracker_EncounterEndFinal");
        }

        private static void ThreatTracker_Entity_ThreatListUpdated(object sender, ThreatListUpdatedEventArgs e)
        {
            TrackedEntities[e.EntityUuid] = e.ThreatInfo;
        }

        private static void ThreatTracker_EncounterStart(EventArgs e)
        {
            BindCurrentEncounterEvents();
        }

        private static void BindCurrentEncounterEvents()
        {
            EncounterManager.Current.EntityThreatListUpdated -= ThreatTracker_Entity_ThreatListUpdated;
            EncounterManager.Current.EntityThreatListUpdated += ThreatTracker_Entity_ThreatListUpdated;
            System.Diagnostics.Debug.WriteLine("BindCurrentEncounterEvents");
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 240), new Vector2(ImGui.GETFLTMAX()));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.Begin($"{TITLE}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking))
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

                DrawMenuBar();

                ImGui.PushStyleVarX(ImGuiStyleVar.FramePadding, 4);
                ImGui.PushStyleVarY(ImGuiStyleVar.FramePadding, 1);
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(37 / 255f, 37 / 255f, 38 / 255f, 1.0f)));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                if (ImGui.BeginListBox("##ThreatsListBox", new Vector2(-1,-1)))
                {
                    ImGui.PopStyleVar();

                    int trackedEntityIdx = 0;

                    foreach (var trackedEntity in TrackedEntities)
                    {
                        var trackedName = EntityCache.Instance.Cache.Lines[trackedEntity.Key]?.Name;
                        ImGui.TextUnformatted($"{(!string.IsNullOrEmpty(trackedName) ? trackedName: trackedEntity.Key)}");

                        ImGui.Indent();
                        float indentOffset = ImGui.GetCursorPosX();
                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Colors.DarkRed);
                        var threatName = EntityCache.Instance.Cache.Lines[trackedEntity.Value.EntityUuid]?.Name;
                        ImGui.ProgressBar(1, new Vector2(-1, 18), $"{(!string.IsNullOrEmpty(threatName) ? threatName : trackedEntity.Value.EntityUuid)} :: {trackedEntity.Value.ThreatValue}");
                        ImGui.PopStyleColor();
                        ImGui.Unindent();

                        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(78 / 255f, 78 / 255f, 78 / 255f, 1.0f));
                        ImGui.Separator();
                        ImGui.PopStyleColor();

                        trackedEntityIdx++;
                    }

                    ImGui.EndListBox();
                }
                else
                {
                    ImGui.PopStyleVar();
                }
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);

                ImGui.End();
            }

            ImGui.PopID();
        }

        static float MenuBarButtonWidth = 0.0f;
        public static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                MenuBarSize = ImGui.GetWindowSize();

                ImGui.Text($"Raid Manager - {TITLE}");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, IsTopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}"))
                {
                    if (!IsTopMost)
                    {
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(Settings.Instance.WindowOpacity);
                        IsTopMost = true;
                    }
                    else
                    {
                        Utils.UnsetWindowTopmost();
                        Utils.SetWindowOpacity(1.0f);
                        IsTopMost = false;
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Pin Window As Top Most");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, CollapseToContentOnly ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{(CollapseToContentOnly ? FASIcons.AnglesDown : FASIcons.AnglesUp)}"))
                {
                    CollapseToContentOnly = !CollapseToContentOnly;
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                if (CollapseToContentOnly)
                {
                    ImGui.SetItemTooltip("Expand To Full Options");
                }
                else
                {

                    ImGui.SetItemTooltip("Collapse To Content Only");
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"X"))
                {
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }
    }
}
