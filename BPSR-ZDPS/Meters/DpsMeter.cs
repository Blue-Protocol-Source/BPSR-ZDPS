using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

namespace BPSR_ZDPS.Meters
{
    public class DpsMeter : MeterBase
    {
        //static ImDrawListSplitter renderSplitter = new ImDrawListSplitter(); // Used for splitting the rendering pipeline to make overlays easier

        public DpsMeter()
        {
            Name = "DPS";
        }

        public override void Draw(MainWindow mainWindow)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, ImGui.GetStyle().FramePadding.Y));

            if (ImGui.BeginListBox("##DPSMeterList", new Vector2(-1, -1)))
            {
                ImGui.PopStyleVar();

                if (Settings.Instance.KeepPastEncounterInMeterUntilNextDamage)
                {
                    if (ActiveEncounter?.BattleId != EncounterManager.Current?.BattleId)
                    {
                        ActiveEncounter = EncounterManager.Current;
                    }
                    else if (ActiveEncounter?.EncounterId != EncounterManager.Current?.EncounterId)
                    {
                        if (EncounterManager.Current.HasStatsBeenRecorded())
                        {
                            ActiveEncounter = EncounterManager.Current;
                        }
                    }
                }
                else
                {
                    if (ActiveEncounter?.EncounterId != EncounterManager.Current?.EncounterId || ActiveEncounter?.BattleId != EncounterManager.Current?.BattleId)
                    {
                        ActiveEncounter = EncounterManager.Current;
                    }
                }

                var playerList = ActiveEncounter?.Entities.AsValueEnumerable()
                    .Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar && (Settings.Instance.OnlyShowDamageContributorsInMeters ? x.Value.TotalDamage > 0 : true))
                    .OrderByDescending(x => x.Value.TotalDamage).ToArray();

                ulong topTotalValue = 0;

                for (int i = 0; i < playerList?.Count(); i++)
                {
                    var entity = playerList[i].Value;

                    if (i == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.TotalDamage;
                    }

                    string name = "Unknown";
                    if (!string.IsNullOrEmpty(entity.Name))
                    {
                        name = entity.Name;
                    }
                    else
                    {
                        name = $"[U:{entity.UID}]";
                    }
                    if (AppState.PlayerUUID != 0 && AppState.PlayerUUID == entity.UUID)
                    {
                        AppState.PlayerMeterPlacement = i + 1;
                        AppState.PlayerTotalMeterValue = entity.TotalDamage;
                        AppState.PlayerMeterValuePerSecond = entity.DamageStats.ValuePerSecond;
                    }

                    string profession = "Unknown";
                    if (!string.IsNullOrEmpty(entity.SubProfession))
                    {
                        profession = entity.SubProfession;
                    }
                    else if (!string.IsNullOrEmpty(entity.Profession))
                    {
                        profession = entity.Profession;
                    }

                    double contribution = 0.0;
                    double contributionProgressBar = 0.0;
                    // TotalDamage is the player only total, TotalNpcDamage is only for monster's totals
                    ulong totalEncounterDamage = ActiveEncounter.TotalDamage;

                    if (totalEncounterDamage != 0)
                    {
                        contribution = Math.Round(((double)entity.TotalDamage / (double)totalEncounterDamage) * 100, 4);

                        if (Settings.Instance.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalDamage / (double)topTotalValue) * 100, 4);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string truePerSecond = "";
                    if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
                    {
                        truePerSecond = $"[{Utils.NumberToShorthand(entity.DamageStats.TrueValuePerSecond)}] ";
                    }
                    string dps_format = $"{Utils.NumberToShorthand(entity.TotalDamage)} {truePerSecond}({Utils.NumberToShorthand(entity.DamageStats.ValuePerSecond)}) {contribution.ToString("F0").PadLeft(3, ' ')}%"; // Format: TotalDamage (DPS) Contribution%
                    var startPoint = ImGui.GetCursorPos();
                    // ImGui.GetTextLineHeightWithSpacing();

                    ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f * Settings.Instance.WindowSettings.MainWindow.MeterBarScale);

                    // Begin the rendering split to overlay elements, we have to do it this way since Hexa.NET.ImGui blocks the normal functions
                    //var drawList = ImGui.GetWindowDrawList();
                    //renderSplitter.Split(drawList, 2);
                    //renderSplitter.SetCurrentChannel(drawList, 1);

                    // Add elements

                    // Merge back rendering to finalize the overlays
                    //renderSplitter.SetCurrentChannel(drawList, 0); // Switches us to the other layer of rendering
                    // Draws a colored rectangle over the prior Group element
                    //ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.ColorConvertFloat4ToU32(groupBackground), 5);
                    //renderSplitter.Merge(drawList);

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Professions.ProfessionColors(profession));
                    ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##DpsEntryContribution_{i}");
                    ImGui.PopStyleColor();

                    string professionStr = $"-{profession}";
                    if (!Settings.Instance.ShowSubProfessionNameInMeters)
                    {
                        professionStr = "";
                    }

                    string abilityScoreStr = $" ({entity.AbilityScore})";
                    if (!Settings.Instance.ShowAbilityScoreInMeters)
                    {
                        abilityScoreStr = "";
                    }

                    ImGui.SetCursorPos(startPoint);
                    if (SelectableWithHintImage($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}.", $"{name}{professionStr}{abilityScoreStr}##DpsEntry_{i}", dps_format, entity.ProfessionId))
                    //if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##DpsEntry_{i}", dps_format))
                    //if (ImGui.Selectable($"{name}-{profession} ({entity.AbilityScore}) [{entity.UID.ToString()}] ({entity.TotalDamage})##DpsEntry_{i}"))
                    {
                        mainWindow.entityInspector = new EntityInspector();
                        mainWindow.entityInspector.LoadEntity(entity, ActiveEncounter.StartTime);
                        mainWindow.entityInspector.Open();
                    }

                    ImGui.PopFont();
                }

                ImGui.EndListBox();
            }
            else
            {
                ImGui.PopStyleVar();
            }
        }
    }
}
