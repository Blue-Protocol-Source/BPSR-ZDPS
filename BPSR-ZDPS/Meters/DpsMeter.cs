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

                KeyValuePair<long, Entity>[]? entityList;

                var playerList = activeEncounter.Entities.AsValueEnumerable()
                    .Where(x => x.Value.EntityType == Zproto.EEntityType.EntChar && (Settings.Instance.OnlyShowDamageContributorsInMeters ? x.Value.TotalDamage > 0 : true))
                    .OrderByDescending(x => x.Value.TotalDamage);

                if(Settings.Instance.OnlyShowPartyMembersInMeters && AppState.PartyTeamId != 0 && AppState.PlayerUUID != 0)
                {
                    var teamList = playerList.Where(x =>
                    {
                        if (x.Value.UUID != AppState.PlayerUID)
                        {
                            var teamId = x.Value.GetAttrKV("AttrTeamId") as long?;
                            if (teamId == null || (teamId != null && AppState.PartyTeamId != teamId))
                            {
                                return false;
                            }
                            return true;
                        }
                        return true;
                        });
                    entityList = teamList.ToArray();
                }
                else
                {
                    entityList = playerList.ToArray();
                }

                ulong topTotalValue = 0;

                // This is required to get the player rank value since the clipper may not process them
                int entityIdx = 0;
                foreach (var entity in entityList)
                {
                    if (entityIdx == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.Value.TotalDamage;
                    }

                    if (AppState.PlayerUUID != 0 && AppState.PlayerUUID == entity.Value.UUID)
                    {
                        AppState.PlayerMeterPlacement = entityIdx + 1;
                        AppState.PlayerTotalMeterValue = entity.Value.TotalDamage;
                        AppState.PlayerMeterValuePerSecond = entity.Value.DamageStats.ValuePerSecond;

                        // We can exit the loop now since we don't need anything else
                        break;
                    }
                    entityIdx++;
                }

                ImGuiListClipper clipper = new();
                clipper.Begin(entityList.Count());
                while(clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var player = entityList[i];

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
                        // TotalDamage is the player only total, TotalNpcDamage is only for monster's totals
                        ulong totalEncounterDamage = activeEncounter.TotalDamage;

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
                        string activePerSecond = "";
                        if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
                        {
                            activePerSecond = $"[{Utils.NumberToShorthand(entity.DamageStats.ValuePerSecondActive)}] ";
                        }
                        string dps_format = $"{Utils.NumberToShorthand(entity.TotalDamage)} {activePerSecond}({Utils.NumberToShorthand(entity.DamageStats.ValuePerSecond)}) {contribution.ToString("F0").PadLeft(3, ' ')}%"; // Format: TotalDamage (DPS) Contribution%
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

                        StringBuilder nameFormat = new();
                        nameFormat.Append(name);

                        if (Settings.Instance.ShowSubProfessionNameInMeters)
                        {
                            nameFormat.Append($"-{profession}");
                        }

                        if (Settings.Instance.ShowAbilityScoreInMeters && Settings.Instance.ShowSeasonStrengthInMeters)
                        {
                            nameFormat.Append($" ({entity.AbilityScore}+{entity.SeasonStrength})");
                        }
                        else if (Settings.Instance.ShowAbilityScoreInMeters)
                        {
                            nameFormat.Append($" ({entity.AbilityScore})");
                        }
                        else if (Settings.Instance.ShowSeasonStrengthInMeters)
                        {
                            nameFormat.Append($" ({entity.SeasonStrength})");
                        }

                        ImGui.SetCursorPos(startPoint);
                        if (SelectableWithHintImage($" {(i + 1).ToString().PadLeft((entityList.Count() < 101 ? 2 : 3), '0')}.", $"{nameFormat}##DpsEntry_{i}", dps_format, entity.ProfessionId))
                        //if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##DpsEntry_{i}", dps_format))
                        //if (ImGui.Selectable($"{name}-{profession} ({entity.AbilityScore}) [{entity.UID.ToString()}] ({entity.TotalDamage})##DpsEntry_{i}"))
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
