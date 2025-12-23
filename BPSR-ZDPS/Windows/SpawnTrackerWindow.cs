using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Managers.External;
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
    public static class SpawnTrackerWindow
    {
        public const string LAYER = "SpawnTrackerWindowLayer";
        public static string TITLE_ID = "###SpawnTrackerWindow";
        public static string TITLE = "Spawn Tracker";
        public static bool IsOpened = false;
        public static bool IsTopMost = false;
        public static bool CollapseToContentOnly = false;
        public static Vector2 DefaultWindowSize = new Vector2(700, 600);
        public static bool ResetWindowSize = false;

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;

        static bool HasInitFilterList = false;
        static Dictionary<string, bool> MonsterFilters = new();
        static CancellationTokenSource? RealtimeCancellationTokenSource = null;
        static string RegionName = "";

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            InitializeBindings();
            ConnectToBPTimer();
            ImGui.PopID();
        }

        static void ConnectToBPTimer()
        {
            if (BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.NotLoaded || BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.Error || BPTimerManager.SpawnDataRealtimeConnection == BPTimerManager.ESpawnDataLoadStatus.Error)
            {
                BPTimerManager.SpawnDataLoaded = BPTimerManager.ESpawnDataLoadStatus.InProgress;
                BPTimerManager.MobsDescriptors.Clear();
                BPTimerManager.StatusDescriptors.Clear();
                BPTimerManager.FetchAllMobs();
            }
        }

        public static void InitializeBindings()
        {
            if (HasInitBindings == false)
            {
                HasInitBindings = true;
            }
        }

        public static void InitalizeFilterList()
        {
            MonsterFilters.Clear();
            if (BPTimerManager.MobsDescriptors.Count > 0)
            {
                foreach (var mob in BPTimerManager.MobsDescriptors)
                {
                    MonsterFilters.Add(mob.MobId, false);
                }
            }
        }

        public static void CreateRealtimeConndtion()
        {
            if (RealtimeCancellationTokenSource != null)
            {
                RealtimeCancellationTokenSource.Cancel();
            }
            RealtimeCancellationTokenSource = BPTimerManager.StartRealtime();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(240, 140), new Vector2(ImGui.GETFLTMAX()));

            if (Settings.Instance.WindowSettings.SpawnTracker.WindowPosition != new Vector2())
            {
                ImGui.SetNextWindowPos(Settings.Instance.WindowSettings.SpawnTracker.WindowPosition, ImGuiCond.FirstUseEver);
            }

            if (Settings.Instance.WindowSettings.SpawnTracker.WindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(Settings.Instance.WindowSettings.SpawnTracker.WindowSize, ImGuiCond.FirstUseEver);
            }

            if (ResetWindowSize)
            {
                ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.Always);
                ResetWindowSize = false;
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (AppState.MousePassthrough && IsTopMost)
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
                }

                DrawMenuBar();

                float textScale = 18.0f * Settings.Instance.WindowSettings.SpawnTracker.TextScale;
                ImGui.PushFont(HelperMethods.Fonts["Segoe"], textScale);

                if (BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.NotLoaded)
                {
                    ImGui.TextUnformatted("No data loaded from BPTimer currently.");
                }
                else if (BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.InProgress)
                {
                    ImGui.TextUnformatted("Please wait, requesting data from BPTimer...");
                }
                else if (BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.Complete)
                {
                    if (!HasInitFilterList)
                    {
                        HasInitFilterList = true;
                        InitalizeFilterList();
                        CreateRealtimeConndtion();
                    }
                }

                if (!CollapseToContentOnly)
                {
                    ImGui.PushStyleVarX(ImGuiStyleVar.FramePadding, 4);
                    ImGui.PushStyleVarY(ImGuiStyleVar.FramePadding, 1);
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(37 / 255f, 37 / 255f, 38 / 255f, 1.0f)));
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                    ImGui.TextUnformatted("Select Monsters To Track: ");

                    ImGui.BeginDisabled(BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.InProgress);

                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X);
                    ImGui.TextUnformatted("Region: ");
                    ImGui.SameLine();
                    int selectedRegionIndex = Settings.Instance.WindowSettings.SpawnTracker.SelectedRegionIndex;
                    var regions = BPTimerManager.BPTimerRegions.ToArray();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.Combo("##RegionSelectionCombo", ref selectedRegionIndex, regions, regions.Length))
                    {
                        Settings.Instance.WindowSettings.SpawnTracker.SelectedRegionIndex = selectedRegionIndex;
                    }

                    ImGui.EndDisabled();

                    if (regions.Length > selectedRegionIndex)
                    {
                        RegionName = regions[selectedRegionIndex];
                    }

                    if (ImGui.BeginListBox("##FilterDataListBox", new Vector2(-1, 0)))
                    {
                        ImGui.PopStyleVar();

                        var mobs = BPTimerManager.MobsDescriptors.AsValueEnumerable();
                        foreach (var mob in mobs)
                        {
                            if (MonsterFilters.TryGetValue(mob.MobId, out var filterStatus))
                            {
                                bool? savedStatus = null;
                                if (Settings.Instance.WindowSettings.SpawnTracker.TrackedMonsters.TryGetValue(mob.MobId, out var saved))
                                {
                                    savedStatus = saved;
                                    MonsterFilters[mob.MobId] = saved;
                                }

                                bool isEnabled = savedStatus ?? filterStatus;
                                string name = mob.GameMobName;
                                if (string.IsNullOrEmpty(name))
                                {
                                    name = mob.MobName;
                                }
                                int totalLines = 0;
                                if (mob.MobMapTotalChannels.TryGetValue(RegionName, out var lineCount))
                                {
                                    totalLines = lineCount;
                                }
                                if (ImGui.Checkbox($"{name} [{mob.MobMapName}] ({totalLines} Lines)##MobCheckBox_{mob.MobId}", ref isEnabled))
                                {
                                    MonsterFilters[mob.MobId] = isEnabled;
                                }

                                Settings.Instance.WindowSettings.SpawnTracker.TrackedMonsters[mob.MobId] = isEnabled;
                            }
                        }

                        ImGui.EndListBox();
                    }
                    else
                    {
                        ImGui.PopStyleVar();
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar(2);
                    if (ImGui.Button("Select All"))
                    {
                        foreach (var item in MonsterFilters)
                        {
                            Settings.Instance.WindowSettings.SpawnTracker.TrackedMonsters[item.Key] = true;
                            MonsterFilters[item.Key] = true;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Select None"))
                    {
                        foreach (var item in MonsterFilters)
                        {
                            Settings.Instance.WindowSettings.SpawnTracker.TrackedMonsters[item.Key] = false;
                            MonsterFilters[item.Key] = false;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Channels To Display: ");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                    int displayCountLimit = Settings.Instance.WindowSettings.SpawnTracker.DisplayLineCountLimit;
                    if (ImGui.SliderInt("##DisplayCountLimit", ref displayCountLimit, 0, 15, $"{(displayCountLimit == 0 ? "All" : displayCountLimit)}"))
                    {
                        Settings.Instance.WindowSettings.SpawnTracker.DisplayLineCountLimit = displayCountLimit;
                    }
                    ImGui.PopStyleColor(2);

                    ImGui.Separator();
                }

                DateTime currentDateTimeUtc = DateTime.UtcNow;

                if (ImGui.BeginListBox("##StatusListBox", new Vector2(-1, -1)))
                {
                    if (BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.Complete)
                    {
                        var mobs = BPTimerManager.MobsDescriptors;
                        foreach (var mob in mobs)
                        {
                            if (MonsterFilters.TryGetValue(mob.MobId, out var filterStatus))
                            {
                                if (filterStatus == false)
                                {
                                    continue;
                                }
                            }

                            ImGui.BeginGroup();

                            string name = mob.GameMobName;
                            if (string.IsNullOrEmpty(name))
                            {
                                name = mob.MobName;
                            }
                            
                            if (mob.MobType.Equals("Boss", StringComparison.OrdinalIgnoreCase))
                            {
                                var difference = TimeUntilOccurrence(currentDateTimeUtc, mob.MobRespawnTime);

                                ImGui.Selectable($"{name} [{mob.MobMapName}] (Respawn: {difference.diff.Minutes:00}m {difference.diff.Seconds:00}s)##MobLabel_{mob.MobId}", ImGuiSelectableFlags.SpanAllColumns);

                                ImGui.ProgressBar(difference.pct, new Vector2(-1, ImGui.GetFontSize() * 0.25f), $"##OccurrenceProgressBar_{mob.MobId}");
                            }
                            else
                            {
                                ImGui.Selectable($"{name} [{mob.MobMapName}]##MobLabel_{mob.MobId}", ImGuiSelectableFlags.SpanAllColumns);
                            }

                            ImGui.EndGroup();
                            var groupSize = ImGui.GetItemRectSize();

                            var statusDescriptors = BPTimerManager.StatusDescriptors.AsValueEnumerable().Where(x => x.MobId == mob.MobId && x.Region == RegionName).OrderByDescending(x => x.UpdateTimestamp).OrderBy(x =>
                            {
                                if (x.UpdateTimestamp?.Subtract(DateTime.Now).TotalMinutes < -5 && x.LastHp != 0)
                                {
                                    // Put "expired" data at the very end
                                    return 102;
                                }
                                if (x.UpdateTimestamp?.Subtract(DateTime.Now).TotalMinutes < -6 && x.LastHp == 0)
                                {
                                    // Put old "dead" data behind expired data
                                    return 103;
                                }
                                if (x.LastHp == 0)
                                {
                                    // Recently killed enemies should be moved to the end of the line
                                    return 101;
                                }
                                return x.LastHp;
                            });

                            int currentItemCount = 1;
                            bool endedOnSameLine = false;
                            foreach (var status in statusDescriptors)
                            {
                                float lineWidth = 50 * Settings.Instance.WindowSettings.SpawnTracker.LineScale;
                                float lineHeight = 18.0f * Settings.Instance.WindowSettings.SpawnTracker.LineScale;

                                int lineItemCount = (int)MathF.Floor(groupSize.X / (lineWidth + ImGui.GetStyle().ItemSpacing.X));

                                bool shouldSameLine = currentItemCount == 0 || currentItemCount % (lineItemCount) != 0;

                                bool isDead = false;
                                bool isUnknown = false;

                                float pct = status.LastHp / 100.0f;
                                if (status.UpdateTimestamp?.Subtract(DateTime.Now).TotalMinutes < -5 && status.LastHp != 0)
                                {
                                    pct = 0.0f;
                                    isUnknown = true;
                                    // We don't call 'continue' so that Unknown lines still show up in the UI
                                    //continue;
                                }
                                else if (pct == 0.0f)
                                {
                                    pct = 1.0f;
                                    isDead = true;
                                }

                                if(Settings.Instance.WindowSettings.SpawnTracker.DisplayLineCountLimit > 0 && Settings.Instance.WindowSettings.SpawnTracker.DisplayLineCountLimit < currentItemCount)
                                {
                                    continue;
                                }

                                bool isCriticalHp = pct < 0.20 && !isDead && !isUnknown;

                                if (isCriticalHp)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Border, Colors.Red);
                                    ImGui.PushStyleColor(ImGuiCol.BorderShadow, Colors.Red_Transparent);
                                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                                }

                                if (pct < 0.30 || isDead)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(224 / 255f, 64 / 255f, 64 / 255f, 0.85f));
                                }
                                else if (pct < 0.60)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(223 / 255f, 171 / 255f, 8 / 255f, 0.85f));
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(28 / 255f, 180 / 255f, 84 / 255f, 0.85f));
                                }
                                ImGui.PushFont(HelperMethods.Fonts["Segoe"], lineHeight);
                                ImGuiEx.TextAlignedProgressBar(pct, $"{status.ChannelNumber}", 0.5f, lineWidth, lineHeight);
                                if (ImGui.BeginPopupContextItem($"##LineContextMenu_{mob.MobId}_{status.ChannelNumber}_{status.Region}"))
                                {
                                    if (ImGui.MenuItem($"Report Line {status.ChannelNumber} As Dead"))
                                    {
                                        Serilog.Log.Information("User sending manual dead monster report.");
                                        BPTimerManager.SendForceDeadReport(mob.MonsterId, (uint)status.ChannelNumber);
                                    }
                                    ImGui.EndPopup();
                                }
                                ImGui.PopFont();
                                if (shouldSameLine)
                                {
                                    ImGui.SameLine();
                                }

                                ImGui.PopStyleColor();
                                if (isCriticalHp)
                                {
                                    ImGui.PopStyleVar();
                                    ImGui.PopStyleColor(2);
                                }

                                if (isUnknown)
                                {
                                    ImGui.SetItemTooltip($"Unknown\n{status.UpdateTimestamp}");
                                }
                                else if (isDead)
                                {
                                    ImGui.SetItemTooltip($"Dead\n{status.UpdateTimestamp}");
                                }
                                else
                                {
                                    if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
                                    {
                                        ImGui.TextUnformatted($"{MathF.Round(pct * 100, 0)}%\n{status.UpdateTimestamp}");
                                        if (!string.IsNullOrEmpty(status.Location))
                                        {
                                            // Location is set when we have a mob that can spawn in multiple areas
                                            var tex = ImageArchive.LoadImage(Path.Combine("BPTimer", "Maps", $"{status.MonsterId}_{status.Location}"));
                                            if (tex != null)
                                            {
                                                float texSize = 128.0f * Settings.Instance.WindowSettings.SpawnTracker.TextScale;
                                                ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                            }
                                        }
                                        ImGui.EndTooltip();
                                    }
                                }

                                endedOnSameLine = shouldSameLine;
                                currentItemCount++;
                            }
                            if (endedOnSameLine)
                            {
                                ImGui.NewLine();
                            }
                        }
                    }

                    ImGui.EndListBox();
                }

                ImGui.PopFont();

                ImGui.End();
            }

            ImGui.PopID();
        }

        static (TimeSpan nextOccurrence, float pct, TimeSpan diff) TimeUntilOccurrence(DateTime currentDateTime, int intervalMinutes)
        {
            DateTime nextOccurrence;
            DateTime lastOccurrence;

            lastOccurrence = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, currentDateTime.Hour, intervalMinutes, 0);
            if (currentDateTime < lastOccurrence)
            {
                lastOccurrence = lastOccurrence.AddHours(-1); 
            }
            nextOccurrence = lastOccurrence.AddHours(1);

            TimeSpan cycle = TimeSpan.FromHours(1);
            TimeSpan elapsed = currentDateTime - lastOccurrence;
            TimeSpan difference = nextOccurrence - currentDateTime;

            float pct = MathF.Round(1 - ((float)elapsed.TotalSeconds / (float)cycle.TotalSeconds), 4);

            return (nextOccurrence - currentDateTime, pct, difference);
        }

        static float MenuBarButtonWidth = 0.0f;
        public static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                MenuBarSize = ImGui.GetWindowSize();

                ImGui.Text($"{TITLE}");

                ImGui.BeginDisabled(BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.InProgress);
                bool hasConnectionError = BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.Error || BPTimerManager.SpawnDataRealtimeConnection == BPTimerManager.ESpawnDataLoadStatus.Error;
                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 4));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, hasConnectionError ? 0.0f : 1.0f, hasConnectionError ? 0.0f : 1.0f, 1.0f));
                if (ImGui.MenuItem($"{FASIcons.Server}##ReconnectBtn"))
                {
                    Serilog.Log.Information("User performing manual BPTimer reconnect.");
                    BPTimerManager.SpawnDataLoaded = BPTimerManager.ESpawnDataLoadStatus.NotLoaded;
                    ConnectToBPTimer();
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Force Reconnect To BPTimer");
                ImGui.EndDisabled();

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, IsTopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}##TopMostBtn"))
                {
                    if (!IsTopMost)
                    {
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(Settings.Instance.WindowSettings.SpawnTracker.WindowOpacity);
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
                if (ImGui.MenuItem($"{(CollapseToContentOnly ? FASIcons.AnglesDown : FASIcons.AnglesUp)}##CollapseToContentBtn"))
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
                if (ImGui.MenuItem($"X##CloseBtn"))
                {
                    Settings.Instance.WindowSettings.SpawnTracker.WindowPosition = ImGui.GetWindowPos();
                    Settings.Instance.WindowSettings.SpawnTracker.WindowSize = ImGui.GetWindowSize();
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }
    }

    public class SpawnTrackerWindowSettings : WindowSettingsBase
    {
        public float WindowOpacity = 1.0f;
        public float TextScale = 1.0f;
        public float LineScale = 1.0f;
        public int DisplayLineCountLimit = 5;
        public int SelectedRegionIndex = 0;
        public Dictionary<string, bool> TrackedMonsters = new();
    }
}
