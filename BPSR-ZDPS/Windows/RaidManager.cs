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
    public static class RaidManager
    {
        public const string LAYER = "RaidManagerWindowLayer";
        public static string TITLE_ID = "###RaidManagerWindow";
        public static bool IsOpened = false;

        static int RunOnceDelayed = 0;

        static string EntityNameFilter = "";
        static List<long> SelectedEntityUuids = new();
        static Dictionary<long, TrackedSkill> TrackedSkills = new();
        static TrackedSkill? SelectedSkill = null;
        static int SelectedSkillCooldown;
        static string SkillCastConditionValue = "";
        static KeyValuePair<long, EntityCacheLine>[]? EntityFilterMatches;
        static KeyValuePair<string, DataTypes.Skill>[]? SkillFilterMatches;

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.Begin($"Raid Manager{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();
                }

                ImGui.Text("Cooldown Priority Tracker");
                // Select a list of entities to track their casts
                // When they cast a specific skill, begin tracking the cooldown time for it
                // Indicate they are on cooldown and have the next entity in priority ready to go
                
                ImGui.PushStyleVarX(ImGuiStyleVar.FramePadding, 4);
                ImGui.PushStyleVarY(ImGuiStyleVar.FramePadding, 1);
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(37 / 255f, 37 / 255f, 38 / 255f, 1.0f)));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                //ImGui.BeginChild("ConditionListBoxChild", new Vector2(0, 140), ImGuiChildFlags.FrameStyle);
                if (ImGui.BeginListBox("##ConditionsListBox", new Vector2(-1, 120)))
                {
                    ImGui.PopStyleVar();
                    for (int i = 0; i < SelectedEntityUuids.Count; i++)
                    {
                        var selectedEntity = SelectedEntityUuids[i];
                        
                        ImGui.Text($"{i + 1}.");
                        ImGui.SameLine();
                        ImGui.Text($"{EntityCache.Instance.Cache.Lines[selectedEntity]?.Name}");
                        ImGui.SameLine();

                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ((20 * 4) + ImGui.GetStyle().ItemSpacing.X));

                        ImGui.BeginDisabled(i == 0);
                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (ImGui.Button($"{FASIcons.ChevronUp}##MoveUpBtn_{i}"))
                        {

                        }
                        ImGui.PopFont();
                        ImGui.EndDisabled();

                        ImGui.SameLine();
                        ImGui.BeginDisabled(i == SelectedEntityUuids.Count - 1);
                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (ImGui.Button($"{FASIcons.ChevronDown}##MoveDownBtn_{i}"))
                        {

                        }
                        ImGui.PopFont();
                        ImGui.EndDisabled();

                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{i}"))
                        {

                        }
                        ImGui.PopFont();
                        ImGui.PopStyleColor();
                        ImGui.Indent();
                        float indentOffset = ImGui.GetCursorPosX();
                        foreach (var trackedSkill in TrackedSkills)
                        {
                            float textAlignment = 0.50f;
                            var cursorPos = ImGui.GetCursorPos();
                            string displayText = $"{trackedSkill.Value.SkillName} (-05:00)";
                            var textSize = ImGui.CalcTextSize(displayText);
                            float progressBarWidth = ImGui.GetContentRegionAvail().X - indentOffset;
                            float labelX = cursorPos.X + (progressBarWidth - textSize.X) * textAlignment;
                            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Colors.DarkRed);
                            ImGui.ProgressBar(1.00f, new Vector2(progressBarWidth, 18), "");
                            ImGui.PopStyleColor();
                            ImGui.SetCursorPos(new Vector2(labelX, cursorPos.Y + (ImGui.GetItemRectSize().Y - textSize.Y) * textAlignment));
                            ImGui.Text(displayText);
                            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + ImGui.GetItemRectSize().Y));
                        }
                        ImGui.Unindent();

                        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(78 / 255f, 78 / 255f, 78 / 255f, 1.0f));
                        ImGui.Separator();
                        ImGui.PopStyleColor();
                    }

                    ImGui.EndListBox();
                }
                else
                {
                    ImGui.PopStyleVar();
                }
                //ImGui.EndChild();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);

                bool useTableVersion = false;
                if (useTableVersion)
                {
                    // Display table of tracked entities with options to MoveUp, MoveDown, Remove
                    ImGuiTableFlags table_flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuter;
                    if (ImGui.BeginTable("CooldownPriorityListTable", 3, table_flags, new Vector2(-1, 120)))
                    {
                        ImGui.TableSetupColumn("##Number", ImGuiTableColumnFlags.DefaultHide, 0f, 0);
                        ImGui.TableSetupColumn("##Name", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide, 1f, 1);
                        ImGui.TableSetupColumn("##ActionButtons", ImGuiTableColumnFlags.DefaultHide, 0f, 2);

                        for (int i = 0; i < SelectedEntityUuids.Count; i++)
                        {
                            var selectedEntity = SelectedEntityUuids[i];

                            ImGui.TableNextColumn();
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text($"{i + 1}");

                            ImGui.TableNextColumn();
                            ImGui.Text(EntityCache.Instance.Cache.Lines[selectedEntity]?.Name);

                            ImGui.TableNextColumn();

                            ImGui.BeginDisabled(i == 0);
                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                            if (ImGui.Button($"{FASIcons.ChevronUp}##MoveUpBtn_{i}"))
                            {

                            }
                            ImGui.PopFont();
                            ImGui.EndDisabled();

                            ImGui.SameLine();
                            ImGui.BeginDisabled(i == SelectedEntityUuids.Count - 1);
                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                            if (ImGui.Button($"{FASIcons.ChevronDown}##MoveDownBtn_{i}"))
                            {

                            }
                            ImGui.PopFont();
                            ImGui.EndDisabled();

                            ImGui.SameLine();
                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                            if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{i}"))
                            {

                            }
                            ImGui.PopFont();
                            ImGui.PopStyleColor();
                        }

                        ImGui.EndTable();
                    }
                }

                ImGui.Separator();

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Entity Filter: ");
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputText("##EntityFilterText", ref EntityNameFilter, 64))
                {
                    EntityFilterMatches = EntityCache.Instance.Cache.Lines.AsValueEnumerable().Where(x => x.Value.Name != null && x.Value.Name.Contains(EntityNameFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                // Require at least 3 characters to perform our search to maintain performance against large lists
                if (ImGui.BeginListBox("##FilteredEntitiesListBox", new Vector2(ImGui.GetContentRegionAvail().X, 120)))
                {
                    if (EntityNameFilter.Length > 2)
                    {
                        if (EntityFilterMatches != null && EntityFilterMatches.Any())
                        {
                            long matchIdx = 0;
                            foreach (var match in EntityFilterMatches)
                            {
                                bool isSelected = SelectedEntityUuids.Contains(match.Value.UUID);

                                if (isSelected)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                                    ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                    if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{matchIdx}", new Vector2(30, 30)))
                                    {
                                        SelectedEntityUuids.Remove(match.Value.UUID);
                                    }
                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green_Transparent);
                                    ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                    if (ImGui.Button($"{FASIcons.Plus}##AddBtn_{matchIdx}", new Vector2(30, 30)))
                                    {
                                        SelectedEntityUuids.Add(match.Value.UUID);
                                    }
                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();
                                }

                                ImGui.SameLine();
                                ImGui.Text($"{match.Value.Name} [U:{match.Value.UID}] {{UU:{match.Value.UUID}}}");

                                matchIdx++;
                            }
                        }
                    }

                    ImGui.EndListBox();
                }


                ImGui.SeparatorText("Skill Tracking");
                ImGui.Indent();

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Skill Cast Filter:");
                ImGui.SameLine();
                // If starting with a number, perform a Skill ID lookup, if it's a character, do a Skill Name lookup
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##SkillCastCondition", ref SkillCastConditionValue, 64))
                {
                    if (SkillCastConditionValue.Length > 0)
                    {
                        bool isNum = Char.IsNumber(SkillCastConditionValue[0]);
                        SkillFilterMatches = HelperMethods.DataTables.Skills.Data.AsValueEnumerable().Where(x => isNum ? x.Key.Contains(SkillCastConditionValue) : x.Value.Name.Contains(SkillCastConditionValue, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }
                    else
                    {
                        SkillFilterMatches = null;
                    }
                }

                if (ImGui.BeginListBox("##SkillFilterList", new Vector2(-1, 120)))
                {
                    if (SkillCastConditionValue.Length > 0)
                    {
                        if (SkillFilterMatches != null && SkillFilterMatches.Any())
                        {
                            int skillMatchIdx = 0;
                            foreach (var item in SkillFilterMatches)
                            {
                                if (TrackedSkills.TryGetValue(item.Value.Id,out _))
                                {
                                    ImGui.AlignTextToFramePadding();
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                                    ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                    ImGui.Button($"{FASIcons.Minus}##SkillBtn_{skillMatchIdx}", new Vector2(30, ImGui.GetFontSize() * 2.5f));
                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();
                                    ImGui.SameLine();
                                }
                                
                                bool isSelected = SelectedSkill != null && SelectedSkill.SkillId == item.Value.Id;
                                ImGuiSelectableFlags selectableFlags = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                                if (ImGui.Selectable($"Skill Id: {item.Key}\nSkill Name: {item.Value.Name}##SkillFilterItem_{skillMatchIdx}", isSelected, selectableFlags))
                                {
                                    SelectedSkill = new TrackedSkill()
                                    {
                                        SkillId = item.Value.Id,
                                        SkillName = item.Value.Name
                                    };
                                }

                                skillMatchIdx++;
                            }
                        }
                    }
                    ImGui.EndListBox();
                }

                // Allow manually setting the "cooldown" time for the skill instead of using the real one in case the user needs something different
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Cooldown Time (MS):");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputInt("##SkillCastConditionSkillCooldownInt", ref SelectedSkillCooldown, 1, 1, ImGuiInputTextFlags.None);

                ImGui.BeginDisabled(SelectedSkill == null);
                if(ImGui.Button("Add Skill To Tracker"))
                {
                    SelectedSkill.SkillCooldown = SelectedSkillCooldown;
                    TrackedSkills.Add(SelectedSkill.SkillId, SelectedSkill);
                    SelectedSkill = null;
                    SelectedSkillCooldown = 0;
                }
                ImGui.EndDisabled();
                if (SelectedSkill == null)
                {
                    ImGui.SetItemTooltip("A skill must first be selected from above before it can be added.");
                }    

                ImGui.Unindent();

                ImGui.End();
            }

            ImGui.PopID();
        }
    }

    public class TrackedSkill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int SkillCooldown { get; set; }
    }
}
