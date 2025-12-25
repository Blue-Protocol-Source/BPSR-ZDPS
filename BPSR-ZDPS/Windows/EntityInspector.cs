using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using Silk.NET.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

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
        public DateTime? LoadedEncounterStartTime { get; private set; }

        public bool IsOpened = false;

        private bool PersistantTracking = false;
        private int LoadedFromEncounterIdx = -1;

        static int RunOnceDelayed = 0;

        public ETableFilterMode TableFilterMode = ETableFilterMode.SkillsDamage;

        // Graph storage variables
        static bool HasLoadedGraphsData = false;
        static double[] SkillSnapshotTimestampSeconds = [];
        static double[] SkillSnapshotsDamage = [];
        static List<double> SkillSnapshotsDamageCumulative = new();
        static string[] SkillSnapshotsNames = [];
        static float[] SkillSnapshotsHits = [];

        public enum ETableFilterMode : int
        {
            SkillsDamage,
            SkillsHealing,
            SkillsTaken,
            Attributes,
            Buffs,
            Graphs,
            Debug
        }

        public void Open()
        {
            RunOnceDelayed = 0;
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
            ImGui.SetNextWindowSize(new Vector2(880, 600), ImGuiCond.FirstUseEver);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (PersistantTracking && EncounterManager.Current != null)
            {
                if (EncounterManager.Current.Entities.TryGetValue(LoadedEntity.UUID, out var foundEntity))
                {
                    LoadEntity(foundEntity, EncounterManager.Current.StartTime);
                    //LoadedFromEncounterIdx = EncounterManager.Encounters.Count - 1;
                }
            }

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

                if (ImGui.BeginTable("##EntityProperties", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("##PropsLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                    ImGui.TableSetupColumn("##PropsRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                    ImGui.TableNextColumn();

                    var cursorStart = ImGui.GetCursorPos();
                    if (LoadedEntity.EntityType == Zproto.EEntityType.EntChar)
                    {
                        // Render a background image of the base profession for players
                        var tex = ImageHelper.GetTextureByKey($"Profession_{LoadedEntity.ProfessionId}_128");
                        if (tex != null)
                        {
                            float texSize = 96.0f;
                            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - texSize - ImGui.GetStyle().ItemSpacing.X - ImGui.GetStyle().FramePadding.X);
                            if (Settings.Instance.ColorClassIconsByRole)
                            {
                                var roleColor = Professions.RoleTypeColors(Professions.GetRoleFromBaseProfessionId(LoadedEntity.ProfessionId));
                                roleColor.W = roleColor.W * 0.75f;
                                ImGui.ImageWithBg((ImTextureRef)tex, new Vector2(texSize, texSize), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), roleColor);
                            }
                            else
                            {
                                //ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                ImGui.ImageWithBg((ImTextureRef)tex, new Vector2(texSize, texSize), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), new Vector4(1, 1, 1, 0.50f));
                            }
                            
                            ImGui.SetCursorPos(cursorStart);
                        }
                    }

                    ImGui.TextUnformatted($"Name: {LoadedEntity.Name}");
                    ImGui.TextUnformatted($"Level: {LoadedEntity.Level}");
                    ImGui.TextUnformatted($"Ability Score: {LoadedEntity.AbilityScore}");
                    ImGui.TextUnformatted($"Profession: {LoadedEntity.Profession}");
                    ImGui.TextUnformatted($"ProfessionSpec: {LoadedEntity.SubProfession}");
                    ImGui.TextUnformatted($"EntityType: {LoadedEntity.EntityType.ToString()}");

                    ImGui.TableNextColumn();

                    if (ImGui.BeginTable("##EntityStats", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##LeftSide", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##RightSide", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"HP: {LoadedEntity.GetAttrKV("AttrHp") ?? "0"}");
                        ImGui.TextUnformatted($"Max HP: {LoadedEntity.GetAttrKV("AttrMaxHp") ?? "0"}");
                        ImGui.TextUnformatted($"ATK: {LoadedEntity.GetAttrKV("AttrAttack") ?? "0"}");
                        string MainStat = Professions.GetBaseProfessionMainStatName(LoadedEntity.ProfessionId);
                        if (MainStat == "Strength" || MainStat == "")
                        {
                            ImGui.TextUnformatted($"Strength: {LoadedEntity.GetAttrKV("AttrStrength") ?? "0"}");
                        }
                        else if (MainStat == "Agility")
                        {
                            ImGui.TextUnformatted($"Agility: {LoadedEntity.GetAttrKV("AttrAgility") ?? "0"}");
                        }
                        else if (MainStat == "Intellect")
                        {
                            ImGui.TextUnformatted($"Agility: {LoadedEntity.GetAttrKV("AttrAgility") ?? "0"}");
                        }

                        ImGui.TextUnformatted($"Endurance: {LoadedEntity.GetAttrKV("AttrVitality") ?? "0"}");
                        ImGui.TextUnformatted($"Armor: {LoadedEntity.GetAttrKV("AttrDefense") ?? "0"}");

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
                        ImGui.TextUnformatted($"Crit: {CritPctValue}% ({CriValue})");

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
                        ImGui.TextUnformatted($"Haste: {HastePctValue}% ({HasteValue})");


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
                        ImGui.TextUnformatted($"Luck: {LuckPctValue}% ({LuckValue})");

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
                        ImGui.TextUnformatted($"Mastery: {MasteryPctValue}% ({MasteryValue})");

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
                        ImGui.TextUnformatted($"Versatility: {VersatilityPctValue} ({VersatilityValue})");

                        var BlockPct = LoadedEntity.GetAttrKV("AttrBlockPct");
                        double BlockPctValue = 0.0;
                        if (BlockPct != null)
                        {
                            BlockPctValue = Math.Round((int)BlockPct / 100.0, 2);
                        }
                        ImGui.TextUnformatted($"Block: {BlockPctValue}%");

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
                    string valueExtraTotalLabel = "";
                    string valueTotalPerSecondLabel = "";

                    CombatStats combatStats = null;

                    switch (TableFilterMode)
                    {
                        case ETableFilterMode.SkillsHealing:
                            combatStats = LoadedEntity.HealingStats;
                            valueTotalLabel = "Total Healing:";
                            valueExtraTotalLabel = "Total Overheal:";
                            valueTotalPerSecondLabel = "Total HPS:";
                            break;
                        case ETableFilterMode.SkillsTaken:
                            combatStats = LoadedEntity.TakenStats;
                            valueTotalLabel = "Total Taken:";
                            valueExtraTotalLabel = "Total Shield:";
                            valueTotalPerSecondLabel = "Total DPS:";
                            break;
                        default:
                            combatStats = LoadedEntity.DamageStats;
                            valueTotalLabel = "Total Damage:";
                            valueExtraTotalLabel = "Total Shield Break:";
                            valueTotalPerSecondLabel = "Total DPS:";
                            break;
                    }

                    if (ImGui.BeginTable("##EntityValueTotalsTable", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##ValueTotalsLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##ValueTotalsRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{valueTotalLabel} {Utils.NumberToShorthand(combatStats.ValueTotal)}");
                        ImGui.SetItemTooltip($"{combatStats.ValueTotal:N0}");
                        ImGui.TextUnformatted($"{valueTotalPerSecondLabel} {Utils.NumberToShorthand(combatStats.ValuePerSecond)}");
                        ImGui.SetItemTooltip($"{combatStats.ValuePerSecond:N0}");
                        if (TableFilterMode == ETableFilterMode.SkillsDamage)
                        {
                            ImGui.TextUnformatted($"{valueExtraTotalLabel} {Utils.NumberToShorthand(LoadedEntity.TotalShieldBreak)}");
                            ImGui.SetItemTooltip($"{LoadedEntity.TotalShieldBreak:N0}");
                        }
                        else if (TableFilterMode == ETableFilterMode.SkillsHealing)
                        {
                            ImGui.TextUnformatted($"{valueExtraTotalLabel} {Utils.NumberToShorthand(LoadedEntity.TotalOverhealing)}");
                            ImGui.SetItemTooltip($"{LoadedEntity.TotalOverhealing:N0}");
                        }
                        else if (TableFilterMode == ETableFilterMode.SkillsTaken)
                        {
                            ImGui.TextUnformatted($"Total Shield: {Utils.NumberToShorthand(LoadedEntity.TotalShield)}");
                            ImGui.SetItemTooltip($"{LoadedEntity.TotalShield:N0}");
                        }
                        ImGui.TextUnformatted($"Total Hits: {combatStats.HitsCount}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"Total Crit Rate: {combatStats.CritRate}%");
                        ImGui.TextUnformatted($"Total Lucky Rate: {combatStats.LuckyRate}%");
                        ImGui.TextUnformatted($"Total Crits: {combatStats.CritCount}");
                        if (TableFilterMode == ETableFilterMode.SkillsDamage)
                        {
                            ImGui.TextUnformatted($"Total Immunes: {combatStats.ImmuneCount}");
                        }
                        else if (TableFilterMode == ETableFilterMode.SkillsTaken)
                        {
                            ImGui.TextUnformatted($"Total Immunes: {combatStats.ImmuneCount}");
                        }

                        ImGui.EndTable();
                    }

                    ImGui.TableNextColumn();

                    if (ImGui.BeginTable("##EntityDistributionTable", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##DistributionLeft", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##DistributionRight", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();
                        ImGui.Text($"Total Normal Damage: {Utils.NumberToShorthand(combatStats.ValueNormalTotal)}");
                        ImGui.SetItemTooltip($"{combatStats.ValueNormalTotal:N0}");
                        ImGui.Text($"Total Crit Damage: {Utils.NumberToShorthand(combatStats.ValueCritTotal)}");
                        ImGui.SetItemTooltip($"{combatStats.ValueCritTotal:N0}");
                        ImGui.Text($"Total Lucky Damage: {Utils.NumberToShorthand(combatStats.ValueLuckyTotal)}");
                        ImGui.SetItemTooltip($"{combatStats.ValueLuckyTotal:N0}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"Total Lucky Strikes: {combatStats.LuckyCount}");
                        ImGui.Text($"Total Average Damage: {Utils.NumberToShorthand(combatStats.ValueAverage)}");
                        ImGui.SetItemTooltip($"{combatStats.ValueAverage:N0}");
                        ImGui.Text($"Total Casts: {LoadedEntity.TotalCasts}");

                        ImGui.EndTable();
                    }

                    ImGui.EndTable();
                }

                ImGui.Separator();

                string[] FilterButtons = { "Damage", "Healing", "Taken", "Attributes", "Buffs", "Graphs", "Debug" };

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

                ImGui.SameLine();
                ImGui.Checkbox("Persistent Tracking", ref PersistantTracking);
                ImGui.SetItemTooltip("Enable this to track the current entity across new encounters instead of sticking to the one it was opened for.");

                if (TableFilterMode == ETableFilterMode.SkillsDamage || TableFilterMode == ETableFilterMode.SkillsHealing || TableFilterMode == ETableFilterMode.SkillsTaken)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, ImGui.GetStyle().CellPadding.Y));

                    int columnCount = 8;
                    if (TableFilterMode == ETableFilterMode.SkillsDamage)
                    {
                        columnCount = 8;
                    }
                    else if (TableFilterMode == ETableFilterMode.SkillsHealing)
                    {
                        columnCount = 8;
                    }
                    else if (TableFilterMode == ETableFilterMode.SkillsTaken)
                    {
                        columnCount = 9;
                    }

                    if (ImGui.BeginTable("##SkillStatsTable", columnCount, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                    {
                        string valueTotalColumnName = "Damage";
                        string valuePerSecondColumnName = "Total DPS";
                        string valueShareColumnName = "Total DMG %";
                        string valueExtraStatColumnName = "";

                        IReadOnlyList<KeyValuePair<int, CombatStats>> skillStats = null;

                        switch (TableFilterMode)
                        {
                            case ETableFilterMode.SkillsDamage:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillStats.AsValueEnumerable().Where(x => x.Value.SkillType == ESkillType.Damage).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "Total DPS";
                                valueShareColumnName = "Total DMG %";
                                break;
                            case ETableFilterMode.SkillsHealing:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillStats.AsValueEnumerable().Where(x => x.Value.SkillType == ESkillType.Healing).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Healing";
                                valuePerSecondColumnName = "Total HPS";
                                valueShareColumnName = "Total HEAL %";
                                break;
                            case ETableFilterMode.SkillsTaken:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillStats.AsValueEnumerable().Where(x => x.Value.SkillType == ESkillType.Taken).OrderByDescending(x => x.Value.ValueTotal).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "Total DPS";
                                valueShareColumnName = "Total DMG %";
                                valueExtraStatColumnName = "Deaths";
                                break;
                            default:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillStats.AsValueEnumerable().Where(x => x.Value.SkillType == ESkillType.Damage).OrderByDescending(x => x.Value.ValueTotal).ToList());
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

                        if (TableFilterMode == ETableFilterMode.SkillsTaken)
                        {
                            ImGui.TableSetupColumn(valueExtraStatColumnName);
                        }

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
                            if (Settings.Instance.ShowSkillIconsInDetails)
                            {
                                if (HelperMethods.DataTables.Skills.Data.TryGetValue(skillId.ToString(), out var skill))
                                {
                                    var skillIconName = skill.GetIconName();

                                    if (!string.IsNullOrEmpty(skillIconName))
                                    {
                                        var tex = ImageArchive.LoadImage(Path.Combine("Skills", skillIconName));
                                        var itemRectSize = ImGui.GetItemRectSize().Y;
                                        float texSize = itemRectSize;
                                        if (tex != null)
                                        {
                                            ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                            ImGui.SameLine();
                                        }
                                    }
                                }
                            }
                            ImGui.TextUnformatted(displayName);
                            if (stat.Value.Level > 0)
                            {
                                ImGui.SetItemTooltip($"Level: {stat.Value.Level}{(stat.Value.TierLevel > 0 ? $"\nTier: {stat.Value.TierLevel}" : "")}");
                            }

                            ImGui.TableNextColumn();
                            ImGui.BeginGroup();
                            if (Settings.Instance.ShowSkillIconsInDetails)
                            {
                                var damageElementIconPath = Utils.DamagePropertyToIconPath(stat.Value.DamageElement);
                                if (!string.IsNullOrEmpty(damageElementIconPath))
                                {
                                    var tex = ImageArchive.LoadImage(damageElementIconPath);
                                    var itemRectSize = ImGui.GetItemRectSize().Y;
                                    float texSize = itemRectSize;
                                    if (tex != null)
                                    {
                                        ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                        ImGui.SameLine();
                                    }
                                }
                            }
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(stat.Value.ValueTotal)}");
                            ImGui.EndGroup();
                            ulong immuneDamageTotal = stat.Value.ValueImmuneTotal;
                            ImGui.SetItemTooltip($"Type: {stat.Value.DamageMode}\nElement: {Utils.DamagePropertyToString(stat.Value.DamageElement)}{(immuneDamageTotal > 0 ? $"\nImmuned Damage: {Utils.NumberToShorthand(immuneDamageTotal)}" : "")}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(stat.Value.ValuePerSecond)}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{stat.Value.HitsCount}");
                            if (stat.Value.ImmuneCount > 0)
                            {
                                ImGui.SetItemTooltip($"Immune Count: {stat.Value.ImmuneCount}");
                            }

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{stat.Value.CritRate}%");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(stat.Value.ValueAverage)}");

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
                            ImGui.TextUnformatted($"{totalDamageContribution}%");

                            if (TableFilterMode == ETableFilterMode.SkillsTaken)
                            {
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{stat.Value.KillCount}");
                            }
                        }

                        ImGui.EndTable();
                    }

                    ImGui.PopStyleVar();
                }
                else if (TableFilterMode == ETableFilterMode.Attributes)
                {
                    ImGui.TextUnformatted("Attributes:");
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginListBox("##AttrListBox"))
                    {
                        // Create a ReadOnlyList to try and avoid modification errors
                        var attributes = (IReadOnlyList<KeyValuePair<string, object>>)(LoadedEntity.Attributes.ToList());
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            var attr = attributes.ElementAt(i);
                            if (attr.Key != "$type")
                            {
                                ImGui.TextUnformatted($"[{i}] {attr.Key} = {attr.Value.ToString()}");
                            }
                        }

                        ImGui.EndListBox();
                    }
                }
                else if (TableFilterMode == ETableFilterMode.Buffs)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, ImGui.GetStyle().CellPadding.Y));

                    if (ImGui.BeginTable("##BuffEventsTable", 10, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupScrollFreeze(0, 1);
                        ImGui.TableSetupColumn("UUID");
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 100f);
                        ImGui.TableSetupColumn("Skill ID");
                        ImGui.TableSetupColumn("Level");
                        ImGui.TableSetupColumn("Type");
                        ImGui.TableSetupColumn("Layers");
                        ImGui.TableSetupColumn("Duration");
                        ImGui.TableSetupColumn("Caster", ImGuiTableColumnFlags.WidthStretch, 50f);
                        ImGui.TableSetupColumn("Add Time");
                        ImGui.TableSetupColumn("Remove Time");

                        ImGui.TableHeadersRow();

                        ImGuiListClipper clipper = new();
                        int buffCount = LoadedEntity.BuffEvents.Count;
                        clipper.Begin(buffCount);
                        while (clipper.Step())
                        {
                            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                            {
                                var buffEvent = LoadedEntity.BuffEvents[(buffCount - 1) - i];
                                int buffUuid = (int)buffEvent.Uuid;

                                ImGui.TableNextColumn();

                                int buffTypeColor = -1; // 0 = Debuff, 1 = Buff, 2 = Special/Unknown, (Overrides) 99 = Shield
                                string extraTooltip = "";
                                if (buffEvent.AttributeName == "AttrShieldList")
                                {
                                    buffTypeColor = 99;
                                    var shieldInfo = (Zproto.ShieldInfo)buffEvent.Data;
                                    extraTooltip = $"\nShieldInfo: Value={shieldInfo.Value}, InitialValue={shieldInfo.InitialValue}, MaxValue={shieldInfo.MaxValue}";

                                }
                                else if (buffEvent.Duration < 0)
                                {
                                    // Permanent passives (typically from talents) will be excluded
                                    buffTypeColor = -1;
                                }
                                else
                                {
                                    switch (buffEvent.BuffType)
                                    {
                                        case DataTypes.Enum.EBuffType.Debuff:
                                            buffTypeColor = 0;
                                            break;
                                        case DataTypes.Enum.EBuffType.Gain:
                                            buffTypeColor = 1;
                                            break;
                                        case DataTypes.Enum.EBuffType.GainRecovery:
                                            buffTypeColor = 1;
                                            break;
                                        case DataTypes.Enum.EBuffType.Item:
                                            buffTypeColor = 2;
                                            break;
                                        default:
                                            // Unexpected BuffType
                                            break;
                                    }
                                }

                                if (buffTypeColor == 99)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Header, Colors.DimGray);
                                }
                                else if (buffTypeColor == 0)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Header, Colors.DarkRed_Transparent);
                                }
                                else if (buffTypeColor == 1)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Header, Colors.LightGreen_Transparent);
                                }
                                if (buffTypeColor == 2)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Header, Colors.Goldenrod_Transparent);
                                }

                                if (ImGui.Selectable($"{buffUuid}##BuffEventEntry_{i}", true, ImGuiSelectableFlags.SpanAllColumns))
                                {

                                }

                                if (buffTypeColor > -1)
                                {
                                    ImGui.PopStyleColor();
                                }
                                
                                if (!string.IsNullOrEmpty(buffEvent.Description))
                                {
                                    ImGui.SetItemTooltip($"Buff Id: {buffEvent.BaseId}\n{buffEvent.Description.Replace("%", "%%")}{extraTooltip}");
                                }

                                ImGui.TableNextColumn();
                                string displayName = "";
                                if (!string.IsNullOrEmpty(buffEvent.Name))
                                {
                                    displayName = buffEvent.Name;
                                }
                                if (Settings.Instance.ShowSkillIconsInDetails)
                                {
                                    if (!string.IsNullOrWhiteSpace(buffEvent.Icon))
                                    {
                                        var tex = ImageArchive.LoadImage(Path.Combine("Buffs", buffEvent.Icon));
                                        var itemRectSize = ImGui.GetItemRectSize().Y - ImGui.GetStyle().ItemSpacing.Y;
                                        float texSize = itemRectSize;
                                        if (tex != null)
                                        {
                                            ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                            ImGui.SameLine();
                                        }
                                    }
                                }
                                ImGui.TextUnformatted(displayName);

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{buffEvent.SourceConfigId}");

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{buffEvent.Level}");

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{buffEvent.BuffType}");

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{buffEvent.Layer}");

                                ImGui.TableNextColumn();
                                string displayDuration = buffEvent.Duration.ToString();
                                if (buffEvent.Duration > 0)
                                {
                                    displayDuration = (buffEvent.Duration / 1000.0f).ToString();
                                }
                                ImGui.TextUnformatted($"{displayDuration}s");

                                ImGui.TableNextColumn();
                                if (!string.IsNullOrEmpty(buffEvent.EntityCasterName))
                                {
                                    ImGui.TextUnformatted($"{buffEvent.EntityCasterName}");
                                }
                                else
                                {
                                    ImGui.TextUnformatted($"{buffEvent.FireUuid}");
                                }


                                ImGui.TableNextColumn();
                                string addTime = "";
                                if (buffEvent.EventAddTime.TotalMilliseconds > 0)
                                {
                                    addTime = buffEvent.EventAddTime.ToString("hh\\:mm\\:ss");
                                }
                                ImGui.TextUnformatted($"{addTime}");

                                ImGui.TableNextColumn();
                                string removeTime = "";
                                if (buffEvent.EventRemoveTime.TotalMilliseconds > 0)
                                {
                                    removeTime = buffEvent.EventRemoveTime.ToString("hh\\:mm\\:ss");
                                }
                                ImGui.TextUnformatted($"{removeTime}");
                            }
                        }
                        clipper.End();

                        ImGui.EndTable();
                    }

                    ImGui.PopStyleVar();
                }
                else if (TableFilterMode == ETableFilterMode.Graphs)
                {
                    if (LoadedEntity.DamageStats.SkillSnapshots.Count > 0)
                    {
                        // TODO: Optimize all of this, it's just made to be functional right now

                        // TODO: Give option to clamp StartTime to when the entity performed first attack (LoadedEntity.DamageStats.StartTime)
                        var startTime = LoadedEncounterStartTime?.ToUniversalTime() ?? LoadedEntity.DamageStats.StartTime;

                        if (HasLoadedGraphsData && LoadedEntity.DamageStats.SkillSnapshots.Count != SkillSnapshotsDamage.Length)
                        {
                            // We're likely watching an entity live so we need to keep updating their data live
                            // Currently it's going to be a very rough and poor performance mess but at least it's only executed on this one tab
                            HasLoadedGraphsData = false;
                        }
                        if (!HasLoadedGraphsData)
                        {
                            SkillSnapshotsDamageCumulative.Clear();

                            HasLoadedGraphsData = true;

                            List<double> tempSkillSnapshotTimestampSeconds = new() { 0 };
                            List<double> tempSkillSnapshotsDamage = new() { 0 };

                            tempSkillSnapshotTimestampSeconds.AddRange(LoadedEntity.DamageStats.SkillSnapshots.AsValueEnumerable().Select(x => x.Timestamp.Value.Subtract(startTime.Value).TotalSeconds).ToList());
                            tempSkillSnapshotsDamage.AddRange(LoadedEntity.DamageStats.SkillSnapshots.AsValueEnumerable().Select(x => (double)x.Value).ToArray());

                            SkillSnapshotTimestampSeconds = tempSkillSnapshotTimestampSeconds.ToArray();
                            SkillSnapshotsDamage = tempSkillSnapshotsDamage.ToArray();

                            double lastAdded = 0;
                            foreach (var value in SkillSnapshotsDamage)
                            {
                                SkillSnapshotsDamageCumulative.Add(lastAdded + value);
                                lastAdded = lastAdded + value;
                            }

                            SkillSnapshotsNames = LoadedEntity.SkillStats.AsValueEnumerable().Select(x => x.Value.Name ?? "").ToArray();
                            SkillSnapshotsHits = LoadedEntity.SkillStats.AsValueEnumerable().Select(x => (float)x.Value.HitsCount).ToArray();
                        }

                        if (ImPlot.BeginPlot("Total Damage Over Time"))
                        {
                            ImPlot.SetupAxes("Time (Encounter Duration In Seconds)", "Damage", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit);

                            unsafe
                            {
                                ImPlot.SetupAxisFormat(ImAxis.Y1, (value, buff, size, user_data) =>
                                {
                                    string fmt = Utils.NumberToShorthand((double)value);
                                    fixed (byte* src = Encoding.UTF8.GetBytes(fmt))
                                    {
                                        Buffer.MemoryCopy(src, buff, size, fmt.Length + 1);
                                        return fmt.Length + 1;
                                    }
                                });
                            }

                            //ImPlot.FitPointY(SkillSnapshotsDamage.Max());
                            ImPlot.PlotBars("Bars", ref SkillSnapshotTimestampSeconds[0], ref SkillSnapshotsDamageCumulative.ToArray()[0], SkillSnapshotsDamage.Length, 1);
                            ImPlot.PlotLine("Line", ref SkillSnapshotTimestampSeconds[0], ref SkillSnapshotsDamageCumulative.ToArray()[0], SkillSnapshotsDamage.Length);

                            ImPlot.EndPlot();
                        }

                        if (ImPlot.BeginPlot("Damage Per Second Over Time"))
                        {
                            ImPlot.SetupAxes("Time (Encounter Duration In Seconds)", "Damage Per Second", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit);

                            unsafe
                            {
                                ImPlot.SetupAxisFormat(ImAxis.X1, (value, buff, size, user_data) =>
                                {
                                    string fmt = Utils.NumberToShorthand((double)value);
                                    fixed (byte* src = Encoding.UTF8.GetBytes(fmt))
                                    {
                                        Buffer.MemoryCopy(src, buff, size, fmt.Length + 1);
                                        return fmt.Length + 1;
                                    }
                                });
                            }

                            ImPlot.PlotBars("Bars", ref SkillSnapshotTimestampSeconds[0], ref SkillSnapshotsDamage[0], SkillSnapshotsDamage.Length, 1);
                            ImPlot.PlotLine("Line", ref SkillSnapshotTimestampSeconds[0], ref SkillSnapshotsDamage[0], SkillSnapshotsDamage.Length);

                            ImPlot.EndPlot();
                        }

                        if (ImPlot.BeginPlot("Hits By Source", new Vector2(-1, 520), ImPlotFlags.NoMouseText))
                        {
                            ImPlot.SetupAxes("", "", ImPlotAxisFlags.NoDecorations | ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.NoDecorations | ImPlotAxisFlags.AutoFit);
                            ImPlot.SetupLegend(ImPlotLocation.West, ImPlotLegendFlags.Outside);
                            ImPlot.PlotPieChart(SkillSnapshotsNames, ref SkillSnapshotsHits[0], SkillSnapshotsNames.Length, 0, 0, 1, ImPlotPieChartFlags.IgnoreHidden);
                            ImPlot.EndPlot();
                        }
                    }
                }
                else if (TableFilterMode == ETableFilterMode.Debug)
                {
                    ImGui.TextUnformatted($"UUID: {LoadedEntity.UUID}");
                    ImGui.TextUnformatted($"MonsterType: {LoadedEntity.MonsterType}");

                    ImGui.TextUnformatted("Skill Stats:");
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginListBox("##SkillStatsListBox"))
                    {
                        // Create a ReadOnlyList to try and avoid modification errors
                        var skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillStats.OrderByDescending(x => x.Value.ValueTotal).ToList());
                        for (int i = 0; i < skillStats.Count; i++)
                        {
                            var stat = skillStats.ElementAt(i);
                            int skillId = stat.Key;

                            string display = $"[{i}] {skillId}";
                            display = $"{display}\nValueTotal:{stat.Value.ValueTotal}\nSkillType:{stat.Value.SkillType}\nCastsCount:{stat.Value.CastsCount}";

                            ImGui.TextUnformatted($"{display}");
                        }

                        ImGui.EndListBox();
                    }
                }

                ImGui.End();
            }
            ImGui.PopID();
        }

        public void LoadEntity(Entity entity, DateTime encounterStartTime)
        {
            LoadedEntity = entity;
            LoadedEncounterStartTime = encounterStartTime;

            HasLoadedGraphsData = false;
            SkillSnapshotTimestampSeconds = [];
            SkillSnapshotsDamage = [];
            SkillSnapshotsDamageCumulative = new();
            SkillSnapshotsNames = [];
            SkillSnapshotsHits = [];
        }
    }
}
