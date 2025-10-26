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
    public class HealingMeter : MeterBase
    {

        public HealingMeter()
        {
            Name = "Healing";
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

            if (ImGui.BeginListBox("##HealingMeterList", new Vector2(-1, -1)))
            {
                ImGui.PopStyleVar();

                // Call .ToList() to create a copy of the data in memory as it might change
                var playerList = EncounterManager.Current?.Entities.Where(x => x.EntityType == Zproto.EEntityType.EntChar && x.TotalHealing > 0).OrderByDescending(x => x.TotalHealing).ToList();

                ulong topTotalValue = 0;

                for (int i = 0; i < playerList?.Count(); i++)
                {
                    var entity = playerList.ElementAt(i);

                    if (i == 0 && AppState.NormalizeMeterContributions)
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
                    if (AppState.PlayerUID != 0 && AppState.PlayerUID == (long)entity.UID)
                    {
                        AppState.PlayerMeterPlacement = i + 1;
                        AppState.PlayerTotalMeterValue = (long)entity.TotalHealing;
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
                        contribution = Math.Round(((double)entity.TotalHealing / (double)EncounterManager.Current.TotalHealing) * 100, 0);

                        if (AppState.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalHealing / (double)topTotalValue) * 100, 0);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string totalHealing = HelperMethods.NumberToShorthand((long)entity.TotalHealing);
                    string totalHps = entity.HealingStats.ValuePerSecond.ToString();
                    string dps_format = $"{totalHealing} ({totalHps}) {contribution.ToString().PadLeft(3, ' ')}%%";
                    var startPoint = ImGui.GetCursorPos();

                    ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f);

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, HelperMethods.ProfessionColors(profession));
                    ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##HpsEntryContribution_{i}");
                    ImGui.PopStyleColor();

                    ImGui.SetCursorPos(startPoint);
                    if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##HpsEntry_{i}", dps_format))
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
