using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Windows
{
    public class EntityInspector
    {
        public const string LAYER = "EntityInspectorLayer";

        // This entity will only be valid within the context of the current Encounter
        // TODO: Pull from a global entity storage instead of per-encounter or
        // be context aware - if pulling from a historical report, maintain that reference even if current encounter changes
        // if pulling from current, maintain a connection to current encounter to show latest data as encounters change
        public Entity? LoadedEntity { get; set; }

        public bool IsOpened = false;

        public ETableFilterMode TableFilterMode = ETableFilterMode.SkillsDamage;

        public enum ETableFilterMode : int
        {
            SkillsDamage,
            SkillsHealing,
            SkillsTaken,
            Attributes,
            Debug
        }

        public void Open()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            //ImGui.OpenPopup("###EntityInspectorWindow");
            IsOpened = true;
            ImGui.PopID();
        }

        public void Draw(MainWindow mainWindow)
        {
            if (LoadedEntity == null)
            {
                return;
            }

            if (!IsOpened)
            {
                return;
            }

            var main_viewport = ImGui.GetMainViewport();
            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            string entityName = "";
            if (!string.IsNullOrEmpty(LoadedEntity.Name))
            {
                entityName = $"{LoadedEntity.Name} [{LoadedEntity.UID}]";
            }
            else
            {
                entityName = $"[{LoadedEntity.UID}]";
            }

            if (ImGui.Begin($"Entity Inspector - {entityName}###EntityInspectorWindow", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                if (ImGui.BeginTable("##EntityProperties", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("##PropsLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                    ImGui.TableSetupColumn("##PropsRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                    ImGui.TableNextColumn();

                    ImGui.Text($"Name: {LoadedEntity.Name}");
                    ImGui.Text($"Level: {LoadedEntity.Level}");
                    ImGui.Text($"Ability Score: {LoadedEntity.AbilityScore}");
                    ImGui.Text($"Profession: {LoadedEntity.Profession}");
                    ImGui.Text($"ProfessionSpec: {LoadedEntity.SubProfession}");
                    ImGui.Text($"EntityType: {LoadedEntity.EntityType.ToString()}");

                    ImGui.TableNextColumn();

                    if (ImGui.BeginTable("##EntityStats", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##LeftSide", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##RightSide", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();
                        ImGui.Text($"HP: {LoadedEntity.GetAttrKV("AttrHp") ?? "0"}");
                        ImGui.Text($"Max HP: {LoadedEntity.GetAttrKV("AttrMaxHp") ?? "0"}");
                        ImGui.Text($"ATK: {LoadedEntity.GetAttrKV("AttrAttack") ?? "0"}");
                        string MainStat = HelperMethods.GetBaseProfessionMainStatName(LoadedEntity.ProfessionId);
                        if (MainStat == "Strength" || MainStat == "")
                        {
                            ImGui.Text($"Strength: {LoadedEntity.GetAttrKV("AttrStrength") ?? "0"}");
                        }
                        else if (MainStat == "Agility")
                        {
                            ImGui.Text($"Agility: {LoadedEntity.GetAttrKV("AttrAgility") ?? "0"}");
                        }
                        else if (MainStat == "Intellect")
                        {
                            ImGui.Text($"Agility: {LoadedEntity.GetAttrKV("AttrAgility") ?? "0"}");
                        }

                        ImGui.Text($"Endurance: {LoadedEntity.GetAttrKV("AttrVitality") ?? "0"}");
                        ImGui.Text($"Armor: {LoadedEntity.GetAttrKV("AttrDefense") ?? "0"}");

                        ImGui.TableNextColumn();
                        var Cri = LoadedEntity.GetAttrKV("AttrCri"); // Raw Crit stat value
                        int CriValue = 0;
                        if (Cri != null)
                        {
                            CriValue = (int)Cri;
                        }
                        var CritPct = LoadedEntity.GetAttrKV("AttrCrit");
                        double CritPctValue = 0.0;
                        if (CritPct != null)
                        {
                            CritPctValue = Math.Round((int)CritPct / 100.0, 2);
                        }
                        ImGui.Text($"Crit: {CritPctValue}%% ({CriValue})");

                        var Haste = LoadedEntity.GetAttrKV("AttrHaste");
                        int HasteValue = 0;
                        if (Haste != null)
                        {
                            HasteValue = (int)Haste;
                        }
                        var HastePct = LoadedEntity.GetAttrKV("AttrHastePct");
                        double HastePctValue = 0.0;
                        if (HastePct != null)
                        {
                            HastePctValue = Math.Round((int)HastePct / 100.0, 2);
                        }
                        ImGui.Text($"Haste: {HastePctValue}%% ({HasteValue})");


                        var Luck = LoadedEntity.GetAttrKV("AttrLuck");
                        int LuckValue = 0;
                        if (Luck != null)
                        {
                            LuckValue = (int)Luck;
                        }
                        var LuckPct = LoadedEntity.GetAttrKV("AttrLuckyStrikeProb");
                        double LuckPctValue = 0.0;
                        if (LuckPct != null)
                        {
                            LuckPctValue = Math.Round((int)LuckPct / 100.0, 2);
                        }
                        ImGui.Text($"Luck: {LuckPctValue}%% ({LuckValue})");

                        var Mastery = LoadedEntity.GetAttrKV("AttrMastery");
                        int MasteryValue = 0;
                        if (Mastery != null)
                        {
                            MasteryValue = (int)Mastery;
                        }
                        var MasteryPct = LoadedEntity.GetAttrKV("AttrMasteryPct");
                        double MasteryPctValue = 0.0;
                        if (MasteryPct != null)
                        {
                            MasteryPctValue = Math.Round((int)MasteryPct / 100.0, 2);
                        }
                        ImGui.Text($"Mastery: {MasteryPctValue}%% ({MasteryValue})");

                        var Versatility = LoadedEntity.GetAttrKV("AttrVersatility");
                        int VersatilityValue = 0;
                        if (Versatility != null)
                        {
                            VersatilityValue = (int)Versatility;
                        }
                        var VersatilityPct = LoadedEntity.GetAttrKV("AttrVersatilityPct");
                        double VersatilityPctValue = 0.0;
                        if (VersatilityPct != null)
                        {
                            VersatilityPctValue = Math.Round((int)VersatilityPct / 100.0, 2);
                        }
                        ImGui.Text($"Versatility: {VersatilityPctValue} ({VersatilityValue})");

                        var BlockPct = LoadedEntity.GetAttrKV("AttrBlockPct");
                        double BlockPctValue = 0.0;
                        if (BlockPct != null)
                        {
                            BlockPctValue = Math.Round((int)BlockPct / 100.0, 2);
                        }
                        ImGui.Text($"Block: {BlockPctValue}%%");

                        ImGui.EndTable();
                    }

                    ImGui.EndTable();
                }

                ImGui.Separator();

                if (ImGui.BeginTable("##EntityOverallStatsTable", 2, ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.BordersInnerV))
                {
                    ImGui.TableSetupColumn("##OverallStatsLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                    ImGui.TableSetupColumn("##OverallStatsRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                    ImGui.TableNextColumn();

                    string valueTotalLabel = "";
                    string valueTotalPerSecondLabel = "";

                    CombatStats2 combatStats = null;

                    switch (TableFilterMode)
                    {
                        case ETableFilterMode.SkillsHealing:
                            combatStats = LoadedEntity.HealingStats;
                            valueTotalLabel = "Total Healing:";
                            valueTotalPerSecondLabel = "Total HPS:";
                            break;
                        case ETableFilterMode.SkillsTaken:
                            combatStats = LoadedEntity.TakenStats;
                            valueTotalLabel = "Total Taken:";
                            valueTotalPerSecondLabel = "Total DPS:";
                            break;
                        default:
                            combatStats = LoadedEntity.DamageStats;
                            valueTotalLabel = "Total Damage:";
                            valueTotalPerSecondLabel = "Total DPS:";
                            break;
                    }

                    if (ImGui.BeginTable("##EntityValueTotalsTable", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##ValueTotalsLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##ValueTotalsRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();
                        ImGui.Text($"{valueTotalLabel} {HelperMethods.NumberToShorthand(combatStats.ValueTotal)}");
                        ImGui.Text($"{valueTotalPerSecondLabel} {HelperMethods.NumberToShorthand(combatStats.ValuePerSecond)}");
                        ImGui.Text($"Total Hits: {combatStats.HitsCount}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"Total Crit Rate: {combatStats.CritRate}");
                        ImGui.Text($"Total Lucky Rate: {combatStats.LuckyRate}");
                        ImGui.Text($"Total Crits: {combatStats.CritCount}");

                        ImGui.EndTable();
                    }

                    ImGui.TableNextColumn();

                    if (ImGui.BeginTable("##EntityDistributionTable", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##DistributionLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##DistributionRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();
                        ImGui.Text($"Total Normal Damage: {HelperMethods.NumberToShorthand(combatStats.ValueNormalTotal)}");
                        ImGui.Text($"Total Crit Damage: {HelperMethods.NumberToShorthand(combatStats.ValueCritTotal)}");
                        ImGui.Text($"Total Lucky Damage: {HelperMethods.NumberToShorthand(combatStats.ValueLuckyTotal)}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"Total Lucky Strikes: {combatStats.LuckyCount}");
                        ImGui.Text($"Total Average Damage: {HelperMethods.NumberToShorthand(combatStats.ValueAverage)}");
                        ImGui.Text($"Total Casts: {LoadedEntity.TotalCasts}");

                        ImGui.EndTable();
                    }

                    ImGui.EndTable();
                }

                ImGui.Separator();

                string[] FilterButtons = { "Damage", "Healing", "Taken", "Attributes", "Debug" };

                for (int filerBtnIdx = 0; filerBtnIdx < FilterButtons.Length; filerBtnIdx++)
                {
                    bool isSelected = TableFilterMode == (ETableFilterMode)filerBtnIdx;

                    if (isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, Colors.DimGray);
                    }

                    if (ImGui.Button($"{FilterButtons[filerBtnIdx]}##SkillStats_FilterBtn_{filerBtnIdx}"))
                    {
                        TableFilterMode = (ETableFilterMode)filerBtnIdx;
                    }

                    if (isSelected)
                    {
                        ImGui.PopStyleColor();
                    }

                    if (filerBtnIdx < FilterButtons.Length - 1)
                    {
                        ImGui.SameLine();
                    }
                }

                if (TableFilterMode == ETableFilterMode.SkillsDamage || TableFilterMode == ETableFilterMode.SkillsHealing || TableFilterMode == ETableFilterMode.SkillsTaken)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, ImGui.GetStyle().CellPadding.Y));

                    if (ImGui.BeginTable("##SkillStatsTable", 8, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                    {
                        string valueTotalColumnName = "Damage";
                        string valuePerSecondColumnName = "Total DPS";
                        string valueShareColumnName = "Total DMG %";

                        IReadOnlyList<KeyValuePair<int, CombatStats2>> skillStats = null;

                        switch (TableFilterMode)
                        {
                            case ETableFilterMode.SkillsDamage:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats2>>)(LoadedEntity.SkillStats.Where(x => x.Value.SkillType == ESkillType.Damage).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "Total DPS";
                                valueShareColumnName = "Total DMG %";
                                break;
                            case ETableFilterMode.SkillsHealing:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats2>>)(LoadedEntity.SkillStats.Where(x => x.Value.SkillType == ESkillType.Healing).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Healing";
                                valuePerSecondColumnName = "Total HPS";
                                valueShareColumnName = "Total HEAL %";
                                break;
                            case ETableFilterMode.SkillsTaken:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats2>>)(LoadedEntity.SkillStats.Where(x => x.Value.SkillType == ESkillType.Taken).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "Total DPS";
                                valueShareColumnName = "Total DMG %";
                                break;
                            default:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats2>>)(LoadedEntity.SkillStats.Where(x => x.Value.SkillType == ESkillType.Damage).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "Total DPS";
                                valueShareColumnName = "Total DMG %";
                                break;
                        }

                        ImGui.TableSetupColumn("ID");
                        ImGui.TableSetupColumn("Skill Name", ImGuiTableColumnFlags.WidthStretch, 100f);
                        ImGui.TableSetupColumn(valueTotalColumnName);
                        ImGui.TableSetupColumn(valuePerSecondColumnName);
                        ImGui.TableSetupColumn("Hit Count");
                        ImGui.TableSetupColumn("Crit Rate");
                        ImGui.TableSetupColumn("Avg Per Hit");
                        ImGui.TableSetupColumn(valueShareColumnName);
                        ImGui.TableHeadersRow();

                        for (int i = 0; i < skillStats.Count; i++)
                        {
                            var stat = skillStats.ElementAt(i);
                            int skillId = stat.Key;

                            ImGui.TableNextColumn();

                            if (ImGui.Selectable($"{skillId}##SkillStatEntry_{i}", false, ImGuiSelectableFlags.SpanAllColumns))
                            {

                            }

                            ImGui.TableNextColumn();
                            string displayName = "";
                            if (!string.IsNullOrEmpty(stat.Value.Name))
                            {
                                displayName = stat.Value.Name;
                            }
                            ImGui.Text(displayName);

                            ImGui.TableNextColumn();
                            ImGui.Text($"{HelperMethods.NumberToShorthand(stat.Value.ValueTotal)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{HelperMethods.NumberToShorthand(stat.Value.ValuePerSecond)}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{stat.Value.HitsCount}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{stat.Value.CritRate}%%");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{HelperMethods.NumberToShorthand(stat.Value.ValueAverage)}");

                            ImGui.TableNextColumn();
                            double totalDamageContribution = 0.0;
                            if (stat.Value.ValueTotal > 0)
                            {
                                ulong entityTotalValue = 0;
                                if (TableFilterMode == ETableFilterMode.SkillsDamage)
                                {
                                    entityTotalValue = LoadedEntity.TotalDamage;
                                }
                                else if (TableFilterMode == ETableFilterMode.SkillsHealing)
                                {
                                    entityTotalValue = LoadedEntity.TotalHealing;
                                }
                                else if (TableFilterMode == ETableFilterMode.SkillsTaken)
                                {
                                    entityTotalValue = LoadedEntity.TotalTakenDamage;
                                }

                                totalDamageContribution = Math.Round(((double)stat.Value.ValueTotal / (double)entityTotalValue) * 100.0, 0);
                            }
                            ImGui.Text($"{totalDamageContribution}%%");
                        }

                        ImGui.EndTable();
                    }

                    ImGui.PopStyleVar();
                }
                else if (TableFilterMode == ETableFilterMode.Attributes)
                {
                    ImGui.Text("Attributes:");
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginListBox("##AttrListBox"))
                    {
                        // Create a ReadOnlyList to try and avoid modification errors
                        var attributes = (IReadOnlyList<KeyValuePair<string, object>>)(LoadedEntity.Attributes.ToList());
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            var attr = attributes.ElementAt(i);

                            ImGui.Text($"[{i}] {attr.Key} = {attr.Value.ToString()}");
                        }

                        ImGui.EndListBox();
                    }
                }
                else if (TableFilterMode == ETableFilterMode.Debug)
                {
                    ImGui.Text("Skill Stats:");
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginListBox("##SkillStatsListBox"))
                    {
                        // Create a ReadOnlyList to try and avoid modification errors
                        var skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats2>>)(LoadedEntity.SkillStats.OrderByDescending(x => x.Value.ValueTotal).ToList());
                        for (int i = 0; i < skillStats.Count; i++)
                        {
                            var stat = skillStats.ElementAt(i);
                            int skillId = stat.Key;

                            string display = $"[{i}] {skillId}";
                            display = $"{display}\nValueTotal:{stat.Value.ValueTotal}\nSkillType:{stat.Value.SkillType}\nCastsCount:{stat.Value.CastsCount}";

                            ImGui.Text($"{display}");
                        }

                        ImGui.EndListBox();
                    }
                }

                /*ImGui.Text("Actions Timeline:");
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginListBox("##ActionsListBox"))
                {
                    // Create a ReadOnlyList to try and avoid modification errors
                    // If this does not work still, may need to make a backing ImmutableList<ActionStat> that is only added to via a specific Add function
                    // And have reading from it always use a def as: public IReadOnlyList<ActionStat> ActionStats { get { return _actionStats; } }
                    // Would need to make use of a Set function to replace the entire entry for a value being modified
                    // Otherwise, perhaps making use of lock calls is the solution
                    var actionSets = (IReadOnlyList<ActionStat>)LoadedEntity.ActionStats;
                    for (int i = 0;i < actionSets.Count; i++)
                    {
                        var actionStat = actionSets.ElementAt(i);

                        string displayName = "";
                        if (!string.IsNullOrEmpty(actionStat.ActionName))
                        {
                            var strBytes = Encoding.Default.GetBytes(actionStat.ActionName);
                            string encoded = Encoding.UTF8.GetString(strBytes);
                            displayName = encoded;
                        }
                        if (ImGui.Selectable($"[{i}] {actionStat.ActionId} = {displayName}##ActionSelectable_{i}"))
                        {
                            // TODO: Allow inspecting the complete state by probably just storing the entire SyncDamageInfo for the event
                        }
                    }

                    ImGui.EndListBox();
                }*/

                ImGui.End();
            }
            ImGui.PopID();
        }

        public void LoadEntity(Entity entity)
        {
            LoadedEntity = entity;
        }
    }
}
