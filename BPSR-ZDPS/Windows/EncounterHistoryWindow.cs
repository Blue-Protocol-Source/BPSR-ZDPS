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
    public static class EncounterHistoryWindow
    {
        public const string LAYER = "EncounterHistoryWindowLayer";

        public static bool IsOpened = false;

        public static int SelectedEncounterIndex = -1;
        public static int SelectedOrderByOption = 0;
        public static int SelectedViewMode = 0;

        static int RunOnceDelayed = 0;

        public static void Open()
        {
            RunOnceDelayed = 0;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup("###EncounterHistoryWindow");
            IsOpened = true;
            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.Begin("Encounter History###EncounterHistoryWindow", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
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

                ImGui.BeginDisabled(true);
                int viewMode = SelectedViewMode;
                var tabButtonHalfWidth = (ImGui.GetContentRegionAvail().X / 2) - (ImGui.GetStyle().ItemSpacing.X / 2);

                if (viewMode == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Colors.DimGray);
                }
                if (ImGui.Button("View By Each Individual Encounter", new Vector2(tabButtonHalfWidth, 0)))
                {
                    SelectedViewMode = 0;
                }
                if (viewMode == 0)
                {
                    ImGui.PopStyleColor();
                }

                ImGui.SameLine();
                if (viewMode == 1)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Colors.DimGray);
                }
                if (ImGui.Button("View By Each Grouped Battle", new Vector2(tabButtonHalfWidth, 0)))
                {
                    SelectedViewMode = 1;
                    // TODO: Allow viewing encounters grouped by their BattleId and showing the combined totals for them

                    var battles = EncounterManager.Encounters.GroupBy(x => x.BattleId);
                }
                if (viewMode == 1)
                {
                    ImGui.PopStyleColor();
                }
                ImGui.EndDisabled();

                // TODO: Support reading history from an encounter cache file as well
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Encounters: {EncounterManager.Encounters.Count - 1}");

                string[] OrderByOptions = { "Order By Damage", "Order By Healing", "Order By Taken" };
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.CalcTextSize($"{OrderByOptions[SelectedOrderByOption]}").X + 32); // Extra spaces to ensure full text is visible
                ImGui.Combo("##OrderByCombo", ref SelectedOrderByOption, OrderByOptions, OrderByOptions.Length);

                string selectedPreviewText = "";
                if (EncounterManager.Encounters.Count < SelectedEncounterIndex)
                {
                    SelectedEncounterIndex = -1;
                }
                else if (SelectedEncounterIndex != -1)
                {
                    string encounterStartTime = EncounterManager.Encounters[SelectedEncounterIndex].StartTime.ToString("yyyy-MM-dd HH-mm-ss");
                    string encounterEndTime = EncounterManager.Encounters[SelectedEncounterIndex].EndTime.ToString("yyyy-MM-dd HH-mm-ss");
                    string encounterDuration = EncounterManager.Encounters[SelectedEncounterIndex].GetDuration().ToString("hh\\:mm\\:ss");
                    string encounterSceneName = $" {EncounterManager.Encounters[SelectedEncounterIndex].SceneName}" ?? "";
                    selectedPreviewText = $"[{SelectedEncounterIndex + 1}] {encounterStartTime} - {encounterEndTime} ({encounterDuration}){encounterSceneName}";
                }
                else
                {
                    selectedPreviewText = "Select an encounter...";
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##EncounterHistoryCombo", selectedPreviewText, ImGuiComboFlags.None))
                {
                    for (int i = 0; i < EncounterManager.Encounters.Count - 1; i++)
                    {
                        bool isSelected = SelectedEncounterIndex == i;
                        string encounterStartTime = EncounterManager.Encounters[i].StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                        string encounterEndTime = EncounterManager.Encounters[i].EndTime.ToString("yyyy-MM-dd HH:mm:ss");
                        string encounterDuration = EncounterManager.Encounters[i].GetDuration().ToString("hh\\:mm\\:ss");
                        string encounterSceneName = $" {EncounterManager.Encounters[i].SceneName}" ?? "";
                        if (ImGui.Selectable($"[{i + 1}] {encounterStartTime} - {encounterEndTime} ({encounterDuration}){encounterSceneName}##EncounterHistoryItem_{i}", isSelected))
                        {
                            // TODO: Load up the historical encounter
                            SelectedEncounterIndex = i;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }

                // Display Encounter Stats
                if (SelectedEncounterIndex != -1)
                {
                    ImGuiTableFlags tableFlags = ImGuiTableFlags.ScrollX;
                    int columnsCount = 24;
                    if (ImGui.BeginTable("##HistoricalEncounterStatsTable", columnsCount, tableFlags, new Vector2(-1, -1)))
                    {
                        ImGui.TableSetupColumn("#");
                        ImGui.TableSetupColumn("UID");
                        ImGui.TableSetupColumn("Name");
                        ImGui.TableSetupColumn("Profession");
                        ImGui.TableSetupColumn("Ability Score");
                        ImGui.TableSetupColumn("Total Damage");
                        ImGui.TableSetupColumn("Total DPS");
                        ImGui.TableSetupColumn("Shield Break");
                        ImGui.TableSetupColumn("Crit Rate");
                        ImGui.TableSetupColumn("Lucky Rate");
                        ImGui.TableSetupColumn("Crit Damage");
                        ImGui.TableSetupColumn("Lucky Damage");
                        ImGui.TableSetupColumn("Crit Lucky Damage");
                        ImGui.TableSetupColumn("Max Instant DPS");
                        ImGui.TableSetupColumn("Total Healing");
                        ImGui.TableSetupColumn("Total HPS");
                        ImGui.TableSetupColumn("Effective Healing");
                        ImGui.TableSetupColumn("Total Overhealing");
                        ImGui.TableSetupColumn("Crit Healing");
                        ImGui.TableSetupColumn("Lucky Healing");
                        ImGui.TableSetupColumn("Crit Lucky Healing");
                        ImGui.TableSetupColumn("Max Instant HPS");
                        ImGui.TableSetupColumn("Damage Taken");
                        ImGui.TableSetupColumn("Damage Share");
                        ImGui.TableHeadersRow();

                        var entitiesFiltered = EncounterManager.Encounters[SelectedEncounterIndex].Entities.Where(x => x.EntityType == Zproto.EEntityType.EntChar || x.EntityType == Zproto.EEntityType.EntMonster);
                        List<Entity> entities;
                        switch (SelectedOrderByOption)
                        {
                            case 0:
                                entities = entitiesFiltered.OrderByDescending(x => x.TotalDamage).ToList();
                                break;
                            case 1:
                                entities = entitiesFiltered.OrderByDescending(x => x.TotalHealing).ToList();
                                break;
                            case 2:
                                entities = entitiesFiltered.OrderByDescending(x => x.TotalTakenDamage).ToList();
                                break;
                            default:
                                entities = entitiesFiltered.OrderByDescending(x => x.TotalDamage).ToList();
                                break;
                        }

                        // Adds vertical padding in each row
                        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, ImGui.GetStyle().CellPadding.Y + 2));

                        for (int entIdx = 0; entIdx < entities.Count; entIdx++)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();

                            var entity = entities[entIdx];

                            string profession = entity.SubProfession ?? entity.Profession ?? "";
                            if (!string.IsNullOrEmpty(profession))
                            {
                                var color = Professions.ProfessionColors(profession);
                                color = color - new Vector4(0, 0, 0, 0.50f); // Make the color extremely muted since we're going to have a lot of them

                                ImGui.PushStyleColor(ImGuiCol.Header, color);
                                //ImGui.PushStyleColor(ImGuiCol.TableRowBg, HelperMethods.ProfessionColors(profession));
                                //ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(color));
                            }

                            if (ImGui.Selectable($"{entIdx + 1}##EntHistSelect_{entIdx}", true, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                mainWindow.entityInspector = new();
                                mainWindow.entityInspector.LoadEntity(entity);
                                mainWindow.entityInspector.Open();
                            }

                            if (!string.IsNullOrEmpty(profession))
                            {
                                ImGui.PopStyleColor();
                            }

                            ImGui.TableNextColumn();
                            ImGui.Text(entity.UID.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(entity.Name ?? $"[{entity.UID}]");

                            ImGui.TableNextColumn();
                            ImGui.Text(profession);

                            ImGui.TableNextColumn();
                            ImGui.Text(entity.AbilityScore.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalDamage));

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.DamageStats.ValuePerSecond));

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalShieldBreak));

                            ImGui.TableNextColumn();
                            ImGui.Text($"{entity.DamageStats.CritRate}%%");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{entity.DamageStats.LuckyRate}%%");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.DamageStats.ValueCritTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.DamageStats.ValueLuckyTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.DamageStats.ValueCritLuckyTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.DamageStats.ValueMax)}");

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalHealing));

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.HealingStats.ValuePerSecond));

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalHealing - entity.TotalOverhealing));

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalOverhealing));

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.HealingStats.ValueCritTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.HealingStats.ValueLuckyTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.HealingStats.ValueCritLuckyTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{Utils.NumberToShorthand(entity.HealingStats.ValueMax)}");

                            ImGui.TableNextColumn();
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalTakenDamage));

                            ImGui.TableNextColumn();
                            double totalTaken = 0;
                            if (entity.TotalTakenDamage > 0)
                            {
                                if (entity.EntityType == Zproto.EEntityType.EntChar)
                                {
                                    totalTaken = Math.Round(((double)entity.TotalTakenDamage / (double)EncounterManager.Encounters[SelectedEncounterIndex].TotalTakenDamage) * 100, 0);
                                }
                                else if (entity.EntityType == Zproto.EEntityType.EntMonster)
                                {
                                    totalTaken = Math.Round(((double)entity.TotalTakenDamage / (double)EncounterManager.Encounters[SelectedEncounterIndex].TotalNpcTakenDamage) * 100, 0);
                                }
                            }
                            ImGui.Text($"{Utils.NumberToShorthand(totalTaken)}%%");

                            if (!string.IsNullOrEmpty(profession))
                            {
                                //ImGui.PopStyleColor();
                            }
                        }

                        ImGui.PopStyleVar();

                        ImGui.EndTable();
                    }
                }

                ImGui.End();
            }

            ImGui.PopID();
        }

        public static void GroupEncountersByBattleId(long battleId)
        {
            var battleGroups = EncounterManager.Encounters.GroupBy(x => x.BattleId);
        }
    }
}
