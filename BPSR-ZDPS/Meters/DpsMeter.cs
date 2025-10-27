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
    public class DpsMeter : MeterBase
    {
        //static ImDrawListSplitter renderSplitter = new ImDrawListSplitter(); // Used for splitting the rendering pipeline to make overlays easier

        public DpsMeter()
        {
            Name = "DPS";
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

        bool SelectableWithHintImage(string number, string name, string value, int profession)
        {
            var startPoint = ImGui.GetCursorPos();

            ImGui.AlignTextToFramePadding();

            int texSize = 22;
            float offset = ImGui.CalcTextSize(number).X + (ImGui.GetStyle().ItemSpacing.X * 2) + (texSize + 2);

            ImGui.SetCursorPosX(offset);
            bool ret = ImGui.Selectable(name, false, ImGuiSelectableFlags.SpanAllColumns);
            ImGui.SameLine();

            ImGui.SetCursorPos(startPoint);

            ImGui.Text(number);
            ImGui.SameLine();

            var tex = ImageHelper.GetTextureByKey($"Profession_{profession}");
            
            if (tex == null)
            {
                ImGui.Dummy(new Vector2(texSize, texSize));
            }
            else
            {
                var roleColor = Professions.RoleTypeColors(Professions.GetRoleFromBaseProfessionId(profession));

                if (Settings.Instance.ColorClassIconsByRole)
                {
                    ImGui.ImageWithBg((ImTextureRef)tex, new Vector2(texSize, texSize), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), roleColor);
                }
                else
                {
                    ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                }
            }
            
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0.0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(value.Remove(value.Length - 1)).X));
            ImGui.Text(value);

            return ret;
        }

        public override void Draw(MainWindow mainWindow)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, ImGui.GetStyle().FramePadding.Y));

            if (ImGui.BeginListBox("##DPSMeterList", new Vector2(-1, -1)))
            {
                ImGui.PopStyleVar();

                // Call .ToList() to create a copy of the data in memory as it might change
                var playerList = EncounterManager.Current?.Entities.Where(x => x.EntityType == Zproto.EEntityType.EntChar).OrderByDescending(x => x.TotalDamage).ToList();

                ulong topTotalValue = 0;

                for (int i = 0; i < playerList?.Count(); i++)
                {
                    var entity = playerList.ElementAt(i);

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
                    if (AppState.PlayerUID != 0 && AppState.PlayerUID == (long)entity.UID)
                    {
                        AppState.PlayerMeterPlacement = i + 1;
                        AppState.PlayerTotalMeterValue = (long)entity.TotalDamage;
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
                    if (EncounterManager.Current.TotalDamage != 0)
                    {
                        contribution = Math.Round(((double)entity.TotalDamage / (double)EncounterManager.Current.TotalDamage) * 100, 0);

                        if (Settings.Instance.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalDamage / (double)topTotalValue) * 100, 0);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string dps_format = $"{Utils.NumberToShorthand(entity.TotalDamage)} ({Utils.NumberToShorthand(entity.DamageStats.ValuePerSecond)}) {contribution.ToString().PadLeft(3, ' ')}%%"; // Format: TotalDamage (DPS) Contribution%
                    var startPoint = ImGui.GetCursorPos();
                    // ImGui.GetTextLineHeightWithSpacing();

                    ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f);

                    // TODO: Make progress bar fill the entire line just like how the Selectable already is (as seen with hover state)

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

                    ImGui.SetCursorPos(startPoint);
                    if (SelectableWithHintImage($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}.", $"{name}-{profession} ({entity.AbilityScore})##DpsEntry_{i}", dps_format, entity.ProfessionId))
                    //if (SelectableWithHint($" {(i + 1).ToString().PadLeft((playerList.Count() < 101 ? 2 : 3), '0')}. {name}-{profession} ({entity.AbilityScore})##DpsEntry_{i}", dps_format))
                    //if (ImGui.Selectable($"{name}-{profession} ({entity.AbilityScore}) [{entity.UID.ToString()}] ({entity.TotalDamage})##DpsEntry_{i}"))
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
