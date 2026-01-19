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

                var playerList = ActiveEncounter?.Entities.AsValueEnumerable().Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar && (x.Value.TotalTakenDamage > 0 || x.Value.TotalDeaths > 0)).OrderByDescending(x => x.Value.TotalTakenDamage).ToArray();

                ulong topTotalValue = 0;

                for (int i = 0; i < playerList?.Count(); i++)
                {
                    var entity = playerList[i].Value;

                    if (i == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.TotalTakenDamage;
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
                    if (AppState.PlayerUID != 0 && AppState.PlayerUID == (long)entity.UID)
                    {
                        AppState.PlayerMeterPlacement = i + 1;
                        AppState.PlayerTotalMeterValue = entity.TotalTakenDamage;
                        AppState.PlayerMeterValuePerSecond = entity.TakenStats.ValuePerSecond;
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
                    if (ActiveEncounter.TotalHealing != 0)
                    {
                        contribution = Math.Round(((double)entity.TotalTakenDamage / (double)ActiveEncounter.TotalTakenDamage) * 100, 4);

                        if (Settings.Instance.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalTakenDamage / (double)topTotalValue) * 100, 4);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string truePerSecond = "";
                    if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
                    {
                        truePerSecond = $"[{Utils.NumberToShorthand(entity.TakenStats.TrueValuePerSecond)}] ";
                    }
                    string totalTaken = Utils.NumberToShorthand(entity.TotalTakenDamage);
                    string totalTps = Utils.NumberToShorthand(entity.TakenStats.ValuePerSecond);
                    StringBuilder format = new();

                    if (Settings.Instance.MeterSettingsTankingShowDeaths)
                    {
                        format.Append($"[ {entity.TotalDeaths} ] ");
                    }

                    format.Append($"{totalTaken} {truePerSecond}({totalTps}) {contribution.ToString("F0").PadLeft(3, ' ')}%");
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
