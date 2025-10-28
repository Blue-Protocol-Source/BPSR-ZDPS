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

        static int SelectedTabIndex = 0;

        List<MeterBase> Meters = new();
        public EntityInspector entityInspector = new();
        public bool IsTopMost = false;

        public void Draw()
        {
            DrawContent();

            SettingsWindow.Draw(this);
            EncounterHistoryWindow.Draw(this);
            entityInspector.Draw(this);
        }

        static bool p_open = true;
        public void DrawContent()
        {
            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(550, 600), ImGuiCond.FirstUseEver);

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

            DrawMenuBar();

            if (RunOnce)
            {
                RunOnce = false;

                // This includes string files and caches
                AppState.LoadDataTables();

                Settings.Instance.Apply();

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

                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 4));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.Minus}"))
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

                // Create new Encounter button
                ImGui.SetCursorPosX(MainMenuBarSize.X - (settingsWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.Rotate}"))
                {
                    EncounterManager.StopEncounter();
                    System.Diagnostics.Debug.WriteLine($"Starting new manual encounter at {DateTime.Now}");
                    EncounterManager.StartEncounter();
                }
                ImGui.PopFont();

                ImGui.SetCursorPosX(MainMenuBarSize.X - settingsWidth);
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.BeginMenu($"{FASIcons.Gear}"))  //("O"))
                {
                    //Utils.SetWindowTopmost();
                    ImGui.PopFont();

                    if (ImGui.MenuItem("Encounter History"))
                    {
                        EncounterHistoryWindow.Open();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Settings"))
                    {
                        SettingsWindow.Open();
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

            ImGui.Text(duration);

            ImGui.SameLine();
            string currentValuePerSecond = $"{Utils.NumberToShorthand(AppState.PlayerTotalMeterValue)} ({Utils.NumberToShorthand(AppState.PlayerMeterValuePerSecond)})";
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + float.Max(0.0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(currentValuePerSecond).X));
            ImGui.Text(currentValuePerSecond);

            ImGui.EndChild();
        }
    }
}
