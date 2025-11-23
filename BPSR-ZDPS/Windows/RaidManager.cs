using BPSR_ZDPS.DataTypes;
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
        public static bool IsTopMost = false;
        public static bool CollapseToContentOnly = false;

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;

        static Dictionary<long, List<TrackedSkill>> TrackedEntities = new();
        static Dictionary<long, TrackedSkill> TrackedSkills = new();

        static string EntityNameFilter = "";
        static TrackedSkill? SelectedSkill = null;
        static int SelectedSkillCooldown;
        static string SkillCastConditionValue = "";
        static KeyValuePair<long, EntityCacheLine>[]? EntityFilterMatches;
        static KeyValuePair<string, DataTypes.Skill>[]? SkillFilterMatches;

        static float EntityFilterSectionHeight = 0.0f;
        static float SkillFilterSectionHeight = 0.0f;

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            InitializeBindings();
            ImGui.PopID();
        }

        public static void InitializeBindings()
        {
            if (HasInitBindings == false)
            {
                HasInitBindings = true;
                EncounterManager.EncounterStart += RaidManager_EncounterStart;
                EncounterManager.EncounterEnd += RaidManager_EncounterEnd;
            }
        }

        private static void RaidManager_EncounterEnd(EventArgs e)
        {
            foreach (var trackedEntity in TrackedEntities)
            {
                EncounterManager.Current.GetOrCreateEntity(trackedEntity.Key).RemoveEventHandlers();
            }
        }

        private static void RaidManager_EncounterStart(EventArgs e)
        {
            foreach (var trackedEntity in TrackedEntities)
            {
                EncounterManager.Current.GetOrCreateEntity(trackedEntity.Key).SkillActivated += RaidManager_Entity_SkillActivated;
            }
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 240), new Vector2(ImGui.GETFLTMAX()));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.Begin($"Raid Manager{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking))
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

                DrawMenuBar();

                ImGui.Text("Cooldown Priority Tracker");
                // Select a list of entities to track their casts
                // When they cast a specific skill, begin tracking the cooldown time for it
                // Indicate they are on cooldown and have the next entity in priority ready to go
                
                ImGui.PushStyleVarX(ImGuiStyleVar.FramePadding, 4);
                ImGui.PushStyleVarY(ImGuiStyleVar.FramePadding, 1);
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(37 / 255f, 37 / 255f, 38 / 255f, 1.0f)));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                //ImGui.BeginChild("ConditionListBoxChild", new Vector2(0, 140), ImGuiChildFlags.FrameStyle);
                float listBoxHeight = ImGui.GetContentRegionAvail().Y - EntityFilterSectionHeight - SkillFilterSectionHeight - ImGui.GetStyle().ItemSpacing.Y;
                if (ImGui.BeginListBox("##ConditionsListBox", new Vector2(-1, listBoxHeight)))
                {
                    ImGui.PopStyleVar();
                    int trackedEntityIdx = 0;
                    foreach (var trackedEntity in TrackedEntities)
                    {
                        ImGui.Text($"{trackedEntityIdx + 1}.");
                        ImGui.SameLine();
                        ImGui.Text($"{EntityCache.Instance.Cache.Lines[trackedEntity.Key]?.Name}");
                        ImGui.SameLine();

                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ((20 * 4) + ImGui.GetStyle().ItemSpacing.X));

                        ImGui.BeginDisabled(trackedEntityIdx == 0);
                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (ImGui.Button($"{FASIcons.ChevronUp}##MoveUpBtn_{trackedEntityIdx}"))
                        {

                        }
                        ImGui.PopFont();
                        ImGui.EndDisabled();

                        ImGui.SameLine();
                        ImGui.BeginDisabled(trackedEntityIdx == TrackedEntities.Count - 1);
                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (ImGui.Button($"{FASIcons.ChevronDown}##MoveDownBtn_{trackedEntityIdx}"))
                        {

                        }
                        ImGui.PopFont();
                        ImGui.EndDisabled();

                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{trackedEntityIdx}"))
                        {

                        }
                        ImGui.PopFont();
                        ImGui.PopStyleColor();
                        ImGui.Indent();
                        float indentOffset = ImGui.GetCursorPosX();
                        foreach (var trackedSkill in trackedEntity.Value)
                        {
                            if (trackedSkill.ExpectedEndTime != null)
                            {
                                var remainingTime = trackedSkill.GetTimeRemaining();
                                if (remainingTime.TotalMilliseconds > 0)
                                {
                                    float textAlignment = 0.50f;
                                    var cursorPos = ImGui.GetCursorPos();
                                    string displayText = $"{trackedSkill.SkillName} ({remainingTime.ToString(@"hh\:mm\:ss\.ff")})";
                                    var textSize = ImGui.CalcTextSize(displayText);
                                    float progressBarWidth = ImGui.GetContentRegionAvail().X - indentOffset;
                                    float labelX = cursorPos.X + (progressBarWidth - textSize.X) * textAlignment;
                                    float remainingPct = (float)Math.Round(remainingTime.TotalMilliseconds / trackedSkill.SkillCooldownDefined, 4);
                                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Colors.DarkRed);
                                    ImGui.ProgressBar(remainingPct, new Vector2(progressBarWidth, 18), "");
                                    ImGui.PopStyleColor();
                                    ImGui.SetCursorPos(new Vector2(labelX, cursorPos.Y + (ImGui.GetItemRectSize().Y - textSize.Y) * textAlignment));
                                    ImGui.Text(displayText);
                                    ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + ImGui.GetItemRectSize().Y));
                                }
                            }
                        }
                        ImGui.Unindent();

                        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(78 / 255f, 78 / 255f, 78 / 255f, 1.0f));
                        ImGui.Separator();
                        ImGui.PopStyleColor();

                        trackedEntityIdx++;
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

                if (!CollapseToContentOnly)
                {

                    ImGui.Separator();

                    var entityFilterStartPos = ImGui.GetCursorPosY();
                    if (ImGui.CollapsingHeader("Add Tracked Entity"))
                    {
                        ImGui.Indent();

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
                                        bool isSelected = TrackedEntities.ContainsKey(match.Value.UUID);

                                        if (isSelected)
                                        {
                                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                            if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{matchIdx}", new Vector2(30, 30)))
                                            {
                                                TrackedEntities.Remove(match.Value.UUID);
                                                EncounterManager.Current.GetOrCreateEntity(match.Value.UUID).SkillActivated -= RaidManager_Entity_SkillActivated;
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
                                                TrackedEntities.Add(match.Value.UUID, new List<TrackedSkill>());
                                                EncounterManager.Current.GetOrCreateEntity(match.Value.UUID).SkillActivated += RaidManager_Entity_SkillActivated;
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

                        ImGui.Unindent();
                    }
                    EntityFilterSectionHeight = ImGui.GetCursorPosY() - entityFilterStartPos;

                    var skillFilterStartPos = ImGui.GetCursorPosY();
                    if (ImGui.CollapsingHeader("Add Tracked Skill"))
                    {
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
                                        // We're using the Key instead of item.Value.Id as overrides could add entirely new entries and may not redefine the Id value
                                        int skillId = int.Parse(item.Key);

                                        if (TrackedSkills.TryGetValue(skillId, out _))
                                        {
                                            ImGui.AlignTextToFramePadding();
                                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                            if (ImGui.Button($"{FASIcons.Minus}##SkillBtn_{skillMatchIdx}", new Vector2(30, ImGui.GetFontSize() * 2.5f)))
                                            {
                                                TrackedSkills.Remove(skillId);
                                                foreach (var trackedEntity in TrackedEntities)
                                                {
                                                    RemoveTrackedSkillFromEntity(trackedEntity.Key, skillId);
                                                }
                                            }
                                            ImGui.PopFont();
                                            ImGui.PopStyleColor();
                                            ImGui.SameLine();
                                        }

                                        bool isSelected = SelectedSkill != null && SelectedSkill.SkillId == skillId;
                                        ImGuiSelectableFlags selectableFlags = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                                        if (ImGui.Selectable($"Skill Id: {item.Key}\nSkill Name: {item.Value.Name}##SkillFilterItem_{skillMatchIdx}", isSelected, selectableFlags))
                                        {
                                            SelectedSkill = new TrackedSkill()
                                            {
                                                SkillId = skillId,
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
                        if (ImGui.Button("Add Skill To Tracker"))
                        {
                            SelectedSkill.SkillCooldownDefined = SelectedSkillCooldown;
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

                    }
                    SkillFilterSectionHeight = ImGui.GetCursorPosY() - skillFilterStartPos;

                    // Debug buttons
                    if (false)
                    {
                        if (ImGui.Button("Bind All Test"))
                        {
                            AddEventHandlersToEntities();
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Unbind All Test"))
                        {
                            foreach (var trackedEntity in TrackedEntities)
                            {
                                EncounterManager.Current.GetOrCreateEntity(trackedEntity.Key).RemoveEventHandlers();
                            }
                        }
                    }
                }
                else
                {
                    EntityFilterSectionHeight = 0.0f;
                    SkillFilterSectionHeight = 0.0f;
                }
                ImGui.End();
            }

            ImGui.PopID();
        }

        static float MenuBarButtonWidth = 0.0f;
        public static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                MenuBarSize = ImGui.GetWindowSize();

                ImGui.Text("Raid Manager");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, IsTopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}"))
                {
                    if (!IsTopMost)
                    {
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(Settings.Instance.WindowOpacity);
                        IsTopMost = true;
                    }
                    else
                    {
                        Utils.UnsetWindowTopmost();
                        Utils.SetWindowOpacity(1.0f);
                        IsTopMost = false;
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Pin Window As Top Most");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, CollapseToContentOnly ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{(CollapseToContentOnly ? FASIcons.AnglesDown : FASIcons.AnglesUp)}"))
                {
                    CollapseToContentOnly = !CollapseToContentOnly;
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                if (CollapseToContentOnly)
                {
                    ImGui.SetItemTooltip("Expand To Full Options");
                }
                else
                {
                    
                    ImGui.SetItemTooltip("Collapse To Content Only");
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"X"))
                {
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }

        public static void AddEventHandlersToEntities()
        {
            // TODO: Loop through each SelectedEntityUuids and bind the event handler for skill activation
            // This should be executed whenever a new Encounter is started so that it continues to update as new ones occur
            // As Encounters end, handlers will need to be removed (this may be handled on the Entity side already)
            foreach (var trackedEntity in TrackedEntities)
            {
                EncounterManager.Current.GetOrCreateEntity(trackedEntity.Key).SkillActivated += RaidManager_Entity_SkillActivated;
            }
        }

        private static void RaidManager_Entity_SkillActivated(object sender, SkillActivatedEventArgs e)
        {
            if (TrackedSkills.ContainsKey(e.SkillId))
            {
                if (TrackedEntities.TryGetValue(e.CasterUuid, out var trackedSkills))
                {
                    // Try to update an existing tracker before adding a new one to the entity
                    var foundEntry = trackedSkills.Where(x => x.SkillId == e.SkillId);
                    if (foundEntry.Any())
                    {
                        foundEntry.First().SetActivationTime(e.ActivationDateTime);
                    }
                    else
                    {
                        var newSkill = new TrackedSkill()
                        {
                            SkillId = e.SkillId,
                            SkillName = TrackedSkills[e.SkillId].SkillName,
                            SkillCooldownDefined = TrackedSkills[e.SkillId].SkillCooldownDefined
                        };
                        newSkill.SetActivationTime(e.ActivationDateTime);
                        trackedSkills.Add(newSkill);
                    }
                }
            }
        }

        private static void RemoveTrackedSkillFromEntity(long trackedEntityUuid, int skillId)
        {
            if (TrackedEntities.TryGetValue(trackedEntityUuid, out var trackedSkills))
            {
                trackedSkills.RemoveAll(x => x.SkillId == skillId);
            }
        }
    }

    public class TrackedSkill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int SkillCooldownDefined { get; set; }
        public DateTime ActivationTime { get; private set; }
        public DateTime? ExpectedEndTime { get; private set; } = null;

        public void SetActivationTime(DateTime activationTime)
        {
            ActivationTime = activationTime;
            ExpectedEndTime = ActivationTime.AddMilliseconds(SkillCooldownDefined);
        }

        public TimeSpan GetTimeRemaining()
        {
            if (ExpectedEndTime == null)
            {
                return new TimeSpan();
            }
            else
            {
                return ExpectedEndTime.Value.Subtract(DateTime.Now);
            }
        }
    }
}
