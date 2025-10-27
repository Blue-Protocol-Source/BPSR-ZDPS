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
        public static string TITLE_ID = "###SettingsWindow";

        static int PreviousSelectedNetworkDeviceIdx = -1;
        static int SelectedNetworkDeviceIdx = -1;
        static bool normalizeMeterContributions;
        static bool useShortWidthNumberFormatting;
        static bool colorClassIconsByRole;
        static bool showSkillIconsInDetails;

        static SharpPcap.LibPcap.LibPcapLiveDeviceList? NetworkDevices;

        public static void Open()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr("SettingsStack"));
            ImGui.OpenPopup(TITLE_ID);

            NetworkDevices = SharpPcap.LibPcap.LibPcapLiveDeviceList.Instance;

            normalizeMeterContributions = Settings.Instance.NormalizeMeterContributions;
            useShortWidthNumberFormatting = Settings.Instance.UseShortWidthNumberFormatting;
            colorClassIconsByRole = Settings.Instance.ColorClassIconsByRole;
            showSkillIconsInDetails = Settings.Instance.ShowSkillIconsInDetails;

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

            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X, io.DisplaySize.Y), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(550, 600), ImGuiCond.FirstUseEver);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr("SettingsStack"));

            if (ImGui.BeginPopupModal($"Settings{TITLE_ID}"))
            {
                ImGui.SeparatorText("Network Device");
                ImGui.Text("Select the network device to read from:");

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                string network_device_preview = "";
                if (SelectedNetworkDeviceIdx > -1 && NetworkDevices?.Count > 0)
                {
                    network_device_preview = NetworkDevices[SelectedNetworkDeviceIdx].Description;
                }

                if (ImGui.BeginCombo("##NetworkDeviceCombo", network_device_preview))
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
                    }

                    if (NetworkDevices == null || NetworkDevices?.Count == 0)
                    {
                        ImGui.Selectable("<No Network Devices Found>");
                    }

                    ImGui.EndCombo();
                }

                ImGui.SeparatorText("Keybinds");

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
                ImGui.Text("Pinned Window Opacity: ");
                ImGui.SameLine();
                ImGui.DragFloat("##PinnedWindowOpacity", ref Settings.Instance.WindowOpacity, 0.01f, 0.0f, 1.0f);
                ImGui.Indent();
                ImGui.BeginDisabled(true);
                ImGui.TextWrapped("How transparent a pinned window is.");
                ImGui.EndDisabled();
                ImGui.Unindent();

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

                ImGui.NewLine();
                if (ImGui.Button("Save", new Vector2(120, 0)))
                {
                    if (SelectedNetworkDeviceIdx != PreviousSelectedNetworkDeviceIdx)
                    {
                        PreviousSelectedNetworkDeviceIdx = SelectedNetworkDeviceIdx;

                        MessageManager.StopCapturing();

                        Settings.Instance.NetCaptureDeviceName = NetworkDevices[SelectedNetworkDeviceIdx].Name;
                        MessageManager.NetCaptureDeviceName = NetworkDevices[SelectedNetworkDeviceIdx].Name;

                        MessageManager.InitializeCapturing();
                    }

                    Settings.Instance.NormalizeMeterContributions = normalizeMeterContributions;

                    Settings.Instance.UseShortWidthNumberFormatting = useShortWidthNumberFormatting;

                    Settings.Instance.ColorClassIconsByRole = colorClassIconsByRole;

                    Settings.Instance.ShowSkillIconsInDetails = showSkillIconsInDetails;

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X);
                if (ImGui.Button("Close", new Vector2(120, 0)))
                {
                    SelectedNetworkDeviceIdx = PreviousSelectedNetworkDeviceIdx;

                    normalizeMeterContributions = Settings.Instance.NormalizeMeterContributions;

                    useShortWidthNumberFormatting = Settings.Instance.UseShortWidthNumberFormatting;

                    colorClassIconsByRole = Settings.Instance.ColorClassIconsByRole;

                    showSkillIconsInDetails = Settings.Instance.ShowSkillIconsInDetails;

                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }
    }
}
