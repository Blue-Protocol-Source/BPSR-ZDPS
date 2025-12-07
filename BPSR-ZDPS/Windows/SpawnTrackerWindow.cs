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

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;

        // TODO: Move into Settings
        static int DisplayCountLimit = 5;

        static bool HasInitFilterList = false;
        static Dictionary<string, bool> MonsterFilters = new();
        static CancellationTokenSource? RealtimeCancellationTokenSource = null;

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            InitializeBindings();
            if (BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.NotLoaded || BPTimerManager.SpawnDataLoaded == BPTimerManager.ESpawnDataLoadStatus.Error)
            {
                BPTimerManager.SpawnDataLoaded = BPTimerManager.ESpawnDataLoadStatus.InProgress;
                BPTimerManager.MobsDescriptors.Clear();
                BPTimerManager.StatusDescriptors.Clear();
                BPTimerManager.FetchAllMobs();
            }
            ImGui.PopID();
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

            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(240, 140), new Vector2(ImGui.GETFLTMAX()));

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
                    if (ImGui.BeginListBox("##FilterDataListBox", new Vector2(-1, 0)))
                    {
                        ImGui.PopStyleVar();

                        var mobs = BPTimerManager.MobsDescriptors.AsValueEnumerable();
                        foreach (var mob in mobs)
                        {
                            if (MonsterFilters.TryGetValue(mob.MobId, out var filterStatus))
                            {
                                bool isEnabled = filterStatus;
                                string name = mob.GameMobName;
                                if (string.IsNullOrEmpty(name))
                                {
                                    name = mob.MobName;
                                }
                                if (ImGui.Checkbox($"{name} [{mob.MobMapName}] ({mob.MobMapTotalChannels} Lines)##MobCheckBox_{mob.MobId}", ref isEnabled))
                                {
                                    MonsterFilters[mob.MobId] = isEnabled;
                                }
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
                            MonsterFilters[item.Key] = true;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Select None"))
                    {
                        foreach (var item in MonsterFilters)
                        {
                            MonsterFilters[item.Key] = false;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Channels To Display: ");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                    int displayCountLimit = DisplayCountLimit;
                    ImGui.SliderInt("##DisplayCountLimit", ref DisplayCountLimit, 0, 15, $"{(DisplayCountLimit == 0 ? "All" : DisplayCountLimit)}");
                    ImGui.PopStyleColor(2);

                    ImGui.Separator();
                }

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
                            ImGui.Selectable($"{name} [{mob.MobMapName}]##MobBtn_{mob.MobId}", ImGuiSelectableFlags.SpanAllColumns);

                            ImGui.EndGroup();
                            var groupSize = ImGui.GetItemRectSize();

                            var statusDescriptors = BPTimerManager.StatusDescriptors.AsValueEnumerable().Where(x => x.MobId == mob.MobId).OrderByDescending(x => x.UpdateTimestamp).OrderBy(x =>
                            {
                                if (x.UpdateTimestamp?.Subtract(DateTime.Now).TotalMinutes < -5)
                                {
                                    // Put "expired" data at the very end
                                    return 102;
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
                                /*if (status.MobId != mob.MobId)
                                {
                                    continue;
                                }*/

                                float lineWidth = 50;
                                int lineItemCount = (int)MathF.Floor(groupSize.X / (lineWidth + ImGui.GetStyle().ItemSpacing.X));

                                bool shouldSameLine = currentItemCount == 0 || currentItemCount % (lineItemCount) != 0;

                                bool isDead = false;
                                bool isUnknown = false;

                                float pct = status.LastHp / 100.0f;
                                if (status.UpdateTimestamp?.Subtract(DateTime.Now).TotalMinutes < -5)
                                {
                                    pct = 0.0f;
                                    isUnknown = true;
                                    //continue;
                                }
                                else if (pct == 0.0f)
                                {
                                    pct = 1.0f;
                                    isDead = true;
                                }

                                if(DisplayCountLimit > 0 && DisplayCountLimit < currentItemCount)
                                {
                                    continue;
                                }

                                // TODO: Sort by lowest HP first and only show up to 3 lines of data when including "expired" lines

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

                                ImGuiEx.TextAlignedProgressBar(pct, $"{status.ChannelNumber}", 0.5f, lineWidth, 18);
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
                                    //ImGui.SetItemTooltip($"{MathF.Round(pct * 100, 0)}%%\n{status.UpdateTimestamp}");
                                    if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
                                    {
                                        ImGui.TextUnformatted($"{MathF.Round(pct * 100, 0)}%\n{status.UpdateTimestamp}");
                                        if (!string.IsNullOrEmpty(status.Location))
                                        {
                                            // Location is set when we have a mob that can spanw in multiple areas
                                            var tex = ImageArchive.LoadImage(Path.Combine("BPTimer", "Maps", $"{status.MonsterId}_{status.Location}"));
                                            if (tex != null)
                                            {
                                                ImGui.Image((ImTextureRef)tex, new Vector2(128, 128));
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

                /*if (ImGui.Button("Get Data"))
                {
                    BPTimerManager.MobsDescriptors.Clear();
                    BPTimerManager.StatusDescriptors.Clear();
                    BPTimerManager.FetchAllMobs();
                }

                ImGui.SameLine();
                if (ImGui.Button("Realtime"))
                {
                    if (RealtimeCancellationTokenSource != null)
                    {
                        RealtimeCancellationTokenSource.Cancel();
                    }
                    RealtimeCancellationTokenSource = BPTimerManager.StartRealtime();
                }*/

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

                ImGui.Text($"{TITLE}");

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
