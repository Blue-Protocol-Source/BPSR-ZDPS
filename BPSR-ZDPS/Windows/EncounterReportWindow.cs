using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Web;
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
    public class EncounterReportWindow
    {
        public const string LAYER = "EncounterReportLayer";

        public bool IsOpened = false;

        Encounter? LoadedEncounter = null;

        float LargestPositionX = 0.0f;
        float LargestPositionY = 0.0f;

        Vector2 WindowSize = new Vector2();

        public void Open(Encounter encounter)
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            IsOpened = true;
            LoadedEncounter = encounter;
            ImGui.PopID();
        }

        public Vector2 Draw()
        {
            if (!IsOpened)
            {
                return WindowSize;
            }

            if (LoadedEncounter == null)
            {
                return WindowSize;
            }

            var encounter = LoadedEncounter;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono_Offscreen"], 18f);

            string TitleText = $"ZDPS Report (v{Utils.AppVersion}) - Encounter: {encounter.SceneName} ({(encounter.EndTime - encounter.StartTime).ToString("hh\\:mm\\:ss")}) [ZTeamId: {Utils.CreateZTeamId(encounter)}]";
            ImGui.SetNextWindowSize(new Vector2(-1, -1), ImGuiCond.Always);
            ImGui.Begin($"{TitleText}###EncounterReportWindow", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize);

            ImGui.PushFont(HelperMethods.Fonts["Segoe_Offscreen"], 18f);
            // Removed ImGuiTableFlags.ScrollX for the direct SizingFixedFit flag instead to perform same layout be ensure the scroll bar never appears at the bottom
            if (ImGui.BeginTable("##ReportTable", 23, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit, new Vector2(-1, -1)))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("UID");
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Profession");
                ImGui.TableSetupColumn("Ability Score");
                ImGui.TableSetupColumn("Total DMG");
                ImGui.TableSetupColumn("Total DPS");
                ImGui.TableSetupColumn("Crit Rate");
                ImGui.TableSetupColumn("Lucky Rate");
                ImGui.TableSetupColumn("Crit DMG");
                ImGui.TableSetupColumn("Lucky DMG");
                ImGui.TableSetupColumn("Crit Lucky DMG");
                ImGui.TableSetupColumn("Max Single DMG");
                ImGui.TableSetupColumn("Total Healing");
                ImGui.TableSetupColumn("Total HPS");
                ImGui.TableSetupColumn("Effective Healing");
                ImGui.TableSetupColumn("Total Overhealing");
                ImGui.TableSetupColumn("Crit Healing");
                ImGui.TableSetupColumn("Lucky Healing");
                ImGui.TableSetupColumn("Crit Lucky Healing");
                ImGui.TableSetupColumn("Max Single Heal");
                ImGui.TableSetupColumn("Damage Taken");
                ImGui.TableSetupColumn("Deaths");

                ImGui.TableHeadersRow();

                var lastColumnWidth = ImGui.GetItemRectSize().X;

                var entities = encounter.Entities.AsValueEnumerable()
                    .Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar || (x.Value.EntityType == Zproto.EEntityType.EntMonster && x.Value.TotalDamage > 0))
                    .OrderByDescending(x => x.Value.TotalDamage);

                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, ImGui.GetStyle().CellPadding.Y + 2));

                int entityIndex = 0;
                foreach (var entityObj in entities)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    var entity = entityObj.Value;

                    string profession = entity.SubProfession ?? entity.Profession ?? "";
                    if (!string.IsNullOrEmpty(profession))
                    {
                        var color = Professions.ProfessionColors(profession);
                        color = color - new Vector4(0, 0, 0, 0.50f);
                        ImGui.PushStyleColor(ImGuiCol.Header, color);
                    }

                    ImGui.Selectable($"{entityIndex + 1}##EntSelectable_{entityIndex}", true, ImGuiSelectableFlags.SpanAllColumns);

                    if (!string.IsNullOrEmpty(profession))
                    {
                        ImGui.PopStyleColor();
                    }

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(entity.UID.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(entity.Name ?? $"[{entity.UID}]");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(profession);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(entity.AbilityScore.ToString());

                    ImGui.TableNextColumn();
                    string totalDamageDealt = Utils.NumberToShorthand(entity.TotalDamage);
                    double totalDamagePct = 0;
                    if (entity.TotalDamage > 0)
                    {
                        if (entity.EntityType == Zproto.EEntityType.EntMonster)
                        {
                            totalDamagePct = Math.Round(((double)entity.TotalDamage / (double)encounter.TotalNpcDamage) * 100, 0);
                        }
                        else
                        {
                            totalDamagePct = Math.Round(((double)entity.TotalDamage / (double)encounter.TotalDamage) * 100, 0);
                        }
                    }
                    ImGui.TextUnformatted($"{totalDamageDealt} ({totalDamagePct}%)");
                    //ImGui.TextUnformatted($"999.99M (100%)"); // Placeholder max value width

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(entity.DamageStats.ValuePerSecond));

                    // ShieldBreak

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{entity.DamageStats.CritRate}%");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{entity.DamageStats.LuckyRate}%");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.DamageStats.ValueCritTotal)}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.DamageStats.ValueLuckyTotal)}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.DamageStats.ValueCritLuckyTotal)}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.DamageStats.ValueMax)}");

                    // TotalShield

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(entity.TotalHealing));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(entity.HealingStats.ValuePerSecond));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(entity.TotalHealing - entity.TotalOverhealing));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(entity.TotalOverhealing));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.HealingStats.ValueCritTotal)}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.HealingStats.ValueLuckyTotal)}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.HealingStats.ValueCritLuckyTotal)}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.NumberToShorthand(entity.HealingStats.ValueMax)}");

                    ImGui.TableNextColumn();
                    string totalDamageTaken = Utils.NumberToShorthand(entity.TotalTakenDamage);
                    double totalDamageTakenPct = 0;
                    if (entity.TotalTakenDamage > 0)
                    {
                        if (entity.EntityType == Zproto.EEntityType.EntChar)
                        {
                            totalDamageTakenPct = Math.Round(((double)entity.TotalTakenDamage / (double)encounter.TotalTakenDamage) * 100, 0);
                        }
                        else if (entity.EntityType == Zproto.EEntityType.EntMonster)
                        {
                            totalDamageTakenPct = Math.Round(((double)entity.TotalTakenDamage / (double)encounter.TotalNpcTakenDamage) * 100, 0);
                        }
                    }
                    ImGui.TextUnformatted($"{totalDamageTaken} ({totalDamageTakenPct}%)");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{entity.TotalDeaths}");

                    var cursorPos = ImGui.GetCursorPos();
                    var endingPosX = cursorPos.X + lastColumnWidth;
                    if (endingPosX > LargestPositionX)
                    {
                        LargestPositionX = endingPosX;
                    }

                    var endingPosY = cursorPos.Y;
                    if (endingPosY > LargestPositionY)
                    {
                        LargestPositionY = endingPosY;
                    }

                    entityIndex++;
                }

                // Add a faked totals row
                bool showTotals = true;
                if (showTotals)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    var playerEntities = entities.Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar);

                    ImGui.Selectable("##TotalsRowSelectable", true, ImGuiSelectableFlags.SpanAllColumns);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("Totals");

                    ImGui.TableNextColumn();
                    // Name
                    ImGui.TextUnformatted("(Players)");

                    ImGui.TableNextColumn();
                    // Profession

                    ImGui.TableNextColumn();
                    // Ability Score

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(encounter.TotalDamage));

                    ImGui.TableNextColumn();
                    var dps = playerEntities.Select(x => x.Value.DamageStats.ValuePerSecond);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(dps.Sum()));

                    ImGui.TableNextColumn();
                    // Crit Rate

                    ImGui.TableNextColumn();
                    // Lucky Rate

                    ImGui.TableNextColumn();
                    // Crit Damage
                    var critDmg = playerEntities.Select(x => x.Value.DamageStats.ValueCritTotal);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(critDmg.Sum()));

                    ImGui.TableNextColumn();
                    // Lucky Damage
                    var luckyDmg = playerEntities.Select(x => x.Value.DamageStats.ValueLuckyTotal);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(luckyDmg.Sum()));

                    ImGui.TableNextColumn();
                    // Crit Lucky Damage
                    var critLuckyDmg = playerEntities.Select(x => x.Value.DamageStats.ValueCritLuckyTotal);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(critLuckyDmg.Sum()));

                    ImGui.TableNextColumn();
                    // Max Single Damage

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(encounter.TotalHealing));

                    ImGui.TableNextColumn();
                    var hps = playerEntities.Select(x => x.Value.HealingStats.ValuePerSecond);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(hps.Sum()));

                    ImGui.TableNextColumn();
                    var effectiveHealing = playerEntities.Select(x => x.Value.TotalHealing - x.Value.TotalOverhealing);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(effectiveHealing.Sum()));

                    ImGui.TableNextColumn();
                    var overhealing = playerEntities.Select(x => x.Value.TotalOverhealing);
                    ImGui.TextUnformatted(Utils.NumberToShorthand(overhealing.Sum()));

                    ImGui.TableNextColumn();
                    // Crit Healing

                    ImGui.TableNextColumn();
                    // Lucky Healing

                    ImGui.TableNextColumn();
                    // Crit Lucky Healing

                    ImGui.TableNextColumn();
                    // Max Single Heal

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.NumberToShorthand(encounter.TotalTakenDamage));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(encounter.TotalDeaths.ToString());
                }

                ImGui.PopStyleVar();

                ImGui.EndTable();
            }

            // This will not be a correct value on the first iteration of the window
            WindowSize = ImGui.GetWindowSize();
            if (WindowSize.X < LargestPositionX)
            {
                WindowSize.X = LargestPositionX;
            }
            if (WindowSize.Y < LargestPositionY)
            {
                WindowSize.Y = LargestPositionY;
            }

            ImGui.PopFont();

            ImGui.End();

            ImGui.PopFont();

            ImGui.PopID();

            return WindowSize;
        }
    }
}
