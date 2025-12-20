using BPSR_DeepsLib;
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
    public static class SettingsWindow
    {
        public const string LAYER = "SettingsWindowLayer";
        public static string TITLE_ID = "###SettingsWindow";

        static int PreviousSelectedNetworkDeviceIdx = -1;
        static int SelectedNetworkDeviceIdx = -1;
        static bool normalizeMeterContributions;
        static bool useShortWidthNumberFormatting;
        static bool colorClassIconsByRole;
        static bool showSkillIconsInDetails;
        static bool onlyShowDamageContributorsInMeters;
        static bool useAutomaticWipeDetection;
        static bool skipTeleportStateCheckInAutomaticWipeDetection;
        static bool splitEncountersOnNewPhases;
        static bool displayTruePerSecondValuesInMeters;
        static float windowOpacity;
        static float meterBarScale;
        static bool useDatabaseForEncounterHistory;
        static int databaseRetentionPolicyDays;
        static bool limitEncounterBuffTrackingWithoutDatabase;

        static bool playNotificationSoundOnMatchmake;
        static string matchmakeNotificationSoundPath;
        static bool loopNotificationSoundOnMatchmake;
        static float matchmakeNotificationVolume;
        static bool playNotificationSoundOnReadyCheck;
        static string readyCheckNotificationSoundPath;
        static bool loopNotificationSoundOnReadyCheck;
        static float readyCheckNotificationVolume;

        static bool logToFile;

        static bool IsBindingEncounterResetKey = false;
        static uint EncounterResetKey;
        static string EncounterResetKeyName = "";
        static bool IsBindingPinnedWindowClickthroughKey = false;
        static uint PinnedWindowClickthroughKey;
        static string PinnedWindowClickthroughKeyName = "";

        static SharpPcap.LibPcap.LibPcapLiveDeviceList? NetworkDevices;
        static EGameCapturePreference GameCapturePreference;

        static bool saveEncounterReportToFile;
        static int reportFileRetentionPolicyDays;
        static int minimumPlayerCountToCreateReport;
        static bool webhookReportsEnabled;
        static EWebhookReportsMode webhookReportsMode;
        static string webhookReportsDeduplicationServerUrl;
        static string webhookReportsDiscordUrl;
        static string webhookReportsCustomUrl;

        // External Settings
        static bool externalBPTimerEnabled;
        static bool externalBPTimerIncludeCharacterId;
        static bool externalBPTimerFieldBossHpReportsEnabled;

        static WindowSettings windowSettings;

        static bool IsDiscordWebhookUrlValid = true;

        static int RunOnceDelayed = 0;

        static bool IsElevated = false;

        public static void Open()
        {
            RunOnceDelayed = 0;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);

            NetworkDevices = SharpPcap.LibPcap.LibPcapLiveDeviceList.Instance;

            Load();

            EncounterResetKey = Settings.Instance.HotkeysEncounterReset;
            if (EncounterResetKey == 0)
            {
                EncounterResetKeyName = "[UNBOUND]";
            }
            else
            {
                EncounterResetKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)EncounterResetKey));
            }

            // Set selection to matching device name (the index could have changed since last time we were here)
            if (!string.IsNullOrEmpty(Settings.Instance.NetCaptureDeviceName))
            {
                for (int i = 0; i < NetworkDevices.Count; i++)
                {
                    if (NetworkDevices[i].Name == Settings.Instance.NetCaptureDeviceName)
                    {
                        SelectedNetworkDeviceIdx = i;
                        if (PreviousSelectedNetworkDeviceIdx == -1)
                        {
                            // This is the first time we're opening the menu, so let's set the default previous value as well
                            // Doing so prevents the capture from being restarted on first save
                            PreviousSelectedNetworkDeviceIdx = i;
                        }
                    }
                }
            }

            // Default to first device in list as fallback, if there are any
            if (SelectedNetworkDeviceIdx == -1 && NetworkDevices?.Count > 0)
            {
                SelectedNetworkDeviceIdx = 0;
            }

            // Disable all HotKeys while we're in the Settings menu to prevent unexpected behavior when rebinding
            HotKeyManager.UnregisterAllHotKeys();

            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            // TODO: Open window at center of current active monitor
            // Will need to use GLFW to figure out monitors/sizes/positions/etc

            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(500, 350), new Vector2(ImGui.GETFLTMAX()));
            //ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X, io.DisplaySize.Y), ImGuiCond.Appearing);

            ImGui.SetNextWindowSize(new Vector2(650, 680), ImGuiCond.FirstUseEver);
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.BeginPopupModal($"Settings{TITLE_ID}"))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                    using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                    {
                        IsElevated = new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                    }
                }
                else if (RunOnceDelayed == 2)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();
                }
                else if (RunOnceDelayed < 3)
                {
                    RunOnceDelayed++;
                }

                ImGuiTabBarFlags tabBarFlags = ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.NoTooltip | ImGuiTabBarFlags.NoCloseWithMiddleMouseButton;
                if (ImGui.BeginTabBar("##SettingsTabs", tabBarFlags))
                {
                    if (ImGui.BeginTabItem("General"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##GeneralTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("Network Device");
                        ImGui.Text("Select the network device to read from:");

                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                        string network_device_preview = "";
                        if (SelectedNetworkDeviceIdx > -1 && NetworkDevices?.Count > 0)
                        {
                            network_device_preview = NetworkDevices[SelectedNetworkDeviceIdx].Description;
                        }

                        if (ImGui.BeginCombo("##NetworkDeviceCombo", network_device_preview, ImGuiComboFlags.HeightLarge))
                        {
                            for (int i = 0; i < NetworkDevices?.Count; i++)
                            {
                                bool isSelected = (SelectedNetworkDeviceIdx == i);
                                var device = NetworkDevices[i];

                                string friendlyName = "";
                                if (!string.IsNullOrEmpty(device.Interface?.FriendlyName))
                                {
                                    friendlyName = $"{device.Interface?.FriendlyName}\n";
                                }

                                if (ImGui.Selectable($"{friendlyName}{device.Description}\n{device.Name}", isSelected))
                                {
                                    SelectedNetworkDeviceIdx = i;
                                }

                                if (isSelected)
                                {
                                    ImGui.SetItemDefaultFocus();
                                }

                                ImGui.Separator();
                            }

                            if (NetworkDevices == null || NetworkDevices?.Count == 0)
                            {
                                ImGui.Selectable("<No Network Devices Found>");
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Game Capture Preference: ");
                        ImGui.SameLine();

                        var gamePrefName = Utils.GameCapturePreferenceToName(GameCapturePreference);
                        ImGui.SetNextItemWidth(150);
                        if (ImGui.BeginCombo("##EGameCapturePreference", gamePrefName))
                        {
                            if (ImGui.Selectable("Auto"))
                            {
                                GameCapturePreference = EGameCapturePreference.Auto;
                            }
                            else if (ImGui.Selectable("Standalone"))
                            {
                                GameCapturePreference = EGameCapturePreference.Standalone;
                            }
                            else if (ImGui.Selectable("Steam"))
                            {
                                GameCapturePreference = EGameCapturePreference.Steam;
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Select which game version you want ZDPS to capture from.\nAuto will automatically detect and use the currently running version. Two simultaneous clients will cause data problems while on Auto.\nSteam and Standalone will only listen for data from their respective versions, allowing both to be run simultaneously and only report DPS for one.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.SeparatorText("Keybinds");

                        if (IsElevated == false)
                        {
                            ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.Red_Transparent);
                            ImGui.BeginChild("##KeybindsNotice", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders);
                            ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
                            ImGui.TextWrapped("Important Note:");
                            ImGui.PopFont();
                            ImGui.TextWrapped("Keybinds only work while the game is in focus if ZDPS is being run as Administrator. This is a limitation imposed by the Game Devs.");
                            ImGui.EndChild();
                            ImGui.PopStyleColor();
                        }

                        ImGui.TextWrapped("Below are global hotkey keybinds for the application. Click on the box and press a key to bind it. Modifier keys (Ctrl/Alt/Shift) are not supported.");
                        ImGui.TextWrapped("Press Escape to cancel the rebinding process.");

                        ImGui.Indent();

                        RebindKeyButton("Encounter Reset", ref EncounterResetKey, ref EncounterResetKeyName, ref IsBindingEncounterResetKey);
                        if (splitEncountersOnNewPhases)
                        {
                            ImGui.Indent();
                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                            ImGui.TextWrapped("[Split Encounters On New Phases] is Enabled. You likely do not need this keybind to manually reset an Encounter. ZDPS will handle Encounter separation for you.");
                            ImGui.PopStyleColor();
                            ImGui.Unindent();
                        }
                        RebindKeyButton("Pinned Window Clickthrough", ref PinnedWindowClickthroughKey, ref PinnedWindowClickthroughKeyName, ref IsBindingPinnedWindowClickthroughKey);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("This allows your mouse input to go 'through' the pinned (Top Most) window, ignoring it, and interacting with whatever may be behind it such as the game or another application.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.Unindent();

                        ImGui.SeparatorText("Database");

                        ShowRestartRequiredNotice(Settings.Instance.UseDatabaseForEncounterHistory != useDatabaseForEncounterHistory, "Use Database For Encounter History");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Use Database For Encounter History: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##UseDatabaseForEncounterHistory", ref useDatabaseForEncounterHistory);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, all encounter data is saved into a local database file (ZDatabase.db) to reduce memory usage and allow viewing between ZDPS sessions. Applies after restarting ZDPS.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!useDatabaseForEncounterHistory);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Database Encounter History Retention Policy: ");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt("##DatabaseRetentionPolicyDays", ref databaseRetentionPolicyDays, 0, 30, databaseRetentionPolicyDays == 0 ? "Keep Forever" : $"{databaseRetentionPolicyDays} Days");
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("How long to keep previous Encounter History data for. When not set to Keep Forever, expired data is automatically deleted on application close.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.BeginDisabled(useDatabaseForEncounterHistory);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Limit Encounter Buff Tracking Without Database: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##LimitEncounterBuffTrackingWithoutDatabase", ref limitEncounterBuffTrackingWithoutDatabase);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, buffs are limited to only the latest 100 per entity instead of being limitless. This only applies if the Database is disabled to allow reduced memory usage. This setting is not retroactive.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Combat"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##CombatTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("Combat");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Normalize Meter Contribution Bars: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##NormalizeMeterContributions", ref normalizeMeterContributions);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, the bars for each player in a meter will be based on the top player, not the overall contribution.");
                        ImGui.TextWrapped("This means the top player is always considered the '100%%' amount.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Use Short Width Number Formatting: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##UseShortWidthNumberFormatting", ref useShortWidthNumberFormatting);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, uses shorter width number formats when values over 1000 would otherwise be shown.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Use Automatic Wipe Detection: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##UseAutomaticWipeDetection", ref useAutomaticWipeDetection);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, ZDPS will attempt to detect party wipes against bosses and start a new encounter automatically.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Skip Teleport State Check In Automatic Wipe Detection: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##SkipTeleportStateCheckInAutomaticWipeDetection", ref skipTeleportStateCheckInAutomaticWipeDetection);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, the 'Teleport' Player State requirement in Automatic Wipe Detection is not performed. You probably want this Disabled.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Split Encounters On New Phases: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##SplitEncountersOnNewPhases", ref splitEncountersOnNewPhases);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, encounters are automatically split across phase changes. This allows bosses to be split from the rest of a dungeon. It also splits raid boss phases. This probably should be enabled.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Display True Per Second Values In Meters: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##DisplayTruePerSecondValuesInMeters", ref displayTruePerSecondValuesInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, the Damage, Healing, and Taken Per Second value shown in the Meters will have the 'true' Per Second value, shown in square brackets, in addition to the normal 'Active Per Second' value. This means it is recalculated every second instead of only using the time the entity was actively participating in combat pressing buttons.\nNote: Both values are accurate, they are just two different metrics.\nThis only works starting from the Next Encounter. It is not retroactive and this value currently only will be shown in the Meters UI.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("User Interface"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##UserInterfaceTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("User Interface");
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Color Class Icons By Role Type: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ColorClassIconsByRole", ref colorClassIconsByRole);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, class icons shown in meters will be colored by their role instead of all being white.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Show Skill Icons In Details: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowSkillIconsInDetails", ref showSkillIconsInDetails);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, skill icons will be displayed, when possible, in the details panel next to skill names.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Only Show Damage Contributors In Meters: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##OnlyShowContributorsInMeters", ref onlyShowDamageContributorsInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, only players who have dealt damage will show in the DPS meter.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.CollapsingHeader("Pinned (Top Most) Window Opacities"))
                        {
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Main Window: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderFloat("##MainWindowOpacity", ref windowOpacity, 0.05f, 1.0f, $"{(int)(windowOpacity * 100)}%%"))
                            {
                                windowOpacity = MathF.Round(windowOpacity, 2);
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("How transparent the Main Window is while pinned.");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Cooldown Priority Tracker Window: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderFloat("##CooldownPriorityTrackerWindowOpacity", ref windowSettings.RaidManagerCooldowns.WindowOpacity, 0.05f, 1.0f, $"{(int)(windowSettings.RaidManagerCooldowns.WindowOpacity * 100)}%%"))
                            {
                                windowSettings.RaidManagerCooldowns.WindowOpacity = MathF.Round(windowSettings.RaidManagerCooldowns.WindowOpacity, 2);
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("How transparent the Cooldown Priority Tracker Window is while pinned.");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Entity Cache Viewer Window: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderFloat("##EntityCacheViewerWindowOpacity", ref windowSettings.EntityCacheViewer.WindowOpacity, 0.05f, 1.0f, $"{(int)(windowSettings.EntityCacheViewer.WindowOpacity * 100)}%%"))
                            {
                                windowSettings.EntityCacheViewer.WindowOpacity = MathF.Round(windowSettings.EntityCacheViewer.WindowOpacity, 2);
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("How transparent the Entity Cache Viewer Window is while pinned.");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.SeparatorText("Integrations");

                            if (ImGui.CollapsingHeader("BPTimer##BPTimerOpacitySection", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Spawn Tracker Window: ");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                                if (ImGui.SliderFloat("##BPTimerSpawnTrackerWindowOpacity", ref windowSettings.SpawnTracker.WindowOpacity, 0.05f, 1.0f, $"{(int)(windowSettings.SpawnTracker.WindowOpacity * 100)}%%"))
                                {
                                    windowSettings.SpawnTracker.WindowOpacity = MathF.Round(windowSettings.SpawnTracker.WindowOpacity, 2);
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("How transparent the Spawn Tracker Window is while pinned.");
                                ImGui.EndDisabled();
                                ImGui.Unindent();

                                ImGui.Unindent();
                            }

                            ImGui.Unindent();
                        }

                        if (ImGui.CollapsingHeader("Window Scales"))
                        {
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Meter Bar Scale: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderFloat("##MeterBarScale", ref meterBarScale, 0.80f, 2.0f, $"{(int)(meterBarScale * 100)}%%"))
                            {
                                meterBarScale = MathF.Round(meterBarScale, 2);
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("Scaling for how large the bars in the meter windows should be. 100%% is the default scale.");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.SeparatorText("Integrations");

                            if (ImGui.CollapsingHeader("BPTimer##BPTimerScaleSection", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Spawn Tracker Text Scale: ");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                                if (ImGui.SliderFloat("##BPTimerSpawnTrackerTextScale", ref windowSettings.SpawnTracker.TextScale, 0.80f, 3.0f, $"{(int)(windowSettings.SpawnTracker.TextScale * 100)}%%"))
                                {
                                    windowSettings.SpawnTracker.TextScale = MathF.Round(windowSettings.SpawnTracker.TextScale, 2);
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("Scaling for how the text in the Spawn Tracker window should be. 100%% is the default scale.");
                                ImGui.EndDisabled();
                                ImGui.Unindent();

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Spawn Tracker Line Scale: ");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                                if (ImGui.SliderFloat("##BPTimerSpawnTrackerLineScale", ref windowSettings.SpawnTracker.LineScale, 0.80f, 3.0f, $"{(int)(windowSettings.SpawnTracker.LineScale * 100)}%%"))
                                {
                                    windowSettings.SpawnTracker.LineScale = MathF.Round(windowSettings.SpawnTracker.LineScale, 2);
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("Scaling for how large the Line (channel) bars in the Spawn Tracker window should be. 100%% is the default scale.");
                                ImGui.EndDisabled();
                                ImGui.Unindent();

                                ImGui.Unindent();
                            }

                            ImGui.Unindent();
                        }

                        ImGui.SeparatorText("Window Property Resets");

                        if (ImGui.Button("Reset Main Window Position"))
                        {
                            var glfwMonitor = Hexa.NET.GLFW.GLFW.GetPrimaryMonitor();
                            var glfwVidMode = Hexa.NET.GLFW.GLFW.GetVideoMode(glfwMonitor);
                            mainWindow.NextWindowPosition = new Vector2(glfwVidMode.Width, glfwVidMode.Height);
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Resets the Main Window back to the original default center screen position on your primary monitor.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Matchmaking"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##MatchmakingTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("Matchmaking");
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Play Notification Sound On Matchmake: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##PlayNotificationSoundOnMatchmake", ref playNotificationSoundOnMatchmake);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, play a notification sound alert when the matchmaker finds players and is waiting for you to accept.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!playNotificationSoundOnMatchmake);
                        ImGui.Indent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Matchmake Notification Sound Path: ");
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140 - ImGui.GetStyle().ItemSpacing.X);
                        ImGui.InputText("##MatchmakeNotificationSoundPath", ref matchmakeNotificationSoundPath, 1024);
                        ImGui.SameLine();
                        if (ImGui.Button("Browse...##MatchmakeSoundPathBrowseBtn", new Vector2(140, 0)))
                        {
                            string defaultDir = File.Exists(matchmakeNotificationSoundPath) ? Path.GetDirectoryName(matchmakeNotificationSoundPath) : "";

                            ImFileBrowser.OpenFile((selectedFilePath)=>
                            {
                                System.Diagnostics.Debug.WriteLine($"MatchmakeNotificationSoundPath = {selectedFilePath}");
                                matchmakeNotificationSoundPath = selectedFilePath;
                            },
                            "Select a sound file...", defaultDir, "MP3 (*.mp3)|*.mp3|WAV (*.wav)|*.wav|All Files (*.*)|*.*", 0);
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("File path to a custom sound file to play when the matchmake notification occurs.\nA default sound will be used if none is set or the file is invalid.\nNote: Only MP3 and WAV are supported formats.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Loop Notification Sound On Matchmake: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##loopNotificationSoundOnMatchmake", ref loopNotificationSoundOnMatchmake);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, the notification sound will loop until you accept the queue pop or it is canceled.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Matchmake Notification Volume Level: ");
                        ImGui.SetNextItemWidth(-1);
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        if (ImGui.SliderFloat("##MatchmakeNotificationVolume", ref matchmakeNotificationVolume, 0.10f, 3.0f, $"{(int)(matchmakeNotificationVolume * 100)}%%"))
                        {
                            matchmakeNotificationVolume = MathF.Round(matchmakeNotificationVolume, 2);
                        }
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Volume scale of the played back notification sound. 100%% is the normal sound level of the audio file. Values above 100%% may not always appear louder. If you need a louder sound, please edit your file in an external program to increase loudness.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Play Notification Sound On Ready Check: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##PlayNotificationSoundOnReadyCheck", ref playNotificationSoundOnReadyCheck);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, play a notification sound alert when a party ready check is performed and is waiting for you to accept.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!playNotificationSoundOnReadyCheck);
                        ImGui.Indent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Ready Check Notification Sound Path: ");
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140 - ImGui.GetStyle().ItemSpacing.X);
                        ImGui.InputText("##ReadyCheckNotificationSoundPath", ref readyCheckNotificationSoundPath, 1024);
                        ImGui.SameLine();
                        if (ImGui.Button("Browse...##ReadyCheckSoundPathBrowseBtn", new Vector2(140, 0)))
                        {
                            string defaultDir = File.Exists(readyCheckNotificationSoundPath) ? Path.GetDirectoryName(readyCheckNotificationSoundPath) : "";

                            ImFileBrowser.OpenFile((selectedFilePath) =>
                            {
                                System.Diagnostics.Debug.WriteLine($"ReadyCheckNotificationSoundPath = {selectedFilePath}");
                                readyCheckNotificationSoundPath = selectedFilePath;
                            },
                            "Select a sound file...", defaultDir, "MP3 (*.mp3)|*.mp3|WAV (*.wav)|*.wav|All Files (*.*)|*.*", 0);
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("File path to a custom sound file to play when the ready check notification occurs.\nA default sound will be used if none is set or the file is invalid.\nNote: Only MP3 and WAV are supported formats.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Loop Notification Sound On Ready Check: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##loopNotificationSoundOnReadyCheck", ref loopNotificationSoundOnReadyCheck);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, the notification sound will loop until you respond to the ready check.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Ready Check Notification Volume Level: ");
                        ImGui.SetNextItemWidth(-1);
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        if (ImGui.SliderFloat("##ReadyCheckNotificationVolume", ref readyCheckNotificationVolume, 0.10f, 3.0f, $"{(int)(readyCheckNotificationVolume * 100)}%%"))
                        {
                            readyCheckNotificationVolume = MathF.Round(readyCheckNotificationVolume, 2);
                        }
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Volume scale of the played back notification sound.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Development"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##DevelopmentTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("Development");
                        if (ImGui.Button("Reload DataTables"))
                        {
                            AppState.LoadDataTables();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Does not update most existing values - mainly works for data set in new Encounters.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("Restart Capture"))
                        {
                            MessageManager.StopCapturing();
                            MessageManager.InitializeCapturing();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Turns the MessageManager off and on to resolve issues of stalled data.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("Reload Module Save"))
                        {
                            ModuleSolver.Init();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Reloads your module inventory from the 'ModulesSaveData.json' file.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ShowRestartRequiredNotice(Settings.Instance.LogToFile != logToFile, "Write Debug Log To File");
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Write Debug Log To File: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##LogToFile", ref logToFile);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, writes a debug log for ZDPS (ZDPS_log.txt). Applies after restarting ZDPS.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Integrations"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##IntegrationsTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("Integrations");

                        ShowGenericImportantNotice(!useAutomaticWipeDetection, "AutoWipeDetectionDisabled", "[Use Automatic Wipe Detection] is currently Disabled. Reports may be incorrect until it is Enabled again.");
                        ShowGenericImportantNotice(skipTeleportStateCheckInAutomaticWipeDetection, "SkipTeleportStateCheckInAutomaticWipeDetectionEnabled", "[Skip Teleport State Check In Automatic Wipe Detection] is currently Enabled. Reports may be incorrect until it is Disabled again.");
                        ShowGenericImportantNotice(!splitEncountersOnNewPhases, "SplitEncountersOnNewPhasesDisabled", "[Split Encounters On New Phases] is currently Disabled. Reports may be incorrect until it is Enabled again.");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Save Encounter Report To File: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##SaveEncounterReportToFile", ref saveEncounterReportToFile);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("When enabled, writes a report file to the Reports folder located next to ZDPS.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!saveEncounterReportToFile);
                        ImGui.Indent();
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Report File Retention Policy: ");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt("##ReportFileRetentionPolicyDays", ref reportFileRetentionPolicyDays, 0, 30, reportFileRetentionPolicyDays == 0 ? "Keep Forever" : $"{reportFileRetentionPolicyDays} Days");
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("How long to keep locally saved Report files for. When not set to Keep Forever, expired data is automatically deleted on application close.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();
                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Minimum Player Count To Create Report: ");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt("##MinimumPlayerCountToCreateReport", ref minimumPlayerCountToCreateReport, 0, 20, minimumPlayerCountToCreateReport == 0 ? "Any" : $"{minimumPlayerCountToCreateReport} Players");
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("The number of players required in an Encounter to create a report for. This applies to both local saving and Webhook sending.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.SeparatorText("ZDPS Report Webhooks");

                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Webhook Mode: ");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(-1);

                        string reportsModeName = "";
                        switch (webhookReportsMode)
                        {
                            case EWebhookReportsMode.DiscordDeduplication:
                                reportsModeName = "Discord Deduplication";
                                break;
                            case EWebhookReportsMode.Discord:
                                reportsModeName = "Discord Webhook";
                                break;
                            case EWebhookReportsMode.Custom:
                                reportsModeName = "Custom URL";
                                break;
                            case EWebhookReportsMode.FallbackDiscordDeduplication:
                                reportsModeName = "Fallback Discord Deduplication";
                                break;
                        }

                        if (ImGui.BeginCombo("##WebhookMode", $"{reportsModeName}", ImGuiComboFlags.None))
                        {
                            if (ImGui.Selectable("Discord Deduplication"))
                            {
                                webhookReportsMode = EWebhookReportsMode.DiscordDeduplication;
                            }
                            ImGui.SetItemTooltip("Send to a Discord Webhook after using an External Server to check if the same report was sent already within a short timeframe.");
                            if (ImGui.Selectable("Discord Webhook"))
                            {
                                webhookReportsMode = EWebhookReportsMode.Discord;
                            }
                            ImGui.SetItemTooltip("Send directly to a Discord Webhook.");
                            if (ImGui.Selectable("Custom URL"))
                            {
                                webhookReportsMode = EWebhookReportsMode.Custom;
                            }
                            ImGui.SetItemTooltip("Send directly to a custom URL of your choice.");
                            if (ImGui.Selectable("Fallback Discord Deduplication"))
                            {
                                webhookReportsMode = EWebhookReportsMode.FallbackDiscordDeduplication;
                            }
                            ImGui.SetItemTooltip("Have an External Server forward to a Discord Webhook after using the External Server to check if the same report was sent already within a short timeframe.");
                            ImGui.EndCombo();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("Select the type of Webhook Mode you want to use for sending ZDPS Reports.\n'Discord Deduplication' is recommended if other users may be sending the same Encounter Report to the same Discord Channel at the same time to avoid duplicate messages.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        // TODO: Maybe allow adding multiple Webhooks and toggling the enabled state of each one (should allow entering a friendly name next to them too)

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text($"Send Encounter Reports To {reportsModeName}: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##WebhookReportsEnabled", ref webhookReportsEnabled);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped($"When enabled, sends an Encounter Report to the given {reportsModeName} server.");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!webhookReportsEnabled);
                        ImGui.Indent();

                        switch (webhookReportsMode)
                        {
                            case EWebhookReportsMode.DiscordDeduplication:
                            case EWebhookReportsMode.Discord:
                                if (webhookReportsMode == EWebhookReportsMode.DiscordDeduplication)
                                {
                                    ImGui.AlignTextToFramePadding();
                                    ImGui.Text("Deduplication Server URL: ");
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputText("##WebhookReportsDeduplicationServerHost", ref webhookReportsDeduplicationServerUrl, 512);
                                    ImGui.Indent();
                                    ImGui.BeginDisabled(true);
                                    ImGui.TextWrapped("The Discord Deduplication Server URL to prevent duplicate reports with.");
                                    ImGui.EndDisabled();
                                    ImGui.Unindent();
                                }

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Webhook URL: ");
                                ImGui.SameLine();
                                ImGui.SetNextItemWidth(-1);
                                if (ImGui.InputText("##WebhookReportsDiscordUrl", ref webhookReportsDiscordUrl, 512))
                                {
                                    if (Utils.SplitAndValidateDiscordWebhook(webhookReportsDiscordUrl) != null)
                                    {
                                        IsDiscordWebhookUrlValid = true;
                                    }
                                    else
                                    {
                                        IsDiscordWebhookUrlValid = false;
                                    }
                                }

                                if (!IsDiscordWebhookUrlValid)
                                {
                                    ImGui.Indent();
                                    ImGui.BeginDisabled(true);
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
                                    ImGui.TextWrapped("The entered URL appears invalid.");
                                    ImGui.PopStyleColor();
                                    ImGui.EndDisabled();
                                    ImGui.Unindent();
                                }

                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("The Discord Webhook URL to send reports to.");
                                ImGui.EndDisabled();
                                ImGui.Unindent();
                                break;
                            case EWebhookReportsMode.Custom:
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Webhook URL: ");
                                ImGui.SameLine();
                                ImGui.SetNextItemWidth(-1);
                                ImGui.InputText("##WebhookReportsCustomUrl", ref webhookReportsCustomUrl, 512);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("The Custom URL to send reports to.");
                                ImGui.EndDisabled();
                                ImGui.Unindent();
                                break;
                        }

                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        if (ImGui.CollapsingHeader("BPTimer", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("BPTimer Enabled: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##ExternalBPTimerEnabled", ref externalBPTimerEnabled);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("When enabled, allows sending reports back to BPTimer.com.");
                            bool hasBPTimerReports = externalBPTimerFieldBossHpReportsEnabled;
                            if (!hasBPTimerReports)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
                            }
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green);
                            }
                            ImGui.TextWrapped("Note: This setting alone does not enable reports. They must be enabled individually below.");
                            ImGui.PopStyleColor();

                            ImGui.EndDisabled();
                            if (ImGui.CollapsingHeader("Data Collection##BPTimerDataCollectionSection"))
                            {
                                ImGui.Indent();
                                ImGui.TextUnformatted("BPTimer collects the following data:");
                                ImGui.BulletText("Boss ID/HP/Position");
                                ImGui.BulletText("Character Line Number");
                                ImGui.BulletText("Account ID");
                                ImGui.SetItemTooltip("This is being used to determine what game region is being played on.");
                                ImGui.BulletText("Character UID (if you opt-in below)");
                                ImGui.BulletText("Your IP Address");
                                ImGui.Unindent();
                            }
                            ImGui.Unindent();

                            ImGui.BeginDisabled(!externalBPTimerEnabled);
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Include Own Character Data In Report: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##ExternalBPTimerIncludeCharacterId", ref externalBPTimerIncludeCharacterId);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("When enabled, your Character UID will be included in the reported data.");
                            ImGui.EndDisabled();
                            ImGui.Unindent();
                            
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("BPTimer Field Boss HP Reports: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##ExternalBPTimerFieldBossHpReportsEnabled", ref externalBPTimerFieldBossHpReportsEnabled);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("When enabled, reports Field Boss (and Magical Creature) HP data back to BPTimer.com.");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.Unindent();
                            ImGui.EndDisabled();
                        }

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }

                ImGui.NewLine();
                float buttonWidth = 120;
                if (ImGui.Button("Save", new Vector2(buttonWidth, 0)))
                {
                    Save(mainWindow);

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonWidth);
                if (ImGui.Button("Close", new Vector2(buttonWidth, 0)))
                {
                    SelectedNetworkDeviceIdx = PreviousSelectedNetworkDeviceIdx;

                    Load();

                    EncounterResetKey = Settings.Instance.HotkeysEncounterReset;
                    if (EncounterResetKey == 0)
                    {
                        EncounterResetKeyName = "[UNBOUND]";
                    }
                    else
                    {
                        EncounterResetKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)EncounterResetKey));
                    }

                    PinnedWindowClickthroughKey = Settings.Instance.HotkeysPinnedWindowClickthrough;
                    if (PinnedWindowClickthroughKey == 0)
                    {
                        PinnedWindowClickthroughKeyName = "[UNBOUND]";
                    }
                    else
                    {
                        PinnedWindowClickthroughKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)PinnedWindowClickthroughKey));
                    }

                    RegisterAllHotkeys(mainWindow);

                    ImGui.CloseCurrentPopup();
                }

                ImFileBrowser.Draw();

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        private static void Load()
        {
            normalizeMeterContributions = Settings.Instance.NormalizeMeterContributions;
            useShortWidthNumberFormatting = Settings.Instance.UseShortWidthNumberFormatting;
            colorClassIconsByRole = Settings.Instance.ColorClassIconsByRole;
            showSkillIconsInDetails = Settings.Instance.ShowSkillIconsInDetails;
            onlyShowDamageContributorsInMeters = Settings.Instance.OnlyShowDamageContributorsInMeters;
            useAutomaticWipeDetection = Settings.Instance.UseAutomaticWipeDetection;
            skipTeleportStateCheckInAutomaticWipeDetection = Settings.Instance.SkipTeleportStateCheckInAutomaticWipeDetection;
            splitEncountersOnNewPhases = Settings.Instance.SplitEncountersOnNewPhases;
            displayTruePerSecondValuesInMeters = Settings.Instance.DisplayTruePerSecondValuesInMeters;
            windowOpacity = Settings.Instance.WindowOpacity;
            meterBarScale = Settings.Instance.MeterBarScale;

            useDatabaseForEncounterHistory = Settings.Instance.UseDatabaseForEncounterHistory;
            databaseRetentionPolicyDays = Settings.Instance.DatabaseRetentionPolicyDays;
            limitEncounterBuffTrackingWithoutDatabase = Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase;
            GameCapturePreference = Settings.Instance.GameCapturePreference;

            playNotificationSoundOnMatchmake = Settings.Instance.PlayNotificationSoundOnMatchmake;
            matchmakeNotificationSoundPath = Settings.Instance.MatchmakeNotificationSoundPath;
            loopNotificationSoundOnMatchmake = Settings.Instance.LoopNotificationSoundOnMatchmake;
            matchmakeNotificationVolume = Settings.Instance.MatchmakeNotificationVolume;

            playNotificationSoundOnReadyCheck = Settings.Instance.PlayNotificationSoundOnReadyCheck;
            readyCheckNotificationSoundPath = Settings.Instance.ReadyCheckNotificationSoundPath;
            loopNotificationSoundOnReadyCheck = Settings.Instance.LoopNotificationSoundOnReadyCheck;
            readyCheckNotificationVolume = Settings.Instance.ReadyCheckNotificationVolume;

            saveEncounterReportToFile = Settings.Instance.SaveEncounterReportToFile;
            reportFileRetentionPolicyDays = Settings.Instance.ReportFileRetentionPolicyDays;
            minimumPlayerCountToCreateReport = Settings.Instance.MinimumPlayerCountToCreateReport;
            webhookReportsEnabled = Settings.Instance.WebhookReportsEnabled;
            webhookReportsMode = Settings.Instance.WebhookReportsMode;
            webhookReportsDeduplicationServerUrl = Settings.Instance.WebhookReportsDeduplicationServerHost;
            webhookReportsDiscordUrl = Settings.Instance.WebhookReportsDiscordUrl;
            webhookReportsCustomUrl = Settings.Instance.WebhookReportsCustomUrl;

            windowSettings = (WindowSettings)Settings.Instance.WindowSettings.Clone();

            logToFile = Settings.Instance.LogToFile;

            // External
            externalBPTimerEnabled = Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled;
            externalBPTimerIncludeCharacterId = Settings.Instance.External.BPTimerSettings.ExternalBPTimerIncludeCharacterId;
            externalBPTimerFieldBossHpReportsEnabled = Settings.Instance.External.BPTimerSettings.ExternalBPTimerFieldBossHpReportsEnabled;
        }

        private static void Save(MainWindow mainWindow)
        {
            if (SelectedNetworkDeviceIdx != PreviousSelectedNetworkDeviceIdx || GameCapturePreference != Settings.Instance.GameCapturePreference)
            {
                PreviousSelectedNetworkDeviceIdx = SelectedNetworkDeviceIdx;

                MessageManager.StopCapturing();

                Settings.Instance.NetCaptureDeviceName = NetworkDevices[SelectedNetworkDeviceIdx].Name;
                MessageManager.NetCaptureDeviceName = NetworkDevices[SelectedNetworkDeviceIdx].Name;

                Settings.Instance.GameCapturePreference = GameCapturePreference;

                MessageManager.InitializeCapturing();
            }

            Settings.Instance.NormalizeMeterContributions = normalizeMeterContributions;
            Settings.Instance.UseShortWidthNumberFormatting = useShortWidthNumberFormatting;
            Settings.Instance.ColorClassIconsByRole = colorClassIconsByRole;
            Settings.Instance.ShowSkillIconsInDetails = showSkillIconsInDetails;
            Settings.Instance.OnlyShowDamageContributorsInMeters = onlyShowDamageContributorsInMeters;
            Settings.Instance.UseAutomaticWipeDetection = useAutomaticWipeDetection;
            Settings.Instance.SkipTeleportStateCheckInAutomaticWipeDetection = skipTeleportStateCheckInAutomaticWipeDetection;
            Settings.Instance.SplitEncountersOnNewPhases = splitEncountersOnNewPhases;
            Settings.Instance.DisplayTruePerSecondValuesInMeters = displayTruePerSecondValuesInMeters;
            Settings.Instance.WindowOpacity = windowOpacity;
            Settings.Instance.MeterBarScale = meterBarScale;

            Settings.Instance.UseDatabaseForEncounterHistory = useDatabaseForEncounterHistory;
            Settings.Instance.DatabaseRetentionPolicyDays = databaseRetentionPolicyDays;
            Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase = limitEncounterBuffTrackingWithoutDatabase;

            Settings.Instance.PlayNotificationSoundOnMatchmake = playNotificationSoundOnMatchmake;
            Settings.Instance.MatchmakeNotificationSoundPath = matchmakeNotificationSoundPath;
            Settings.Instance.LoopNotificationSoundOnMatchmake = loopNotificationSoundOnMatchmake;
            Settings.Instance.MatchmakeNotificationVolume = matchmakeNotificationVolume;

            Settings.Instance.PlayNotificationSoundOnReadyCheck = playNotificationSoundOnReadyCheck;
            Settings.Instance.ReadyCheckNotificationSoundPath = readyCheckNotificationSoundPath;
            Settings.Instance.LoopNotificationSoundOnReadyCheck = loopNotificationSoundOnReadyCheck;
            Settings.Instance.ReadyCheckNotificationVolume = readyCheckNotificationVolume;

            Settings.Instance.SaveEncounterReportToFile = saveEncounterReportToFile;
            Settings.Instance.ReportFileRetentionPolicyDays = reportFileRetentionPolicyDays;
            Settings.Instance.MinimumPlayerCountToCreateReport = minimumPlayerCountToCreateReport;
            Settings.Instance.WebhookReportsEnabled = webhookReportsEnabled;
            Settings.Instance.WebhookReportsMode = webhookReportsMode;
            Settings.Instance.WebhookReportsDeduplicationServerHost = webhookReportsDeduplicationServerUrl;
            Settings.Instance.WebhookReportsDiscordUrl = webhookReportsDiscordUrl;
            Settings.Instance.WebhookReportsCustomUrl = webhookReportsCustomUrl;

            Settings.Instance.WindowSettings = (WindowSettings)windowSettings.Clone();

            Settings.Instance.LogToFile = logToFile;

            // External
            Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled = externalBPTimerEnabled;
            Settings.Instance.External.BPTimerSettings.ExternalBPTimerIncludeCharacterId = externalBPTimerIncludeCharacterId;
            Settings.Instance.External.BPTimerSettings.ExternalBPTimerFieldBossHpReportsEnabled = externalBPTimerFieldBossHpReportsEnabled;

            RegisterAllHotkeys(mainWindow);

            DB.Init();

            // Write out the new settings to file now that they've been applied
            Settings.Save();
        }

        static void ShowRestartRequiredNotice(bool showCondition, string settingName)
        {
            if (showCondition)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.Red_Transparent);
                ImGui.BeginChild($"##RestartRequiredNotice_{settingName}", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders);
                ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
                ImGui.TextUnformatted("Important Note:");
                ImGui.PopFont();
                ImGui.TextWrapped($"Changing the [{settingName}] setting requires restarting ZDPS to take effect.");
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }

        static void ShowGenericImportantNotice(bool showCondition, string uniqueName, string text)
        {
            if (showCondition)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.Red_Transparent);
                ImGui.BeginChild($"##GenericImportantNotice_{uniqueName}", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders);
                ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
                ImGui.TextUnformatted("Important Note:");
                ImGui.PopFont();
                ImGui.TextWrapped($"{text}");
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }

        static void RegisterAllHotkeys(MainWindow mainWindow)
        {
            if (EncounterResetKey != 0)// && EncounterResetKey != Settings.Instance.HotkeysEncounterReset)
            {
                HotKeyManager.RegisterKey("EncounterReset", mainWindow.CreateNewEncounter, EncounterResetKey);
            }
            Settings.Instance.HotkeysEncounterReset = EncounterResetKey;

            if (PinnedWindowClickthroughKey != 0)
            {
                HotKeyManager.RegisterKey("PinnedWindowClickthrough", mainWindow.ToggleMouseClickthrough, PinnedWindowClickthroughKey);
            }
            Settings.Instance.HotkeysPinnedWindowClickthrough = PinnedWindowClickthroughKey;
        }

        public static void RebindKeyButton(string bindingName, ref uint bindingVariable, ref string bindingVariableName, ref bool bindingState)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{bindingName}:");

            string bindDisplay = "[UNBOUND]";

            if (bindingState == true)
            {
                for (uint key = (uint)ImGuiKey.NamedKeyBegin; key < (uint)ImGuiKey.NamedKeyEnd; key++)
                {
                    if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                    {
                        bindingState = false;
                    }
                    else if (ImGui.IsKeyPressed((ImGuiKey)key))
                    {
                        ImGuiKey[] blacklistedKeys =
                            [
                            ImGuiKey.ModAlt, ImGuiKey.LeftAlt, ImGuiKey.RightAlt, ImGuiKey.ReservedForModAlt,
                            ImGuiKey.ModCtrl, ImGuiKey.LeftCtrl, ImGuiKey.RightCtrl, ImGuiKey.ReservedForModCtrl,
                            ImGuiKey.ModShift, ImGuiKey.LeftShift, ImGuiKey.RightShift, ImGuiKey.ReservedForModShift,
                            ImGuiKey.ModMask, ImGuiKey.ModSuper, ImGuiKey.LeftSuper, ImGuiKey.RightSuper, ImGuiKey.ReservedForModSuper,
                            ImGuiKey.MouseLeft, ImGuiKey.MouseMiddle, ImGuiKey.MouseRight, ImGuiKey.MouseWheelX, ImGuiKey.MouseWheelY,
                            ImGuiKey.Escape, ImGuiKey.F12
                            ];
                        
                        if (!blacklistedKeys.Contains((ImGuiKey)key))
                        {
                            string keyName = ImGui.GetKeyNameS((ImGuiKey)key);
                            bindingVariable = (uint)HotKeyManager.ImGuiKeyToVirtualKey((ImGuiKey)key);
                            bindingVariableName = keyName;
                            bindingState = false;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(bindingVariableName))
            {
                bindDisplay = bindingVariableName;
            }
            ImGui.SameLine();
            bool isInBindingState = bindingState;

            if (isInBindingState)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered]);
            }
            if (ImGui.Button($"{bindDisplay}##BindBtn_{bindingName}", new Vector2(120, 0)))
            {
                bindingState = true;
            }
            if (isInBindingState)
            {
                ImGui.PopStyleColor();
            }
        }
    }
}
