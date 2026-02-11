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
    public class HealingMeter : MeterBase
    {
        public HealingMeter()
        {
            Name = "Healing";
        }

        public override void Draw(MainWindow mainWindow)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, ImGui.GetStyle().FramePadding.Y));

            if (ImGui.BeginListBox("##HealingMeterList", new Vector2(-1, -1)))
            {
                ImGui.PopStyleVar();

                Encounter? activeEncounter = null;

                if (AppState.OpenedHistoricalEncounter != null)
                {
                    activeEncounter = AppState.OpenedHistoricalEncounter;
                }

                if (Settings.Instance.KeepPastEncounterInMeterUntilNextDamage)
                {
                    if ((AppState.ActiveEncounter == null && EncounterManager.Current != null) || (AppState.ActiveEncounter != null && AppState.ActiveEncounter.Entities.IsEmpty))
                    {
                        AppState.ActiveEncounter = EncounterManager.Current;
                    }
                    else if (AppState.ActiveEncounter?.BattleId != EncounterManager.Current?.BattleId)
                    {
                        AppState.ActiveEncounter = EncounterManager.Current;
                    }
                    else if (AppState.ActiveEncounter?.EncounterId != EncounterManager.Current?.EncounterId)
                    {
                        if (EncounterManager.Current.HasStatsBeenRecorded())
                        {
                            AppState.ActiveEncounter = EncounterManager.Current;
                        }
                    }
                }
                else
                {
                    if (AppState.ActiveEncounter?.EncounterId != EncounterManager.Current?.EncounterId || AppState.ActiveEncounter?.BattleId != EncounterManager.Current?.BattleId)
                    {
                        AppState.ActiveEncounter = EncounterManager.Current;
                    }
                }

                if (activeEncounter == null)
                {
                    activeEncounter = AppState.ActiveEncounter;
                }

                var playerList = activeEncounter.Entities.AsValueEnumerable()
                    .Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar && x.Value.TotalHealing > 0)
                    .OrderByDescending(x => x.Value.TotalHealing).ToArray();

                ulong topTotalValue = 0;

                int entityIdx = 0;
                foreach (var entity in playerList)
                {
                    if (entityIdx == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.Value.TotalHealing;
                    }

                    if (AppState.PlayerUUID != 0 && AppState.PlayerUUID == entity.Value.UUID)
                    {
                        AppState.PlayerMeterPlacement = entityIdx + 1;
                        AppState.PlayerTotalMeterValue = entity.Value.TotalHealing;
                        AppState.PlayerMeterValuePerSecond = entity.Value.HealingStats.ValuePerSecond;

                        // We can exit the loop now since we don't need anything else
                        break;
                    }
                    entityIdx++;
                }

                ImGuiListClipper clipper = new();
                clipper.Begin(playerList.Count());
                while(clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var player = playerList[i];

                        var entity = player.Value;

                        if (i == 0 && Settings.Instance.NormalizeMeterContributions)
                        {
                            topTotalValue = entity.TotalHealing;
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
                        if (AppState.PlayerUUID != 0 && AppState.PlayerUUID == (long)entity.UUID)
                        {
                            AppState.PlayerMeterPlacement = i + 1;
                            AppState.PlayerTotalMeterValue = entity.TotalHealing;
                            AppState.PlayerMeterValuePerSecond = entity.HealingStats.ValuePerSecond;
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
                        if (activeEncounter.TotalHealing != 0)
                        {
                            contribution = Math.Round(((double)entity.TotalHealing / (double)activeEncounter.TotalHealing) * 100, 4);

                            if (Settings.Instance.NormalizeMeterContributions)
                            {
                                contributionProgressBar = Math.Round(((double)entity.TotalHealing / (double)topTotalValue) * 100, 4);
                            }
                            else
                            {
                                contributionProgressBar = contribution;
                            }
                        }
                        string activePerSecond = "";
                        if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
                        {
                            activePerSecond = $"[{Utils.NumberToShorthand(entity.HealingStats.ValuePerSecondActive)}] ";
                        }
                        string totalHealing = Utils.NumberToShorthand(entity.TotalHealing);
                        string totalHps = Utils.NumberToShorthand(entity.HealingStats.ValuePerSecond);
                        string hps_format = $"{totalHealing} {activePerSecond}({totalHps}) {contribution.ToString("F0").PadLeft(3, ' ')}%";
                        var startPoint = ImGui.GetCursorPos();

                        ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f * Settings.Instance.WindowSettings.MainWindow.MeterBarScale);

                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Professions.ProfessionColors(profession));
                        ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##HpsEntryContribution_{i}");
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
                        if (SelectableWithHintImage($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}.", $"{name}{professionStr}{abilityScoreStr}##HpsEntry_{i}", hps_format, entity.ProfessionId))
                        //if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##HpsEntry_{i}", hps_format))
                        {
                            mainWindow.entityInspector = new EntityInspector();
                            mainWindow.entityInspector.LoadEntity(entity, activeEncounter.StartTime);
                            mainWindow.entityInspector.Open();
                        }

                        ImGui.PopFont();
                    }
                }
                clipper.End();

                ImGui.EndListBox();
            }
            else
            {
                ImGui.PopStyleVar();
            }
        }
    }
}
