using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Windows
{
    public static class DatabaseMigrationWindow
    {
        public const string LAYER = "DatabaseMigrationWindowLayer";
        public static string TITLE_ID = "###DatabaseMigrationWindow";
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

            DpiScaling.BeginWindow(TITLE_ID);

            ImGui.SetNextWindowSizeConstraints(new Vector2(480, 150) * DpiScaling.Scale, new Vector2(680, 600) * DpiScaling.Scale);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.BeginPopupModal($"Database Migration In Progress{TITLE_ID}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
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
                else if (RunOnceDelayed < 5)
                {
                    RunOnceDelayed++;
                }

                ImGui.TextUnformatted("The ZDatabase is being migrated to a new version.");
                ImGui.TextUnformatted("Please do not exit ZDPS while the migration is running.");

                int currentMigration = DB.MigrationStatus.CurrentMigrationNum;
                int totalMigrations = DB.MigrationStatus.TotalMigrationsNeeded;
                float progress = MathF.Round((float)currentMigration / ((float)totalMigrations + 1.0f), 4);

                if (DB.MigrationStatus.State == Database.Migrations.MigrationStatusState.Done)
                {
                    progress = 1.0f;
                }

                string overallProgressStr = "";
                if (DB.MigrationStatus.State == Database.Migrations.MigrationStatusState.CleanUp)
                {
                    overallProgressStr = " (Finalizing)";
                }

                if (DB.MigrationStatus.State == Database.Migrations.MigrationStatusState.Done)
                {
                    overallProgressStr = " (Done)";
                }

                ImGui.TextUnformatted($"Performing Migration {currentMigration} / {totalMigrations}{overallProgressStr}...");
                ImGui.ProgressBar(progress, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFontSize()), "");

                if (DB.MigrationStatus.CurrentMigration?.Progress >= 0)
                {
                    ImGui.TextUnformatted($"Current migration progress ({MathF.Round(DB.MigrationStatus.CurrentMigration.Progress * 100, 2).ToString("00.00")}%)");
                    ImGui.ProgressBar(DB.MigrationStatus.CurrentMigration.Progress, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFontSize()), "");
                }

                if (DB.MigrationStatus.State == Database.Migrations.MigrationStatusState.Error)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
                    ImGui.TextWrapped(DB.MigrationStatus.ErrorMsg);
                    ImGui.PopStyleColor();

                    if (ImGui.Button("Exit ZDPS", new Vector2(-1, 0)))
                    {
                        Hexa.NET.GLFW.GLFW.SetWindowShouldClose(HelperMethods.GLFWwindow, 1);
                    }
                }
                else
                {
                    ImGui.BeginDisabled(DB.MigrationStatus.State != Database.Migrations.MigrationStatusState.Done);
                    if (ImGui.Button("Close", new Vector2(-1, 0)))
                    {
                        IsOpened = false;
                        mainWindow.SetDbWorkComplete();
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndDisabled();
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }
    }
}
