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
    public class TakenMeter : MeterBase
    {
        public TakenMeter()
        {
            Name = "NPC Taken";
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
            if (ImGui.BeginListBox("##TakenMeterList", new Vector2(-1, -1)))
            {
                // Call .ToList() to create a copy of the data in memory as it might change
                var playerList = EncounterManager.Current?.Entities.Where(x => x.EntityType == Zproto.EEntityType.EntMonster).OrderByDescending(x => x.TotalTakenDamage).ToList();

                ulong topTotalValue = 0;

                for (int i = 0; i < playerList?.Count(); i++)
                {
                    var entity = playerList.ElementAt(i);

                    if (i == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.TotalTakenDamage;
                    }

                    string name = "Unknown";
                    if (!string.IsNullOrEmpty(entity.Name))
                    {
                        name = entity.Name;
                    }

                    double contribution = 0.0;
                    double contributionProgressBar = 0.0;
                    if (EncounterManager.Current.TotalNpcTakenDamage != 0)
                    {
                        contribution = Math.Round(((double)entity.TotalTakenDamage / (double)EncounterManager.Current.TotalNpcTakenDamage) * 100, 0);

                        if (Settings.Instance.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalTakenDamage / (double)topTotalValue) * 100, 0);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string dps_format = $"{Utils.NumberToShorthand((long)entity.TotalTakenDamage)} ({entity.TakenStats.ValuePerSecond}) {contribution.ToString().PadLeft(3, ' ')}%%"; // Format: TotalDamage (DPS) Contribution%
                    var startPoint = ImGui.GetCursorPos();
                    // ImGui.GetTextLineHeightWithSpacing();

                    ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f);

                    ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##TakenEntryContribution_{i}");

                    ImGui.SetCursorPos(startPoint);
                    if (SelectableWithHint($"{name} [{entity.UID.ToString()}]##TakenEntry_{i}", dps_format))
                    {
                        mainWindow.entityInspector = new EntityInspector();
                        mainWindow.entityInspector.LoadEntity(entity);
                        mainWindow.entityInspector.Open();
                    }

                    ImGui.PopFont();
                }

                ImGui.EndListBox();
            }
        }
    }
}
