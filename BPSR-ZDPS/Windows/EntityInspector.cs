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

        static string AttributeFilter = "";

        static List<long> ShowAllInstancesSkillIds = new();

        // Graph storage variables
        static bool HasLoadedGraphsData = false;
        static double[] SkillSnapshotTimestampSeconds = [];
        static double[] SkillSnapshotsDamage = [];
        static List<double> SkillSnapshotsDamageCumulative = new();
        static string[] SkillSnapshotsNames = [];
        static float[] SkillSnapshotsHits = [];
        static Dictionary<string, ScatterPlotSkillMap> SkillScatterMap = new();

        public enum ETableFilterMode : int
        {
            SkillsDamage,
            SkillsHealing,
            SkillsTaken,
            EntityTaken,
            Attributes,
            Buffs,
            Graphs,
            SkillBook,
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
            ImGui.SetNextWindowSize(new Vector2(900, 600), ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 150), new Vector2(ImGui.GETFLTMAX()));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (PersistantTracking && EncounterManager.Current != null)
            {
                if (EncounterManager.Current.Entities.TryGetValue(LoadedEntity.UUID, out var foundEntity))
                {
                    if (LoadedEncounterStartTime != EncounterManager.Current.StartTime)
                    {
                        LoadEntity(foundEntity, EncounterManager.Current.StartTime);
                        //LoadedFromEncounterIdx = EncounterManager.Encounters.Count - 1;
                    }
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
                    if (LoadedEntity.SeasonLevel > 0)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted($"(+{LoadedEntity.SeasonLevel})");
                        ImGui.SetItemTooltip("Season Level");
                    }
                    ImGui.TextUnformatted($"Ability Score: {LoadedEntity.AbilityScore}");
                    if (LoadedEntity.SeasonStrength > 0)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted($"(+{LoadedEntity.SeasonStrength})");
                        ImGui.SetItemTooltip("Season Strength");
                    }
                    ImGui.TextUnformatted($"Profession: {LoadedEntity.Profession}");
                    ImGui.TextUnformatted($"ProfessionSpec: {LoadedEntity.SubProfession}");
                    ImGui.TextUnformatted($"EntityType: {LoadedEntity.EntityType.ToString()}");

                    ImGui.TableNextColumn();

                    if (ImGui.BeginTable("##EntityStats", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("##LeftSide", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 0);
                        ImGui.TableSetupColumn("##RightSide", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoResize, 1f, 1);

                        ImGui.TableNextColumn();

                        var hp = LoadedEntity.Hp;
                        if (hp == 0)
                        {
                            hp = LoadedEntity.GetAttrKV("AttrHp") as int? ?? 0;
                        }
                        var maxHp = LoadedEntity.MaxHp;
                        if (maxHp == 0)
                        {
                            maxHp = LoadedEntity.GetAttrKV("AttrMaxHp") as int? ?? 0;
                        }
                        float hpPct = 0;
                        if (hp >= 0 && hp <= maxHp)
                        {
                            if (maxHp == 0)
                            {
                                hpPct = 0;
                            }
                            else
                            {
                                hpPct = MathF.Round((float)hp / (float)maxHp, 2) * 100.0f;
                            }
                        }
                        ImGui.TextUnformatted($"HP: {hp:N0} ({hpPct}%)");
                        
                        ImGui.TextUnformatted($"Max HP: {maxHp:N0}");
                        ImGui.TextUnformatted($"ATK: {LoadedEntity.GetAttrKV("AttrAttack") ?? "0"}");
                        string MainStat = Professions.GetBaseProfessionMainStatName(LoadedEntity.ProfessionId);
                        if (MainStat == "Strength" || MainStat == "")
                        {
                            ImGui.TextUnformatted($"Strength: {LoadedEntity.GetAttrKV("AttrStrength") ?? "0"}");
                        }
                        else if (MainStat == "Agility")
                        {
                            ImGui.TextUnformatted($"Agility: {LoadedEntity.GetAttrKV("AttrDexterity") ?? "0"}");
                        }
                        else if (MainStat == "Intellect")
                        {
                            ImGui.TextUnformatted($"Intellect: {LoadedEntity.GetAttrKV("AttrIntelligence") ?? "0"}");
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
                        ImGui.TextUnformatted($"Versatility: {VersatilityPctValue}% ({VersatilityValue})");

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
                        case ETableFilterMode.EntityTaken:
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
                        ImGui.TextUnformatted($"{valueTotalPerSecondLabel} {Utils.NumberToShorthand(combatStats.ValuePerSecondActive)} ({Utils.NumberToShorthand(combatStats.ValuePerSecond)})");
                        ImGui.SetItemTooltip($"{combatStats.ValuePerSecondActive:N0} ({combatStats.ValuePerSecond:N0})");
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
                        else if (TableFilterMode == ETableFilterMode.SkillsTaken || TableFilterMode == ETableFilterMode.EntityTaken)
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
                        else if (TableFilterMode == ETableFilterMode.SkillsTaken || TableFilterMode == ETableFilterMode.EntityTaken)
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

                string[] FilterButtons = { "Damage", "Healing", "Taken", "Taken By Entity", "Attributes", "Buffs", "Graphs", "Skill Book", "Debug" };

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

                    int columnCount = 9;
                    if (TableFilterMode == ETableFilterMode.SkillsDamage)
                    {
                        columnCount = 9;
                    }
                    else if (TableFilterMode == ETableFilterMode.SkillsHealing)
                    {
                        columnCount = 9;
                    }
                    else if (TableFilterMode == ETableFilterMode.SkillsTaken)
                    {
                        columnCount = 10;
                    }

                    if (ImGui.BeginTable("##SkillStatsTable", columnCount, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                    {
                        string valueTotalColumnName = "Damage";
                        string valuePerSecondColumnName = "DPS";
                        string valueShareColumnName = "DMG %";
                        string valueExtraStatColumnName = "";

                        IReadOnlyList<KeyValuePair<int, CombatStats>> skillStats = null;

                        switch (TableFilterMode)
                        {
                            case ETableFilterMode.SkillsDamage:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillMetrics.AsValueEnumerable().Where(x => x.Value.Damage.ValueTotal > 0).OrderByDescending(x => x.Value.Damage.ValueTotal).Select(x => new KeyValuePair<int, CombatStats>(x.Key, x.Value.Damage)).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "DPS";
                                valueShareColumnName = "DMG %";
                                break;
                            case ETableFilterMode.SkillsHealing:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillMetrics.AsValueEnumerable().Where(x => x.Value.Healing.ValueTotal > 0).OrderByDescending(x => x.Value.Healing.ValueTotal).Select(x => new KeyValuePair<int, CombatStats>(x.Key, x.Value.Healing)).ToList());
                                valueTotalColumnName = "Healing";
                                valuePerSecondColumnName = "HPS";
                                valueShareColumnName = "HEAL %";
                                break;
                            case ETableFilterMode.SkillsTaken:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillMetrics.AsValueEnumerable().Where(x => x.Value.Taken.ValueTotal > 0).OrderByDescending(x => x.Value.Taken.ValueTotal).Select(x => new KeyValuePair<int, CombatStats>(x.Key, x.Value.Taken)).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "DPS";
                                valueShareColumnName = "DMG %";
                                valueExtraStatColumnName = "Deaths";
                                break;
                            default:
                                skillStats = (IReadOnlyList<KeyValuePair<int, CombatStats>>)(LoadedEntity.SkillMetrics.AsValueEnumerable().Where(x => x.Value.Damage.ValueTotal > 0).OrderByDescending(x => x.Value.Damage.ValueTotal).Select(x => new KeyValuePair<int, CombatStats>(x.Key, x.Value.Damage)).ToList());
                                valueTotalColumnName = "Damage";
                                valuePerSecondColumnName = "DPS";
                                valueShareColumnName = "DMG %";
                                break;
                        }

                        ImGui.TableSetupColumn("ID");
                        ImGui.TableSetupColumn("Skill Name", ImGuiTableColumnFlags.WidthStretch, 100f);
                        ImGui.TableSetupColumn(valueTotalColumnName);
                        ImGui.TableSetupColumn($"a{valuePerSecondColumnName}");
                        ImGui.TableSetupColumn($"e{valuePerSecondColumnName}");
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
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.MenuItem("Copy Skill ID"))
                                {
                                    ImGui.SetClipboardText(skillId.ToString());
                                }
                                if (ImGui.MenuItem("Copy Skill Name"))
                                {
                                    ImGui.SetClipboardText(stat.Value.Name);
                                }
                                if (ImGui.MenuItem("Show All Instances"))
                                {
                                    if (ShowAllInstancesSkillIds.Contains(skillId))
                                    {
                                        ShowAllInstancesSkillIds.Remove(skillId);
                                    }
                                    else
                                    {
                                        ShowAllInstancesSkillIds.Add(skillId);
                                    }
                                }
                                ImGui.SetItemTooltip("Note: Skills with more than 1,000 Hits may cause lag if you show all instances");
                                ImGui.EndPopup();
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
                            ulong shieldBreakTotal = stat.Value.ShieldBreakTotal;
                            ulong immuneDamageTotal = stat.Value.ValueImmuneTotal;
                            if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
                            {
                                ImGui.TextUnformatted($"{stat.Value.ValueTotal:N0}");
                                ImGui.TextUnformatted($"Type: {stat.Value.DamageMode}\nElement: {Utils.DamagePropertyToString(stat.Value.DamageElement)}");
                                if (shieldBreakTotal > 0)
                                {
                                    ImGui.TextUnformatted($"Shield Break Damage: {Utils.NumberToShorthand(shieldBreakTotal)}");
                                }
                                if (immuneDamageTotal > 0)
                                {
                                    ImGui.TextUnformatted($"Immuned Damage: {Utils.NumberToShorthand(immuneDamageTotal)}");
                                }
                                ImGui.EndTooltip();
                            }

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(stat.Value.ValuePerSecondActive)}");
                            ImGui.SetItemTooltip($"Active DPS: {stat.Value.ValuePerSecondActive:N0}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(stat.Value.ValuePerSecond)}");
                            ImGui.SetItemTooltip($"Encounter DPS: {stat.Value.ValuePerSecond:N0}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{stat.Value.HitsCount}");
                            if (stat.Value.ImmuneCount > 0 || stat.Value.LuckyCount > 0 || stat.Value.CastsCount > 0)
                            {
                                var sb = new StringBuilder();

                                if (stat.Value.CastsCount > 0)
                                {
                                    sb.AppendLine($"Casts Count: {stat.Value.CastsCount}");
                                }

                                if (stat.Value.ImmuneCount > 0)
                                {
                                    sb.AppendLine($"Immune Count: {stat.Value.ImmuneCount}");
                                }

                                if (stat.Value.LuckyCount > 0)
                                {
                                    sb.AppendLine($"Lucky Count: {stat.Value.LuckyCount}");
                                }

                                ImGui.SetItemTooltip($"{sb}");
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

                            if (ShowAllInstancesSkillIds.Contains(skillId))
                            {
                                var startTime = LoadedEncounterStartTime?.ToUniversalTime() ?? LoadedEntity.DamageStats.StartTime;

                                int snapshotIdx = 0;
                                foreach (var snapshot in stat.Value.SkillSnapshots.AsValueEnumerable())
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn(); // Skip ID Column

                                    ImGui.TableNextColumn(); // Skip Name Column
                                    ImGui.TextUnformatted(snapshot.Timestamp.Value.Subtract(startTime.Value).ToString());

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted(Utils.NumberToShorthand(snapshot.Value));

                                    ImGui.TableNextColumn(); // Skip PerSecondActive Column

                                    ImGui.TableNextColumn(); // Skip PerSecond Column

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted($"Hit: {snapshotIdx + 1}");

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted($"Crit: {snapshot.IsCrit}");

                                    ImGui.TableNextColumn(); // Skip AVG Column

                                    ImGui.TableNextColumn(); // Skip Total Column
                                    snapshotIdx++;
                                }
                                // Force down to the next row just in case we missed a column
                                ImGui.TableNextRow();
                            }
                        }

                        ImGui.EndTable();
                    }

                    ImGui.PopStyleVar();
                }
                else if (TableFilterMode == ETableFilterMode.EntityTaken)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, ImGui.GetStyle().CellPadding.Y));

                    if (ImGui.BeginTable("##TakenByEntityTable", 9, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                    {
                        var entityGroupedSkills = LoadedEntity.TakenStats.SkillSnapshots.AsValueEnumerable().GroupBy(x => x.OtherUUID);
                        var interactedEntities = LoadedEntity.InteractedEntities.Where(x => x.Value.DidTaken && x.Value.Taken.HitCount > 0).OrderByDescending(x => x.Value.Taken.TotalValue);

                        ImGui.TableSetupColumn("#");
                        ImGui.TableSetupColumn("UID");
                        ImGui.TableSetupColumn("Entity Name", ImGuiTableColumnFlags.WidthStretch, 100f);
                        ImGui.TableSetupColumn("Profession");
                        ImGui.TableSetupColumn("Damage");
                        ImGui.TableSetupColumn("Total DPS");
                        ImGui.TableSetupColumn("Hit Count");
                        ImGui.TableSetupColumn("Crit Rate");
                        ImGui.TableSetupColumn("Avg Per Hit");

                        ImGui.TableHeadersRow();

                        //ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, ImGui.GetStyle().CellPadding.Y + 2));

                        long idx = 0;
                        var startTime = LoadedEncounterStartTime?.ToUniversalTime() ?? LoadedEntity.TakenStats.StartTime;
                        var endTime = LoadedEntity.TakenStats.EndTime;
                        var duration = endTime - startTime;
                        foreach (var entity in interactedEntities)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();

                            string profession = "";
                            if (entity.Value.SubProfessionId > 0)
                            {
                                profession = Professions.GetSubProfessionNameFromId(entity.Value.SubProfessionId);
                            }
                            else if (entity.Value.ProfessionId > 0)
                            {
                                profession = Professions.GetProfessionNameFromId(entity.Value.ProfessionId);
                            }
                            if (!string.IsNullOrEmpty(profession))
                            {
                                var color = Professions.ProfessionColors(profession);
                                color = color - new Vector4(0, 0, 0, 0.50f);

                                ImGui.PushStyleColor(ImGuiCol.Header, color);
                            }

                            if (ImGui.Selectable($"{idx + 1}##RankNum_{idx}", true, ImGuiSelectableFlags.SpanAllColumns))
                            {

                            }

                            if (!string.IsNullOrEmpty(profession))
                            {
                                ImGui.PopStyleColor();
                            }

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{entity.Value.UID}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(entity.Value.Name);

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(profession);

                            ImGui.TableNextColumn();
                            var totalDmgContribution = Math.Round(((double)entity.Value.Taken.TotalValue / (double)LoadedEntity.TakenStats.ValueTotal) * 100.0, 0);
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.Value.Taken.TotalValue)} ({totalDmgContribution}%)");

                            ImGui.TableNextColumn();
                            double perSecond = entity.Value.Taken.TotalValue / duration.Value.TotalSeconds;
                            if (perSecond < 1.0f)
                            {
                                perSecond = 0;
                            }
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(perSecond)}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.Value.Taken.HitCount)}");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Math.Round(((double)entity.Value.Taken.CritCount / (double)entity.Value.Taken.HitCount) * 100.0, 0)}%");

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{Utils.NumberToShorthand((double)entity.Value.Taken.TotalValue / (double)entity.Value.Taken.HitCount)}");

                            idx++;
                        }

                        //ImGui.PopStyleVar();

                        ImGui.EndTable();
                    }

                    ImGui.PopStyleVar();
                }
                else if (TableFilterMode == ETableFilterMode.Attributes)
                {
                    ImGui.TextUnformatted("Attributes:");

                    if (ImGui.BeginListBox("##AttrListBox", new Vector2(-1, -32)))
                    {
                        var attributes = (IReadOnlyList<KeyValuePair<string, object>>)LoadedEntity.Attributes.AsValueEnumerable().ToList();
                        int idx = 0;
                        foreach (var attr in attributes)
                        {
                            if (attr.Key != "$type")
                            {
                                if (attr.Key.Contains(AttributeFilter, StringComparison.OrdinalIgnoreCase))
                                {
                                    ImGui.TextUnformatted($"[{idx}] {attr.Key} = {attr.Value.ToString()}");
                                }

                                idx++;
                            }
                        }

                        // Create a ReadOnlyList to try and avoid modification errors
                        /*var attributes = (IReadOnlyList<KeyValuePair<string, object>>)(LoadedEntity.Attributes.ToList());
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            var attr = attributes.ElementAt(i);
                            if (attr.Key != "$type")
                            {
                                ImGui.TextUnformatted($"[{i}] {attr.Key} = {attr.Value.ToString()}");
                            }
                        }*/

                        if (ImGui.BeginPopupContextWindow())
                        {
                            if (ImGui.MenuItem("Copy All Attributes"))
                            {
                                StringBuilder format = new();
                                int cidx = 0;
                                foreach (var attr in attributes)
                                {
                                    if (attr.Key != "$type")
                                    {
                                        format.AppendLine($"[{cidx}] {attr.Key} = {attr.Value.ToString()}");

                                        cidx++;
                                    }
                                }
                                ImGui.SetClipboardText(format.ToString());
                            }
                            ImGui.SetItemTooltip("Copies all attributes (ignoring the active filter) for the entity to the clipboard.");

                            ImGui.EndPopup();
                        }

                        ImGui.EndListBox();
                    }

                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Filter: ");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputText("##AttributeFilterInpuit", ref AttributeFilter, 128);
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
                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.MenuItem("Copy Buff Id"))
                                    {
                                        ImGui.SetClipboardText(buffEvent.BaseId.ToString());
                                    }
                                    if (ImGui.MenuItem("Copy Buff Name"))
                                    {
                                        ImGui.SetClipboardText(buffEvent.Name);
                                    }
                                    if (ImGui.MenuItem("Copy Buff Description"))
                                    {
                                        ImGui.SetClipboardText(buffEvent.Description);
                                    }
                                    if (ImGui.MenuItem("Copy Skill Id"))
                                    {
                                        ImGui.SetClipboardText(buffEvent.SourceConfigId.ToString());
                                    }
                                    ImGui.EndPopup();
                                }

                                if (buffTypeColor > -1)
                                {
                                    ImGui.PopStyleColor();
                                }

                                if (ImGui.BeginItemTooltip())
                                {
                                    if (string.IsNullOrEmpty(buffEvent.Description))
                                    {
                                        if (HelperMethods.DataTables.Buffs.Data.TryGetValue(buffEvent.BaseId.ToString(), out var buffTableData))
                                        {
                                            buffEvent.SetDescription(buffTableData.Desc);
                                        }
                                        else
                                        {
                                            buffEvent.SetDescription("");
                                        }
                                    }
                                    ImGui.TextUnformatted($"Buff Id: {buffEvent.BaseId}\n{buffEvent.Description}{extraTooltip}");
                                    ImGui.EndTooltip();
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

                        if (HasLoadedGraphsData && LoadedEntity.DamageStats.SkillSnapshots.Count != SkillSnapshotsDamage.Length - 1)
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

                            SkillSnapshotsNames = LoadedEntity.SkillMetrics.AsValueEnumerable().Where(x => x.Value.Damage.HitsCount > 0).Select(x => x.Value.Damage.Name ?? "").ToArray();
                            SkillSnapshotsHits = LoadedEntity.SkillMetrics.AsValueEnumerable().Where(x => x.Value.Damage.HitsCount > 0).Select(x => (float)x.Value.Damage.HitsCount).ToArray();

                            // Build Skill Hit scatter data (very expensive)
                            var tempScatterMap = new ScatterPlotSkillMap();
                            tempScatterMap.Time.Add(0);
                            tempScatterMap.SkillIdentifier.Add(0);
                            SkillScatterMap[""] = tempScatterMap;
                            foreach (var item in LoadedEntity.DamageStats.SkillSnapshots)
                            {
                                string skillName = "";
                                if (LoadedEntity.SkillMetrics.TryGetValue(item.Id, out var skillMetric))
                                {
                                    skillName = skillMetric.Damage.Name;
                                }

                                if (!string.IsNullOrEmpty(skillName))
                                {
                                    skillName += $" [{item.Id}]";
                                }
                                else
                                {
                                    skillName = $"[{item.Id}]";
                                }

                                double skillMapIdx = 0;
                                if (!SkillScatterMap.TryGetValue(skillName, out var map))
                                {
                                    map = new();
                                    //map.Time.Add(0);
                                    //map.SkillIdentifier.Add(SkillScatterMap.Count);
                                    skillMapIdx = SkillScatterMap.Count;
                                }
                                else
                                {
                                    skillMapIdx = map.SkillIdentifier.First();
                                }
                                map.Time.Add(item.Timestamp.Value.Subtract(startTime.Value).TotalSeconds);
                                map.SkillIdentifier.Add(skillMapIdx);

                                SkillScatterMap[skillName] = map;
                            }
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

                        if (ImPlot.BeginPlot("Damage Skills Timeline", new Vector2(-1, 520), ImPlotFlags.None))
                        {
                            ImPlot.SetupAxes("Time (Encounter Duration In Seconds)", "Casts", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit | ImPlotAxisFlags.NoTickLabels);

                            foreach (var item in SkillScatterMap)
                            {
                                ImPlot.PlotScatter(item.Key, ref item.Value.TimeArray[0], ref item.Value.SkillIdentifierArray[0], item.Value.Time.Count);
                            }

                            ImPlot.EndPlot();
                        }
                    }
                }
                else if (TableFilterMode == ETableFilterMode.SkillBook)
                {
                    var skillLevelIdList = LoadedEntity.GetAttrKV("AttrSkillLevelIdList");
                    if (skillLevelIdList != null)
                    {
                        if (skillLevelIdList is Newtonsoft.Json.Linq.JArray)
                        {
                            // This is a Historical Entity, need to restore the original object type
                            skillLevelIdList = ((Newtonsoft.Json.Linq.JArray)skillLevelIdList).ToObject<List<DataTypes.Skills.SkillLevelInfo>>();
                            LoadedEntity.SetAttrKV("AttrSkillLevelIdList", skillLevelIdList);
                        }

                        if (skillLevelIdList is List<DataTypes.Skills.SkillLevelInfo>)
                        {
                            var list = (List<DataTypes.Skills.SkillLevelInfo>)skillLevelIdList;

                            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, ImGui.GetStyle().CellPadding.Y));

                            if (ImGui.BeginTable("##SkillStatsTable", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                            {
                                ImGui.TableSetupColumn("ID");
                                ImGui.TableSetupColumn("Skill Name", ImGuiTableColumnFlags.WidthStretch, 100f);
                                ImGui.TableSetupColumn("Level");
                                ImGui.TableSetupColumn("Tier");

                                ImGui.TableHeadersRow();

                                foreach (var item in list)
                                {
                                    ImGui.TableNextColumn();
                                    ImGui.Selectable($"{item.SkillId}", true, ImGuiSelectableFlags.SpanAllColumns);
                                    if (ImGui.BeginPopupContextItem())
                                    {
                                        if (ImGui.MenuItem("Copy Skill ID"))
                                        {
                                            ImGui.SetClipboardText(item.SkillId.ToString());
                                        }
                                        if (ImGui.MenuItem("Copy Skill Name"))
                                        {
                                            ImGui.SetClipboardText(item.Name);
                                        }
                                        ImGui.EndPopup();
                                    }

                                    ImGui.TableNextColumn();
                                    if (Settings.Instance.ShowSkillIconsInDetails)
                                    {
                                        var skillIconName = item.GetIconName();

                                        var itemRectSize = ImGui.GetItemRectSize().Y;
                                        float texSize = itemRectSize;

                                        if (!string.IsNullOrEmpty(skillIconName))
                                        {
                                            var tex = ImageArchive.LoadImage(Path.Combine("Skills", skillIconName));

                                            if (tex != null)
                                            {
                                                ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                                ImGui.SameLine();
                                            }
                                            else
                                            {
                                                ImGui.Dummy(new Vector2(0, texSize));
                                                ImGui.SameLine();
                                            }
                                        }
                                        else
                                        {
                                            ImGui.Dummy(new Vector2(0, texSize));
                                            ImGui.SameLine();
                                        }
                                    }
                                    ImGui.TextUnformatted($"{item.Name}");

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted($"{item.CurrentLevel}");

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted($"{item.Tier}");
                                }

                                ImGui.EndTable();
                            }

                            ImGui.PopStyleVar();
                        }
                    }
                }
                else if (TableFilterMode == ETableFilterMode.Debug)
                {
                    ImGui.TextUnformatted($"UUID: {LoadedEntity.UUID}");
                    ImGui.TextUnformatted($"MonsterType: {LoadedEntity.MonsterType}");
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
            SkillScatterMap.Clear();
            ShowAllInstancesSkillIds = new();
        }
    }

    public class ScatterPlotSkillMap
    {
        public List<double> Time = new(); // X Axis
        public List<double> SkillIdentifier = new(); // Y Axis (Index of Skill based on appearance order, used for deprojection mapping)

        public Span<double> TimeArray => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Time);
        public Span<double> SkillIdentifierArray => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(SkillIdentifier);
    }
}
