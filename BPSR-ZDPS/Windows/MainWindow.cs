using BPSR_ZDPSLib;
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

        static bool HasPromptedUpdateWindow = false;
        static bool HasPromptedOneTimeEnableUpdateChecks = false;

        static bool ProcessingDbWork = false;
        static bool ResumeFromDbWork = false;

        List<MeterBase> Meters = new();
        public EntityInspector entityInspector = new();
        static bool IsPinned = false;
        static int LastPinnedOpacity = 100;
        public Vector2 WindowPosition;
        public Vector2 NextWindowPosition = new();
        public Vector2 DefaultWindowSize = new Vector2(550, 600);
        public Vector2 WindowSize;
        public Vector2 NextWindowSize = new();

        public void Draw()
        {
            DrawContent();

            UpdateCheckPromptWindow.Draw(this);
            UpdateAvailableWindow.Draw(this);
            DatabaseMigrationWindow.Draw(this);
            SettingsWindow.Draw(this);
            EncounterHistoryWindow.Draw(this);
            entityInspector.Draw(this);
            NetDebug.Draw();
            DebugDungeonTracker.Draw(this);
            RaidManagerCooldownsWindow.Draw(this);
            DatabaseManagerWindow.Draw(this);
            SpawnTrackerWindow.Draw(this);
            ModuleSolver.Draw();
            EntityCacheViewerWindow.Draw(this);
            RaidManagerRaidWarningWindow.Draw(this);
            RaidManagerCountdownWindow.Draw(this);
            ChatWindow.Draw(this);
        }

        static bool p_open = true;
        public void DrawContent()
        {
            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.FirstUseEver);

            if (!Settings.Instance.AllowEncounterSavingPausingInOpenWorld)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(375, 150), new Vector2(ImGui.GETFLTMAX()));
            }
            else
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(400, 220), new Vector2(ImGui.GETFLTMAX()));
            }

            var windowSettings = Settings.Instance.WindowSettings.MainWindow;

            if (windowSettings.WindowPosition != new Vector2())
            {
                ImGui.SetNextWindowPos(windowSettings.WindowPosition, ImGuiCond.FirstUseEver);
            }

            if (windowSettings.WindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(windowSettings.WindowSize, ImGuiCond.FirstUseEver);
            }

            if (NextWindowPosition != new Vector2())
            {
                ImGui.SetNextWindowPos(NextWindowPosition * new Vector2(0.5f, 0.5f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                NextWindowPosition = new Vector2();
            }

            if (NextWindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(NextWindowSize, ImGuiCond.Always);
                NextWindowSize = new Vector2();
            }

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (AppState.MousePassthrough && windowSettings.TopMost)
            {
                exWindowFlags |= ImGuiWindowFlags.NoInputs;
            }

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking | exWindowFlags;
            
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
            WindowSize = ImGui.GetWindowSize();

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

                if (DB.CheckIfMigrationsNeeded())
                {
                    ProcessingDbWork = true;
                }
                else
                {
                    if (MessageManager.NetCaptureDeviceName != "")
                    {
                        MessageManager.InitializeCapturing();
                    }
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
                Utils.BringWindowToFront();

                if (windowSettings.TopMost && !IsPinned)
                {
                    IsPinned = true;
                    Utils.SetWindowTopmost();
                    Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                    LastPinnedOpacity = windowSettings.Opacity;
                }

                if (Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled)
                {
                    if (Settings.Instance.External.BPTimerSettings.ExternalBPTimerFieldBossHpReportsEnabled)
                    {
                        // Attempt to update our supported mob list with data from the BPTimer server
                        Managers.External.BPTimerManager.FetchSupportedMobList();
                    }
                }

                if (ProcessingDbWork)
                {
                    DatabaseMigrationWindow.Open();
                    Task.Run(() =>
                    {
                        DB.CheckAndRunMigrations();
                    });
                }
                else
                {
                    if (Settings.Instance.CheckForZDPSUpdatesOnStartup)
                    {
                        Web.WebManager.CheckForZDPSUpdates();
                    }

                    if (!Settings.Instance.HasPromptedEnableUpdateChecks && !HasPromptedOneTimeEnableUpdateChecks && !Settings.Instance.CheckForZDPSUpdatesOnStartup)
                    {
                        HasPromptedOneTimeEnableUpdateChecks = true;
                        UpdateCheckPromptWindow.Open();
                    }
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

            if (ResumeFromDbWork)
            {
                ProcessingDbWork = false;
                ResumeFromDbWork = false;
                Utils.BringWindowToFront();
                if (MessageManager.NetCaptureDeviceName != "")
                {
                    MessageManager.InitializeCapturing();
                }

                if (Settings.Instance.CheckForZDPSUpdatesOnStartup)
                {
                    Web.WebManager.CheckForZDPSUpdates();
                }
            }

            if (AppState.IsUpdateAvailable && !HasPromptedUpdateWindow)
            {
                HasPromptedUpdateWindow = true;
                UpdateAvailableWindow.Open();
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

            if (AppState.IsEncounterSavingPaused)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.DarkRed_Transparent);
                ImGui.BeginChild("##EncounterSavingPausedChild", ImGuiChildFlags.AutoResizeY);
                ImGui.TextAligned(0.5f, -1, "Encounter Saving Is Paused");
                ImGui.TextAligned(0.5f, -1, "Automatically resumes if you change maps.");
                ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - 200) * 0.5f);
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen);
                if (ImGui.Button("RESUME SAVING NOW##ResumeEncounterSavingBtn", new Vector2(200, 0)))
                {
                    AppState.IsEncounterSavingPaused = false;
                }
                ImGui.PopStyleColor();
                ImGui.EndChild();
                ImGui.PopStyleColor();
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
                var windowSettings = Settings.Instance.WindowSettings.MainWindow;

                MainMenuBarSize = ImGui.GetWindowSize();

                ImGui.Text("ZDPS - BPSR Damage Meter");

                if (Utils.AppVersion != null)
                {
                    //ImGui.SetCursorPosX(MainMenuBarSize.X - (35 * 5)); // This pushes it against the previous button instead of having a gap
                    //ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X); // This loosely locks it to right side
                    ImGui.TextDisabled($"v{Utils.AppVersion}");
                }

                if (Settings.Instance.AllowEncounterSavingPausingInOpenWorld && BattleStateMachine.DungeonStateHistory.Count > 0 && BattleStateMachine.DungeonStateHistory.LastOrDefault().Key == EDungeonState.DungeonStateNull)
                {
                    ImGui.BeginDisabled(AppState.IsBenchmarkMode);

                    ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 5));
                    ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                    ImGui.PushStyleColor(ImGuiCol.Text, (AppState.IsEncounterSavingPaused ? Colors.Red_Transparent : Colors.White));
                    if (ImGui.MenuItem($"{FASIcons.Pause}##PauseEncounterSavingBtn"))
                    {
                        AppState.IsEncounterSavingPaused = !AppState.IsEncounterSavingPaused;
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                    ImGui.SetItemTooltip("Pause Encounter Saving While In Open World.");

                    ImGui.EndDisabled();
                }

                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 4));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.WindowMinimize}##MinimizeBtn"))
                {
                    Utils.MinimizeWindow();
                }
                ImGui.PopFont();

                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, windowSettings.TopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}##TopMostBtn"))
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

                // Create new Encounter button
                ImGui.BeginDisabled(AppState.IsEncounterSavingPaused);

                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.Rotate}##StartNewEncounterBtn"))
                {
                    CreateNewEncounter();
                }
                ImGui.PopFont();
                ImGui.SetItemTooltip("Start New Encounter");

                ImGui.EndDisabled();

                ImGui.SetCursorPosX(MainMenuBarSize.X - settingsWidth);
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.BeginMenu($"{FASIcons.Gear}##OptionsMenu"))
                {
                    if (SettingsRunOnceDelayedPerOpen == 0)
                    {
                        SettingsRunOnceDelayedPerOpen++;
                    }
                    else if (SettingsRunOnceDelayedPerOpen == 2)
                    {
                        SettingsRunOnceDelayedPerOpen++;

                        if (windowSettings.TopMost)
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
                    ImGui.SetItemTooltip("Manage the ZDatabase.db contents.");

                    if (ImGui.BeginMenu("Raid Manager"))
                    {
                        if (ImGui.MenuItem("Cooldown Priority Tracker"))
                        {
                            RaidManagerCooldownsWindow.Open();
                        }

                        if (ImGui.MenuItem("Raid Warnings"))
                        {
                            RaidManagerRaidWarningWindow.Open();
                        }

                        if (ImGui.MenuItem("Countdowns"))
                        {
                            RaidManagerCountdownWindow.Open();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Benchmark", !AppState.IsEncounterSavingPaused))
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

                        bool benchmarkSingleTarget = AppState.BenchmarkSingleTarget;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Only Track First Target Hit: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##BenchmarkSingleTarget", ref benchmarkSingleTarget);
                        AppState.BenchmarkSingleTarget = benchmarkSingleTarget;

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
                            ImGui.BeginDisabled(AppState.BenchmarkTime < 5);
                            if (ImGui.Button("Start Benchmark", new Vector2(-1, 0)))
                            {
                                AppState.BenchmarkSingleTargetUUID = 0;
                                AppState.IsBenchmarkMode = true;
                                CreateNewEncounter();
                            }
                            ImGui.EndDisabled();
                            if (AppState.BenchmarkTime < 5)
                            {
                                ImGui.SetItemTooltip("Benchmark Time must be at least 5 seconds.");
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

                    if (ImGui.MenuItem("Module Optimizer"))
                    {
                        ModuleSolver.Open();
                    }
                    ImGui.SetItemTooltip("Find the best module combos for your build.");

                    if (ImGui.MenuItem("Chat"))
                    {
                        ChatWindow.Open();
                    }

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
                        if (ImGui.MenuItem("Entity Cache Viewer"))
                        {
                            EntityCacheViewerWindow.Open();
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Exit"))
                    {
                        windowSettings.WindowPosition = WindowPosition;
                        windowSettings.WindowSize = WindowSize;
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
                string subName = "";
                if (!string.IsNullOrEmpty(EncounterManager.Current.SceneSubName))
                {
                    subName = $" ({EncounterManager.Current.SceneSubName})";
                }

                // We don't need to prefix with a space due to actual item spacing handling it for us
                ImGui.TextUnformatted($"- {EncounterManager.Current.SceneName}{subName}");
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
            if (AppState.IsEncounterSavingPaused)
            {
                Log.Information("Tried to create a new manual Encounter but Encounter Saving is currently Paused.");
                return;
            }

            EncounterManager.StopEncounter();
            Log.Information($"Starting new manual encounter at {DateTime.Now}");
            EncounterManager.StartEncounter(true);
        }

        public void ToggleMouseClickthrough()
        {
            AppState.MousePassthrough = !AppState.MousePassthrough;
        }

        public void SetDbWorkComplete()
        {
            ResumeFromDbWork = true;
        }
    }

    public class MainWindowWindowSettings : WindowSettingsBase
    {
        public float MeterBarScale = 1.0f;
    }
}
