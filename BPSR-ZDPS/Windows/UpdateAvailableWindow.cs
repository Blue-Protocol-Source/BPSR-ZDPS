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
    public static class UpdateAvailableWindow
    {
        public const string LAYER = "UpdateAvailableWindowLayer";
        public static string TITLE_ID = "###UpdateAvailableWindow";
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

            ImGui.SetNextWindowSize(new Vector2(650, 150), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(650, 150), new Vector2(ImGui.GETFLTMAX()));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Colors.DarkGreen);

            if (ImGui.BeginPopupModal($"ZDPS Update Available###{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
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
                    Utils.SetWindowTopmost();
                }
                else if (RunOnceDelayed < 3)
                {
                    RunOnceDelayed++;
                }

                if (ImGui.BeginChild("##AlertChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - ImGui.GetItemRectSize().Y * 2)))
                {
                    ImGui.TextUnformatted("There is a new update available for ZDPS!");
                    ImGui.TextUnformatted("It is strongly recommended to always update to the latest version available.");

                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.OrangeRed);
                    ImGui.TextUnformatted("Reminder: This is the Season 1 version of ZDPS when downloading updates!");
                    ImGui.PopStyleColor();

                    ImGui.EndChild();
                }

                ImGui.Separator();
                ImGui.NewLine();

                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
                if (ImGui.Button("Go To Update Website", new Vector2(250, 0)))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = Settings.Instance.ZDPSWebsiteURL,
                            UseShellExecute = true,
                        });
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Error opening Update Website.");
                    }
                    
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();
                ImGui.SetItemTooltip($"Click to open [ {Settings.Instance.ZDPSWebsiteURL} ]");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 250);

                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
                if (ImGui.Button("Remind Me Later", new Vector2(250, 0)))
                {
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
