using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

namespace BPSR_ZDPS.Windows
{
    public static class EncounterHistoryWindowDB
    {
        public const string LAYER = "EncounterHistoryWindowLayer";

        public static bool IsOpened = false;

        public static int SelectedEncounterIndex = -1;
        public static int SelectedOrderByOption = 0;
        public static int SelectedIndexByEncounter = -1;
        public static int SelectedIndexByBattle = -1;
        public static int SelectedViewMode = 0;

        static List<Encounter> GroupedBattles = new();

        static List<Encounter> Encounters = [];
        static List<Battle> Battles = [];
        static Encounter GroupedEncounter = new Encounter();

        static int RunOnceDelayed = 0;

        public static void Open()
        {
            RunOnceDelayed = 0;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup("###EncounterHistoryWindow");
            IsOpened = true;
            ImGui.PopID();

            LoadFromDB();
        }

        public static void LoadFromDB()
        {
            Encounters = DB.LoadEncounterSummaries().OrderByDescending(x => x.StartTime).ToList();
            Battles = DB.LoadBattles().OrderByDescending(x => x.StartTime).ToList();
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

                ImGui.BeginDisabled(false);
                int viewMode = SelectedViewMode;
                var tabButtonHalfWidth = (ImGui.GetContentRegionAvail().X / 2) - (ImGui.GetStyle().ItemSpacing.X / 2);

                if (viewMode == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Colors.DimGray);
                }
                if (ImGui.Button("View By Each Individual Encounter", new Vector2(tabButtonHalfWidth, 0)))
                {
                    SelectedViewMode = 0;
                    SelectedEncounterIndex = -1;

                    LoadFromDB();
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
                    SelectedEncounterIndex = -1;
                    // TODO: Allow viewing encounters grouped by their BattleId and showing the combined totals for them

                    //GroupEncountersByBattleId();
                    LoadFromDB();
                }
                if (viewMode == 1)
                {
                    ImGui.PopStyleColor();
                }
                ImGui.EndDisabled();

                List<Encounter> encounters = new List<Encounter>();
                // TODO: Support reading history from an encounter cache file as well
                ImGui.AlignTextToFramePadding();
                if (SelectedViewMode == 0)
                {
                    encounters = Encounters;
                    ImGui.Text($"Encounters: {encounters.Count - 1}");
                }
                else
                {
                    encounters = [GroupedEncounter];
                    ImGui.Text($"Battles: {Battles.Count - 1}");
                }

                string[] OrderByOptions = { "Order By Damage", "Order By Healing", "Order By Taken" };
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.CalcTextSize($"{OrderByOptions[SelectedOrderByOption]}").X + 32); // Extra spaces to ensure full text is visible
                ImGui.Combo("##OrderByCombo", ref SelectedOrderByOption, OrderByOptions, OrderByOptions.Length);

