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
    public class TankingMeter : MeterBase
    {
        ImGuiListClipper clipper = new();

        public TankingMeter()
        {
            Name = "Tanking";
        }

        public override void Draw(MainWindow mainWindow)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, ImGui.GetStyle().FramePadding.Y));

            if (ImGui.BeginListBox("##TankingMeterList", new Vector2(-1, -1)))
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

                var playerList = activeEncounter.Entities.AsValueEnumerable().Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar && (x.Value.TotalTakenDamage > 0 || x.Value.TotalDeaths > 0)).OrderByDescending(x => x.Value.TotalTakenDamage).ToArray();

                ulong topTotalValue = 0;

                int entityIdx = 0;
                foreach (var entity in playerList)
                {
                    if (entityIdx == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.Value.TotalTakenDamage;
                    }

                    if (AppState.PlayerUUID != 0 && AppState.PlayerUUID == entity.Value.UUID)
                    {
                        AppState.PlayerMeterPlacement = entityIdx + 1;
                        AppState.PlayerTotalMeterValue = entity.Value.TotalTakenDamage;
                        AppState.PlayerMeterValuePerSecond = entity.Value.TakenStats.ValuePerSecond;

                        // We can exit the loop now since we don't need anything else
                        break;
                    }
                    entityIdx++;
                }

                clipper.Begin(playerList.Count());
                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var player = playerList[i];

                        var entity = player.Value;

                        string name = "Unknown";
                        if (!string.IsNullOrEmpty(entity.Name))
                        {
                            name = entity.Name;
                        }
                        else
                        {
                            name = $"[U:{entity.UID}]";
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
                            contribution = Math.Round(((double)entity.TotalTakenDamage / (double)activeEncounter.TotalTakenDamage) * 100, 4);

                            if (Settings.Instance.NormalizeMeterContributions)
                            {
                                contributionProgressBar = Math.Round(((double)entity.TotalTakenDamage / (double)topTotalValue) * 100, 4);
                            }
                            else
                            {
                                contributionProgressBar = contribution;
                            }
                        }
                        string activePerSecond = "";
                        if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
                        {
                            activePerSecond = $"[{Utils.NumberToShorthand(entity.TakenStats.ValuePerSecondActive)}] ";
                        }
                        string totalTaken = Utils.NumberToShorthand(entity.TotalTakenDamage);
                        string totalTps = Utils.NumberToShorthand(entity.TakenStats.ValuePerSecond);
                        StringBuilder format = new();

                        if (Settings.Instance.MeterSettingsTankingShowDeaths)
                        {
                            format.Append($"[ {entity.TotalDeaths} ] ");
                        }

                        format.Append($"{totalTaken} {activePerSecond}({totalTps}) {contribution.ToString("F0").PadLeft(3, ' ')}%");
                        var startPoint = ImGui.GetCursorPos();

                        ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f * Settings.Instance.WindowSettings.MainWindow.MeterBarScale);

                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Professions.ProfessionColors(profession));
                        ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##TpsEntryContribution_{i}");
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
                        if (SelectableWithHintImage($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}.", $"{name}{professionStr}{abilityScoreStr}##TpsEntry_{i}", format.ToString(), entity.ProfessionId))
                        //if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##TpsEntry_{i}", tps_format))
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
