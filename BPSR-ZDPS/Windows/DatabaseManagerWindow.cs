using BPSR_ZDPS.DataTypes;
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
    public static class DatabaseManagerWindow
    {
        public const string LAYER = "DatabaseManagerWindowLayer";
        public static string TITLE_ID = "###DatabaseManagerWindow";
        public static bool IsOpened = false;

        private const string DELETECONFIRMATIONPROMPT_TITLE_ID = "###DBMDeleteConfirmationPrompt";

        static int RunOnceDelayed = 0;

        public static FileInfo? DbFileInfo;
        public static bool DbFileExists = false;
        static EDeleteDuration DeleteTimeFrame = EDeleteDuration.None;

        enum EDeleteDuration
        {
            None = 0,
            OneDay = 1,
            FiveDays = 2,
            AllTime = 3
        }

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            DbFileInfo = new FileInfo(DB.DbFilePath);
            DbFileExists = DbFileInfo.Exists;
            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(680, 400), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(680, 400), new Vector2(680, 400));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.BeginPopupModal($"Database Manager{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoResize))
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

                ImGui.TextUnformatted("Below are options to allow managing the ZDatabase.db contents.");
                ImGui.TextUnformatted("Please be mindful when deleting past encounter data, all changes are immediate and permanent.");

                bool isDatabaseEnabled = Settings.Instance.UseDatabaseForEncounterHistory;
                bool isHistoryWindowOpen = EncounterHistoryWindow.IsOpened;
                bool isInspectorWindowOpen = mainWindow.entityInspector.IsOpened;
                bool isAllowedToManage = DbFileExists && isDatabaseEnabled && (isHistoryWindowOpen || isInspectorWindowOpen);

                ImGui.SeparatorText("ZDatabase.db Stats");
                if (DbFileExists)
                {
                    ImGui.TextUnformatted($"File Size: {Utils.BytesToString(DbFileInfo.Length)}");
                    ImGui.TextUnformatted($"Encounters Count: {DB.GetNumEncounters()}");
                }
                else
                {
                    ImGui.TextUnformatted($"No Database found at [{DB.DbFilePath}]");
                }

                ImGui.SeparatorText("Delete Encounter History");
                ImGui.TextUnformatted("Select duration to delete encounter history for:");

                ImGui.BeginDisabled(isAllowedToManage);
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Colors.Red_Transparent);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Colors.Red);
                if (ImGui.Button("Older than 1 day", new Vector2(220, 0)))
                {
                    DeleteTimeFrame = EDeleteDuration.OneDay;
                    ImGui.OpenPopup(DELETECONFIRMATIONPROMPT_TITLE_ID);
                }
                ImGui.SameLine();
                if (ImGui.Button("Older than 5 days", new Vector2(220, 0)))
                {
                    DeleteTimeFrame = EDeleteDuration.FiveDays;
                    ImGui.OpenPopup(DELETECONFIRMATIONPROMPT_TITLE_ID);
                }
                ImGui.SameLine();
                if (ImGui.Button("All Time", new Vector2(200, 0)))
                {
                    DeleteTimeFrame = EDeleteDuration.AllTime;
                    ImGui.OpenPopup(DELETECONFIRMATIONPROMPT_TITLE_ID);
                }
                ImGui.PopStyleColor(3);
                ImGui.EndDisabled();

                DeleteConfirmationPrompt();

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        public static void DeleteConfirmationPrompt()
        {
            if (ImGui.BeginPopupModal($"Delete Confirmation{DELETECONFIRMATIONPROMPT_TITLE_ID}", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextUnformatted("Are you sure you want to delete previous encounter data?");

                float buttonWidth = 200;

                if (ImGui.Button("Yes", new Vector2(buttonWidth, 0)))
                {
                    // TODO: Either set a flag to use DeleteTimeFrame value or directly use it here and call the DB delete function
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("No", new Vector2(buttonWidth, 0)))
                {
                    DeleteTimeFrame = EDeleteDuration.None;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SetItemDefaultFocus();

                ImGui.EndPopup();
            }
        }
    }
}