                string selectedPreviewText = "";
                if (encounters.Count < SelectedEncounterIndex)
                {
                    SelectedEncounterIndex = -1;
                }
                else if (SelectedEncounterIndex != -1)
                {
                    var encoutner = encounters[SelectedEncounterIndex];
                    selectedPreviewText = BuildDropdownStringName(encoutner.StartTime, encoutner.EndTime, encoutner.SceneName, SelectedEncounterIndex);
                }
                else
                {
                    if (SelectedViewMode == 0)
                    {
                        selectedPreviewText = "Select an encounter...";
                    }
                    else
                    {
                        selectedPreviewText = "Select a battle...";
                    }
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                if (SelectedViewMode == 0)
                {
                    if (ImGui.BeginCombo("##EncounterHistoryCombo", selectedPreviewText, ImGuiComboFlags.None))
                    {
                        for (int i = 0; i < encounters.Count - 1; i++)
                        {
                            bool isSelected = SelectedEncounterIndex == i;
                            string encounterLabel = BuildDropdownStringName(encounters[i].StartTime, encounters[i].EndTime, encounters[i].SceneName, i);
                            if (ImGui.Selectable(encounterLabel, isSelected))
                            {
                                // TODO: Load up the historical encounter
                                SelectedEncounterIndex = i;
                                Encounters[SelectedEncounterIndex] = DB.LoadEncounter(encounters[SelectedEncounterIndex].EncounterId);
                            }

                            if (isSelected)
                            {
                                ImGui.SetItemDefaultFocus();
                            }
                        }

                        ImGui.EndCombo();
                    }

                }
                else
                {
                    if (ImGui.BeginCombo("##EncounterHistoryCombo", selectedPreviewText, ImGuiComboFlags.None))
                    {
                        for (int i = 0; i < Battles.Count - 1; i++)
                        {
                            bool isSelected = SelectedEncounterIndex == i;
                            string encounterLabel = BuildDropdownStringName(Battles[i].StartTime, Battles[i].EndTime, Battles[i].SceneName, i);
                            if (ImGui.Selectable(encounterLabel, isSelected))
                            {
                                // TODO: Load up the historical encounter
                                SelectedEncounterIndex = 0;
                                GroupedEncounter = CalcBattleEncounter(Battles[i].BattleId);
                            }

                            if (isSelected)
                            {
                                ImGui.SetItemDefaultFocus();
                            }
                        }

                        ImGui.EndCombo();
                    }
                }

                // Display Encounter Stats
                if (SelectedEncounterIndex != -1)
                {
                    ImGuiTableFlags tableFlags = ImGuiTableFlags.ScrollX;
                    int columnsCount = 26;
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
                        ImGui.TableSetupColumn("Crit DMG");
                        ImGui.TableSetupColumn("Lucky DMG");
                        ImGui.TableSetupColumn("Crit Lucky DMG");
                        ImGui.TableSetupColumn("Max Instant DPS");
                        ImGui.TableSetupColumn("Shield Gain");
                        ImGui.TableSetupColumn("Total Healing");
                        ImGui.TableSetupColumn("Total HPS");
                        ImGui.TableSetupColumn("Effective Healing");
                        ImGui.TableSetupColumn("Total Overhealing");
                        ImGui.TableSetupColumn("Crit Healing");
                        ImGui.TableSetupColumn("Lucky Healing");
                        ImGui.TableSetupColumn("Crit Lucky Healing");
                        ImGui.TableSetupColumn("Max Instant HPS");
                        ImGui.TableSetupColumn("Damage Taken");
                        ImGui.TableSetupColumn("Taken %");
                        ImGui.TableSetupColumn("Deaths");
                        ImGui.TableHeadersRow();

                        var entitiesFiltered = encounters[SelectedEncounterIndex].Entities.AsValueEnumerable().Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar || x.Value.EntityType == Zproto.EEntityType.EntMonster);
                        List<Entity> entities;
                        switch (SelectedOrderByOption)
                        {
                            case 0:
                                entities = entitiesFiltered.OrderByDescending(x => x.Value.TotalDamage).Select(kvp => kvp.Value).ToList();
                                break;
                            case 1:
                                entities = entitiesFiltered.OrderByDescending(x => x.Value.TotalHealing).Select(kvp => kvp.Value).ToList();
                                break;
                            case 2:
                                entities = entitiesFiltered.OrderByDescending(x => x.Value.TotalTakenDamage).Select(kvp => kvp.Value).ToList();
                                break;
                            default:
                                entities = entitiesFiltered.OrderByDescending(x => x.Value.TotalDamage).Select(kvp => kvp.Value).ToList();
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
                            string totalDamageDealt = Utils.NumberToShorthand(entity.TotalDamage);
                            double totalDamagePct = 0;
                            if (entity.TotalDamage > 0)
                            {
                                if (entity.EntityType == Zproto.EEntityType.EntMonster)
                                {
                                    totalDamagePct = Math.Round(((double)entity.TotalDamage / (double)encounters[SelectedEncounterIndex].TotalNpcDamage) * 100, 0);
                                }
                                else
                                {
                                    totalDamagePct = Math.Round(((double)entity.TotalDamage / (double)encounters[SelectedEncounterIndex].TotalDamage) * 100, 0);
                                }
                            }
                            ImGui.Text($"{totalDamageDealt} ({totalDamagePct}%%)");

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
                            ImGui.Text(Utils.NumberToShorthand(entity.TotalShield));

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
                                    totalTaken = Math.Round(((double)entity.TotalTakenDamage / (double)encounters[SelectedEncounterIndex].TotalTakenDamage) * 100, 0);
                                }
                                else if (entity.EntityType == Zproto.EEntityType.EntMonster)
                                {
                                    totalTaken = Math.Round(((double)entity.TotalTakenDamage / (double)encounters[SelectedEncounterIndex].TotalNpcTakenDamage) * 100, 0);
                                }
                            }
                            ImGui.Text($"{Utils.NumberToShorthand(totalTaken)}%%");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{entity.TotalDeaths}");

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

        private static string BuildDropdownStringName(DateTime startTime, DateTime endTime, string sceneName, int idx)
        {
            var encounterStartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss");
            var encounterEndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss");
            var encounterDuration = (endTime - startTime).ToString("hh\\:mm\\:ss");
            var text = $"[{idx + 1}] {encounterStartTime} - {encounterEndTime} ({encounterDuration}){sceneName}##EncounterHistoryItem_{idx}";

            return text;
        }

        public static Encounter CalcBattleEncounter(ulong battleId)
        {
            var encounters = DB.LoadEncountersForBattleId(battleId);
            Encounter enc = new Encounter();
            foreach (var encounter in encounters)
            {
                enc.TotalDamage += encounter.TotalDamage;
                enc.TotalNpcDamage += encounter.TotalNpcDamage;
                enc.TotalShieldBreak += encounter.TotalShieldBreak;
                enc.TotalNpcShieldBreak += encounter.TotalNpcShieldBreak;
                enc.TotalHealing += encounter.TotalHealing;
                enc.TotalNpcHealing += encounter.TotalNpcHealing;
                enc.TotalOverhealing += encounter.TotalOverhealing;
                enc.TotalNpcOverhealing += encounter.TotalNpcOverhealing;
                enc.TotalTakenDamage += encounter.TotalTakenDamage;
                enc.TotalNpcTakenDamage += encounter.TotalNpcTakenDamage;

                foreach (var entity in encounter.Entities)
                {
                    if (enc.Entities.TryGetValue(entity.Value.UUID, out var foundEnt))
                    {
                        foundEnt.MergeEntity(entity.Value);
                    }
                    else
                    {
                        enc.Entities.TryAdd(entity.Value.UUID, (Entity)entity.Value.Clone());
                    }
                }
            }

            return enc;
        }
    }
}
