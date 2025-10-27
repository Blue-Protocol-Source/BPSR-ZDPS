using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Meters
{
    public class TankingMeter : MeterBase
    {

        public TankingMeter()
        {
            Name = "Tanking";
        }

        bool SelectableWithHint(string label, string hint)
        {
            ImGui.AlignTextToFramePadding(); // This makes the entries about 1/3 larger but keeps it nicely centered
            bool ret = ImGui.Selectable(label);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0.0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(hint.Remove(hint.Length - 1)).X));
            ImGui.Text(hint);
            return ret;
        }

        public override void Draw(MainWindow mainWindow)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, ImGui.GetStyle().FramePadding.Y));

            if (ImGui.BeginListBox("##TankingMeterList", new Vector2(-1, -1)))
            {
                ImGui.PopStyleVar();

                // Call .ToList() to create a copy of the data in memory as it might change
                var playerList = EncounterManager.Current?.Entities.Where(x => x.EntityType == Zproto.EEntityType.EntChar && x.TotalTakenDamage > 0).OrderByDescending(x => x.TotalTakenDamage).ToList();

                ulong topTotalValue = 0;

                for (int i = 0; i < playerList?.Count(); i++)
                {
                    var entity = playerList.ElementAt(i);

                    if (i == 0 && AppState.NormalizeMeterContributions)
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
                        AppState.PlayerTotalMeterValue = (long)entity.TotalTakenDamage;
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
                    if (EncounterManager.Current.TotalHealing != 0)
                    {
                        contribution = Math.Round(((double)entity.TotalTakenDamage / (double)EncounterManager.Current.TotalTakenDamage) * 100, 0);

                        if (AppState.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalTakenDamage / (double)topTotalValue) * 100, 0);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string totalTaken = Utils.NumberToShorthand((long)entity.TotalTakenDamage);
                    string totalHps = entity.TakenStats.ValuePerSecond.ToString();
                    string dps_format = $"{totalTaken} ({totalHps}) {contribution.ToString().PadLeft(3, ' ')}%%";
                    var startPoint = ImGui.GetCursorPos();

                    ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f);

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Professions.ProfessionColors(profession));
                    ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##TpsEntryContribution_{i}");
                    ImGui.PopStyleColor();

                    ImGui.SetCursorPos(startPoint);
                    if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##TpsEntry_{i}", dps_format))
                    {
                        mainWindow.entityInspector = new EntityInspector();
                        mainWindow.entityInspector.LoadEntity(entity);
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
