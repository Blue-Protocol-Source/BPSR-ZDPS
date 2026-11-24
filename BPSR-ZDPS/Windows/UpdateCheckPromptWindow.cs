using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class UpdateCheckPromptWindow
    {
        public const string LAYER = "UpdateCheckPromptWindowLayer";
        public static string TITLE_ID = "###UpdateCheckPromptWindow";
        public static bool IsOpened = false;
        static int RunOnceDelayed = 0;

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

            DpiScaling.BeginWindow(TITLE_ID);

            ImGui.SetNextWindowSize(new Vector2(650, 150) * DpiScaling.Scale, ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(650, 150) * DpiScaling.Scale, new Vector2(ImGui.GETFLTMAX()));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Colors.DarkGreen);

            if (ImGui.BeginPopupModal($"ZDPS Enable Update Checking###{TITLE_ID}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
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

                ImGui.TextWrapped("ZDPS is able to check for updates on startup so you never miss when a new ZDPS version is released. It is recommended to enable this setting.");

                ImGui.NewLine();
                ImGui.TextUnformatted("Would you like to enable this now?");

                ImGui.NewLine();
                ImGui.TextDisabled("This setting can be changed later in the General tab of the Settings menu.");

                ImGui.Separator();
                ImGui.NewLine();

                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
                if (ImGui.Button("Enable Now", new Vector2(250 * DpiScaling.Scale, 0)))
                {
                    Settings.Instance.CheckForZDPSUpdatesOnStartup = true;
                    Settings.Instance.HasPromptedEnableUpdateChecks = true;
                    Settings.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 250 * DpiScaling.Scale);

                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
                if (ImGui.Button("Keep Disabled", new Vector2(250 * DpiScaling.Scale, 0)))
                {
                    Settings.Instance.HasPromptedEnableUpdateChecks = true;
                    Settings.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();

                ImGui.EndPopup();
            }

            ImGui.PopStyleColor();

            ImGui.PopID();
        }
    }
}
