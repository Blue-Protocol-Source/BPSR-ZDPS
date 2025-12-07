using BPSR_DeepsLib;
using BPSR_ZDPS.Meters;
using Hexa.NET.ImGui;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Zproto.WorldNtfCsharp.Types;
using Zproto;
using Newtonsoft.Json;
using BPSR_ZDPS.DataTypes;
using Hexa.NET.GLFW;

namespace BPSR_ZDPS.Windows
{
    public class MainWindow
    {
        Vector2 MainMenuBarSize;

        static bool RunOnce = true;
        static int RunOnceDelayed = 0;
        static int SettingsRunOnceDelayedPerOpen = 0;

        static int SelectedTabIndex = 0;

        List<MeterBase> Meters = new();
        public EntityInspector entityInspector = new();
        public bool IsTopMost = false;
        public Vector2 WindowPosition;

        public void Draw()
        {
            DrawContent();

            SettingsWindow.Draw(this);
            EncounterHistoryWindow.Draw(this);
            entityInspector.Draw(this);
            NetDebug.Draw();
            DebugDungeonTracker.Draw(this);
            RaidManagerCooldownsWindow.Draw(this);
            RaidManagerThreatWindow.Draw(this);
            DatabaseManagerWindow.Draw(this);
            SpawnTrackerWindow.Draw(this);
            ModuleSolver.Draw();
        }

        static bool p_open = true;
        public void DrawContent()
        {
            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(550, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 150), new Vector2(ImGui.GETFLTMAX()));

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking;
            
            if (!p_open)
            {
                Hexa.NET.GLFW.GLFW.SetWindowShouldClose(HelperMethods.GLFWwindow, 1);
                return;
            }

            if (!ImGui.Begin("ZDPS", ref p_open, window_flags))
            {
                ImGui.End();
                return;
            }

            WindowPosition = ImGui.GetWindowPos();

            DrawMenuBar();

            if (RunOnce)
            {
                RunOnce = false;

                // This includes string files and caches
                AppState.LoadDataTables();

                Settings.Instance.Apply();

                ModuleSolver.Init();

                if (string.IsNullOrEmpty(MessageManager.NetCaptureDeviceName))
                {
                    var bestDefaultDevice = MessageManager.TryFindBestNetworkDevice();
                    if (bestDefaultDevice != null)
                    {
                        MessageManager.NetCaptureDeviceName = bestDefaultDevice.Name;
                        Settings.Instance.NetCaptureDeviceName = bestDefaultDevice.Name;
                    }
                }

                if (MessageManager.NetCaptureDeviceName != "")
                {
                    MessageManager.InitializeCapturing();
                }

                Meters.Add(new DpsMeter());
                Meters.Add(new HealingMeter());
                Meters.Add(new TankingMeter());
                Meters.Add(new TakenMeter());
            }
            if (RunOnceDelayed == 0)
            {
                RunOnceDelayed++;
            }
            else if (RunOnceDelayed == 1)
            {
                RunOnceDelayed++;
                unsafe
                {
                    HelperMethods.MainWindowPlatformHandleRaw = (IntPtr)ImGui.GetWindowViewport().PlatformHandleRaw;
                }

                HotKeyManager.SetWndProc();
                //HotKeyManager.SetHookProc();

                Settings.Instance.ApplyHotKeys(this);

                Utils.SetCurrentWindowIcon();
            }

            ImGuiTableFlags table_flags = ImGuiTableFlags.SizingStretchSame;
            if (ImGui.BeginTable("##MetersTable", Meters.Count, table_flags))
            {
                ImGui.TableSetupColumn("##TabBtn", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);

                for (int i = 0; i < Meters.Count; i++)
                {
                    ImGui.TableNextColumn();

                    bool isSelected = (SelectedTabIndex == i);

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5);

                    if (isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, Colors.DimGray);
                    }

                    if (ImGui.Button($"{Meters[i].Name}##TabBtn_{i}", new Vector2(-1, 0)))
                    {
                        SelectedTabIndex = i;
                    }

