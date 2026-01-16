using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

namespace BPSR_ZDPS.Windows
{
    public static class RaidManagerThreatWindow
    {
        public const string LAYER = "RaidManagerWindowLayer";
        public static string TITLE_ID = "###RaidManagerThreatTrackerWindow";
        public static string TITLE = "Threat Tracker";
        public static bool IsOpened = false;
        public static bool CollapseToContentOnly = false;

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;
        static int LastPinnedOpacity = 100;
        static bool IsPinned = false;

        static Dictionary<long, List<ThreatInfo>> TrackedEntities = new();

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
            e.Encounter.EntityThreatListUpdated -= ThreatTracker_Entity_ThreatListUpdated;
            System.Diagnostics.Debug.WriteLine("ThreatTracker_EncounterEndFinal");
        }

        private static void ThreatTracker_Entity_ThreatListUpdated(object sender, ThreatListUpdatedEventArgs e)
        {
            TrackedEntities[e.EntityUuid] = e.ThreatInfoList;
        }

        private static void ThreatTracker_EncounterStart(EventArgs e)
        {
            BindCurrentEncounterEvents();
        }

        private static void BindCurrentEncounterEvents()
        {
            TrackedEntities.Clear();

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

            var windowSettings = Settings.Instance.WindowSettings.RaidManagerThreat;

            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 240), new Vector2(ImGui.GETFLTMAX()));

            if (windowSettings.WindowPosition != new Vector2())
            {
                ImGui.SetNextWindowPos(windowSettings.WindowPosition, ImGuiCond.FirstUseEver);
            }

            if (windowSettings.WindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(windowSettings.WindowSize, ImGuiCond.FirstUseEver);
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (AppState.MousePassthrough && windowSettings.TopMost)
            {
                exWindowFlags |= ImGuiWindowFlags.NoInputs;
            }

            if (ImGui.Begin($"{TITLE}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking | exWindowFlags))
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

                    if (windowSettings.TopMost && !IsPinned)
                    {
                        IsPinned = true;
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }
                else if (RunOnceDelayed >= 2)
                {
                    if (windowSettings.TopMost && LastPinnedOpacity != windowSettings.Opacity)
                    {
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
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

                    foreach (var trackedEntity in TrackedEntities.AsValueEnumerable())
                    {
                        var threatList = trackedEntity.Value.AsValueEnumerable().OrderByDescending(x => x.ThreatValue).Where(x => Utils.UuidToEntityType(x.EntityUuid) == (long)Zproto.EEntityType.EntChar);

                        // Only show entities with active threat lists
                        int threatListCount = threatList.Count();
                        if (threatListCount == 0)
                        {
                            continue;
                        }

                        var trackedName = EntityCache.Instance.Cache.Lines[trackedEntity.Key]?.Name;
                        ImGui.TextUnformatted($"{(!string.IsNullOrEmpty(trackedName) ? trackedName: trackedEntity.Key)}");
                        
                        // This is the second highest threat as main target will be at a threat level far too high above others to matter for this
                        long topThreatValue = 0;
                        if (threatListCount > 1)
                        {
                            topThreatValue = threatList.ElementAt(1).ThreatValue;
                        }

                        int threatListIdx = 0;
                        foreach (var threat in threatList)
                        {
                            ImGui.Indent();
                            float indentOffset = ImGui.GetCursorPosX();
                            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Colors.DarkRed);
                            var threatName = EntityCache.Instance.Cache.Lines[threat.EntityUuid]?.Name;

                            float progressPct = 1.0f;
                            if (threatListIdx > 0 && topThreatValue > 0)
                            {
                                progressPct = (float)Math.Round((double)threat.ThreatValue / (double)topThreatValue, 4);
                            }

                            ImGui.ProgressBar(progressPct, new Vector2(-1, 18), $"{(!string.IsNullOrEmpty(threatName) ? threatName : threat.EntityUuid)} :: {threat.ThreatValue}");
                            ImGui.PopStyleColor();
                            ImGui.Unindent();

                            if (threatListIdx == 0 && threatListCount > 1)
                            {
                                ImGui.Dummy(new Vector2(1, 5));
                            }

                            threatListIdx++;
                        }

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
                var windowSettings = Settings.Instance.WindowSettings.RaidManagerThreat;

                MenuBarSize = ImGui.GetWindowSize();

                ImGui.Text($"Raid Manager - {TITLE} (ZDPS BETA)");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 4));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.Question}"))
                {

                }
                ImGui.PopFont();
                ImGui.SetItemTooltip($"This feature is in Beta Testing. Please provide feedback to help improve it.");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, windowSettings.TopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}"))
                {
                    if (!windowSettings.TopMost)
                    {
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                        windowSettings.TopMost = true;
                        IsPinned = true;
                    }
                    else
                    {
                        Utils.UnsetWindowTopmost();
                        Utils.SetWindowOpacity(1.0f);
                        windowSettings.TopMost = false;
                        IsPinned = false;
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
                    windowSettings.WindowPosition = ImGui.GetWindowPos();
                    windowSettings.WindowSize = ImGui.GetWindowSize();
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }
    }

    public class RaidManagerThreatWindowSettings : WindowSettingsBase
    {

    }
}