                    if (isSelected)
                    {
                        ImGui.PopStyleColor();
                    }

                    ImGui.PopStyleVar();
                }

                ImGui.EndTable();
            }

            ImGui.BeginChild("MeterChild", new Vector2(0, - ImGui.GetFrameHeightWithSpacing()));

            if (SelectedTabIndex > -1)
            {
                Meters[SelectedTabIndex].Draw(this);
            }

            ImGui.EndChild();

            DrawStatusBar();

            ImGui.End();
        }

        static float settingsWidth = 0.0f;
        void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                MainMenuBarSize = ImGui.GetWindowSize();

                ImGui.Text("ZDPS - BPSR Damage Meter");

                if (Utils.AppVersion != null)
                {
                    //ImGui.SetCursorPosX(MainMenuBarSize.X - (35 * 5)); // This pushes it against the previous button instead of having a gap
                    //ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X); // This loosely locks it to right side
                    ImGui.TextDisabled($"v{Utils.AppVersion}");
                }

                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 4));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.WindowMinimize}"))
                {
                    // TODO: Minimize window (might not be possible since we're not actually using GLFW windows at this point)
                    // Likely would need to use ImGuiDockSpaceOverViewport(ImGui.GetMainViewport()); for this main window to attach to platform window
                    Utils.MinimiseWindow();
                }
                ImGui.PopFont();

                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, IsTopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}")) {
                    // TODO: Make TopMost (for current and all windows)
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

                // Create new Encounter button
                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.Rotate}"))
                {
                    CreateNewEncounter();
                }
                ImGui.PopFont();
                ImGui.SetItemTooltip("Start New Encounter");

                ImGui.SetCursorPosX(MainMenuBarSize.X - settingsWidth);
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.BeginMenu($"{FASIcons.Gear}"))
                {
                    if (SettingsRunOnceDelayedPerOpen == 0)
                    {
                        SettingsRunOnceDelayedPerOpen++;
                    }
                    else if (SettingsRunOnceDelayedPerOpen == 2)
                    {
                        SettingsRunOnceDelayedPerOpen++;

                        if (IsTopMost)
                        {
                            Utils.SetWindowTopmost();
                        }
                        else
                        {
                            Utils.UnsetWindowTopmost();
                        }
                    }
                    else
                    {
                        SettingsRunOnceDelayedPerOpen++;
                    }

                    ImGui.PopFont();

                    if (ImGui.MenuItem("Encounter History"))
                    {
                        EncounterHistoryWindow.Open();
                    }

                    if (ImGui.MenuItem("Database Manager"))
                    {
                        DatabaseManagerWindow.Open();
                    }
                    ImGui.SetItemTooltip("Manage the ZDatabase.db contents");

                    if (ImGui.BeginMenu("Raid Manager"))
                    {
                        if (ImGui.MenuItem("Cooldown Priority Tracker"))
                        {
                            RaidManagerCooldownsWindow.Open();
                        }

                        if (ImGui.MenuItem("Threat Tracker"))
                        {
                            RaidManagerThreatWindow.Open();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Benchmark"))
                    {
                        ImGui.TextUnformatted("Enter how many seconds you want to run a Benchmark session for:");
                        ImGui.SetNextItemWidth(-1);
                        int benchmarkTime = AppState.BenchmarkTime;
                        ImGui.BeginDisabled(AppState.IsBenchmarkMode);
                        if (ImGui.InputInt("##BenchmarkTimeInput", ref benchmarkTime, 1, 10))
                        {
                            if (benchmarkTime < 0)
                            {
                                benchmarkTime = 0;
                            }
                            else if (benchmarkTime > 1200)
                            {
                                // Limit to 1200 Seconds (20 minutes)
                                benchmarkTime = 1200;
                            }
                            AppState.BenchmarkTime = benchmarkTime;
                        }
                        ImGui.EndDisabled();
                        
                        ImGui.TextUnformatted("Note: The Benchmark time will start after the next attack.\nOnly data for your character will be processed.");
                        if (AppState.IsBenchmarkMode)
                        {
                            if (ImGui.Button("Stop Benchmark Early", new Vector2(-1, 0)))
                            {
                                AppState.HasBenchmarkBegun = false;
                                AppState.IsBenchmarkMode = false;
                                EncounterManager.StartEncounter(false, EncounterStartReason.BenchmarkEnd);
                            }
                            ImGui.SetItemTooltip("Stops the current Benchmark before the time limit is reached.");
                        }
                        else
                        {
                            if (ImGui.Button("Start Benchmark", new Vector2(-1, 0)))
                            {
                                AppState.IsBenchmarkMode = true;
                                CreateNewEncounter();
                            }
                        }
                        
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Integrations"))
                    {
                        bool isBPTimerEnabled = Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled;
                        if (ImGui.BeginMenu("BPTimer", isBPTimerEnabled))
                        {
                            if (ImGui.MenuItem("Spawn Tracker"))
                            {
                                SpawnTrackerWindow.Open();
                            }
                            ImGui.SetItemTooltip("View Field Boss and Magical Creature spawns.\nUses the data from BPTimer.com website.");
                            ImGui.EndMenu();
                        }
                        if (!isBPTimerEnabled)
                        {
                            ImGui.SetItemTooltip("[BPTimer] must be Enabled in the [Settings > Integrations] menu.");
                        }
                        ImGui.EndMenu();
                    }

                    if (ImGui.MenuItem("Module Solver"))
                    {
                        ModuleSolver.Open();
                    }
                    ImGui.SetItemTooltip("FInd the best module combos for your build");

                    ImGui.Separator();
                    if (ImGui.MenuItem("Settings"))
                    {
                        SettingsWindow.Open();
                    }
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Debug"))
                    {
                        if (ImGui.MenuItem("Net Debug"))
                        {
                            NetDebug.Open();
                        }
                        if (ImGui.MenuItem("Dungeon Tracker"))
                        {
                            DebugDungeonTracker.Open();
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Exit"))
                    {
                        p_open = false;
                    }
                    ImGui.EndMenu();
                }
                else
                {
                    SettingsRunOnceDelayedPerOpen = 0;
                    ImGui.PopFont();
                }
                settingsWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }

        void DrawStatusBar()
        {
            ImGui.BeginChild("StatusChild", new Vector2(0, -1));

            ImGui.Text("Status:");

            ImGui.SameLine();
            // Self position in current displayed meter list
            ImGui.Text($"[{AppState.PlayerMeterPlacement}]");

            ImGui.SameLine();
            // Duration of current encounter
            string duration = "00:00:00";
            if (EncounterManager.Current?.GetDuration().TotalSeconds > 0)
            {
                duration = EncounterManager.Current.GetDuration().ToString("hh\\:mm\\:ss");
            }

            if (AppState.IsBenchmarkMode && !AppState.HasBenchmarkBegun)
            {
                duration = "00:00:00";
            }

            ImGui.Text(duration);

            if (!string.IsNullOrEmpty(EncounterManager.Current.SceneName))
            {
                ImGui.SameLine();
                // We don't need to prefix with a space due to actual item spacing handling it for us
                ImGui.TextUnformatted($"- {EncounterManager.Current.SceneName}");
            }

            if (AppState.IsBenchmarkMode)
            {
                ImGui.SameLine();
                ImGui.TextUnformatted($"[BENCHMARK ({AppState.BenchmarkTime}s)]");
            }

            ImGui.SameLine();
            string currentValuePerSecond = $"{Utils.NumberToShorthand(AppState.PlayerTotalMeterValue)} ({Utils.NumberToShorthand(AppState.PlayerMeterValuePerSecond)})";
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + float.Max(0.0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(currentValuePerSecond).X));
            ImGui.Text(currentValuePerSecond);

            ImGui.EndChild();
        }

        public void CreateNewEncounter()
        {
            EncounterManager.StopEncounter();
            Log.Information($"Starting new manual encounter at {DateTime.Now}");
            EncounterManager.StartEncounter(true);
        }
    }
}
