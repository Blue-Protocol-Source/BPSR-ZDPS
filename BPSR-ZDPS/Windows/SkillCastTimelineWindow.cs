using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Windows
{
    public static class SkillCastTimelineWindow
    {
        public const string LAYER = "SkillCastTimelineWindowLayer";
        public static string TITLE_ID = "###SkillCastTimelineWindow";
        public static string TITLE = "Skill Cast Timeline Settings";

        public static bool IsOpened = false;

        static bool ShouldTrackOpenState = false;

        static int RunOnceDelayed = 0;
        static bool HasInitBindings = false;
        static int LastPinnedOpacity = 100;
        static bool IsPinned = false;

        static bool HasBoundEvents = false;

        static ImGuiWindowClassPtr SkillCastTimelineDisplayClass = ImGui.ImGuiWindowClass();

        static bool IsEditMode = false;

        static ConcurrentQueue<TimelineSkillEntry> TimelineEntries = new();

        static bool HadTransparentBackground = false;
        static int LastSetTimelineOpacity = 100;

        static bool RecreateWindowOnce = false;

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
                EncounterManager.EncounterStart += EncounterManager_EncounterStart;
                EncounterManager.EncounterEndFinal += EncounterManager_EncounterEndFinal;

                SkillCastTimelineDisplayClass.ClassId = ImGuiP.ImHashStr("SkillCastTimelineDisplayClass");
                SkillCastTimelineDisplayClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.TopMost;// | ImGuiViewportFlags.NoTaskBarIcon;
                if (Settings.Instance.WindowSettings.SkillCastTimeline.HideTimelineInTaskBar)
                {
                    SkillCastTimelineDisplayClass.ViewportFlagsOverrideSet |= ImGuiViewportFlags.NoTaskBarIcon;
                }

                BindCurrentEncounterEvents();
            }
        }

        private static void EncounterManager_EncounterEndFinal(EncounterEndFinalData e)
        {
            HasBoundEvents = false;
        }

        private static void EncounterManager_EncounterStart(EventArgs e)
        {
            BindCurrentEncounterEvents();
        }

        private static void BindCurrentEncounterEvents()
        {
            if (!HasBoundEvents)
            {
                HasBoundEvents = true;
            }

            EncounterManager.Current.SkillActivated -= Encounter_SkillActivated;
            EncounterManager.Current.SkillActivated += Encounter_SkillActivated;
        }

        private static void Encounter_SkillActivated(object sender, SkillActivatedEventArgs e)
        {
            if (e.CasterUuid != AppState.PlayerUUID)
            {
                return;
            }

            if (HelperMethods.DataTables.Skills.Data.TryGetValue(e.SkillId.ToString(), out var skill))
            {
                var newEntry = new TimelineSkillEntry()
                {
                    SkillId = e.SkillId,
                    SkillName = skill.Name,
                    StartTime = e.ActivationDateTime
                };

                string baseDir = "Skills";
                if (skill.IsImagineSlot())
                {
                    baseDir = "Skills_Imagines";
                }

                string iconPath = Path.Combine(Utils.DATA_DIR_NAME, "Images", baseDir, $"{skill.GetIconName()}.png");
                if (File.Exists(iconPath))
                {
                    newEntry.Icon = Path.Combine(baseDir, skill.GetIconName());
                }
                else
                {
                    string missingIconPath = Path.Combine(Utils.DATA_DIR_NAME, "Images", "Misc", $"skill_empty.png");
                    if (File.Exists(missingIconPath))
                    {
                        newEntry.Icon = Path.Combine("Misc", "skill_empty");
                    }
                }

                TimelineEntries.Enqueue(newEntry);
            }
        }

        public static void Draw(MainWindow mainWindow)
        {
            InitializeBindings();

            var windowSettings = Settings.Instance.WindowSettings.SkillCastTimeline;

            if (windowSettings.IsTimelineEnabled)
            {
                bool shouldShow = true;
                if (IsEditMode)
                {
                    shouldShow = true;
                }
                else if (windowSettings.TimelineOnlyShowInCombat)
                {
                    if (EncounterManager.Current.Entities.TryGetValue(AppState.PlayerUUID, out var player))
                    {
                        var attrCombatState = player.GetAttrKV("AttrCombatState") as int?;
                        if (attrCombatState != null)
                        {
                            shouldShow = attrCombatState > 0;
                        }
                        else
                        {
                            shouldShow = false;
                        }
                    }
                }

                if (shouldShow)
                {
                    DrawTimeline();
                }
                else
                {
                    HadTransparentBackground = false;
                    LastSetTimelineOpacity = 100;
                }
            }

            if (RecreateWindowOnce)
            {
                RecreateWindowOnce = false;
                windowSettings.IsTimelineEnabled = true;
                HadTransparentBackground = false;
                LastSetTimelineOpacity = 100;
            }

            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(500, 720), ImGuiCond.Appearing);

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.Begin($"{TITLE}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                ShouldTrackOpenState = true;

                if (RunOnceDelayed == 0 || RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 2)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();

                    if (windowSettings.TopMost && !IsPinned)
                    {
                        IsPinned = true;
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }
                else if (RunOnceDelayed >= 3)
                {
                    if (windowSettings.TopMost && LastPinnedOpacity != windowSettings.Opacity)
                    {
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }

                if (ImGui.Checkbox("Is Timeline Enabled", ref windowSettings.IsTimelineEnabled))
                {
                    HadTransparentBackground = false;
                    LastSetTimelineOpacity = 100;
                }

                ImGui.Checkbox("Timeline Edit Mode (Allows Movement and Resize)", ref IsEditMode);

                ImGui.Checkbox("Only Show Timeline In Combat", ref windowSettings.TimelineOnlyShowInCombat);
                ImGui.SetItemTooltip("Timeline is always shown if you are in Edit Mode.");

                if (ImGui.Checkbox("Hide Timeline In Task Bar", ref windowSettings.HideTimelineInTaskBar))
                {
                    if (windowSettings.HideTimelineInTaskBar)
                    {
                        SkillCastTimelineDisplayClass.ViewportFlagsOverrideSet |= ImGuiViewportFlags.NoTaskBarIcon;
                    }
                    else
                    {
                        SkillCastTimelineDisplayClass.ViewportFlagsOverrideSet &= ~ImGuiViewportFlags.NoTaskBarIcon;
                    }

                    if (windowSettings.IsTimelineEnabled)
                    {
                        RecreateWindowOnce = true;
                        windowSettings.IsTimelineEnabled = false;
                    }

                    HadTransparentBackground = false;
                    LastSetTimelineOpacity = 100;
                }
                ImGui.SetItemTooltip("Hiding the Timeline may prevent screen recording software like OBS from seeing the window.");

                ImGui.TextUnformatted("Timeline Speed:");
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                if (ImGui.SliderFloat("##TimelineSpeed", ref windowSettings.TimelineSpeed, 0.25f, 10.0f, ImGuiSliderFlags.AlwaysClamp))
                {
                    windowSettings.TimelineSpeed = MathF.Round(windowSettings.TimelineSpeed, 2);
                }
                ImGui.PopStyleColor(2);
                ImGui.SetItemTooltip("How many seconds it takes for a skill icon to move across the entire timeline.");

                ImGui.TextUnformatted("Icon Size:");
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SliderInt("##IconSize", ref windowSettings.TimelineIconSize, 24, 96, ImGuiSliderFlags.AlwaysClamp);
                ImGui.PopStyleColor(2);
                ImGui.SetItemTooltip("How large the icons should appear on the timeline.");

                ImGui.Checkbox("Allow Skills To Overlap", ref windowSettings.TimelineAllowOverlap);
                ImGui.SetItemTooltip("Allows skill icons to overlap instead of being directly after the previous one if they were cast very close together.");

                ImGui.BeginDisabled(windowSettings.TimelineWindowModifiedOpacity || windowSettings.TimelineUseChromaKey);
                ImGui.Checkbox("Transparent Background", ref windowSettings.TimelineTransparentBackground);
                ImGui.EndDisabled();

                ImGui.BeginDisabled(windowSettings.TimelineTransparentBackground || windowSettings.TimelineUseChromaKey);
                ImGui.Checkbox("Timeline Window Opacity", ref windowSettings.TimelineWindowModifiedOpacity);
                if (windowSettings.TimelineWindowModifiedOpacity)
                {
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                    ImGui.SliderInt("##TimelineOpacityValue", ref windowSettings.TimelineWindowOpacityValue, 20, 100);
                    ImGui.PopStyleColor(2);
                }
                ImGui.EndDisabled();

                ImGui.BeginDisabled(windowSettings.TimelineTransparentBackground || windowSettings.TimelineWindowModifiedOpacity);
                ImGui.Checkbox("Use Chroma Key", ref windowSettings.TimelineUseChromaKey);
                ImGui.SetItemTooltip("Chroma Key color for the background.");

                ImGui.ColorPicker4("##ChromaKey", ref windowSettings.TimelineWindowChromaKey);
                ImGui.EndDisabled();

                ImGui.Separator();

                if (ImGui.Button("Empty Skill Queue"))
                {
                    while (TimelineEntries.Count > 0)
                    {
                        TimelineEntries.TryDequeue(out _);
                    }
                }
                ImGui.SetItemTooltip($"Use this to clear out any skills stuck in the queue.\nCurrent Count: {TimelineEntries.Count}");

                ImGui.End();
            }

            ImGui.PopID();
        }

        public static void DrawTimeline()
        {
            var windowSettings = Settings.Instance.WindowSettings.SkillCastTimeline;

            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 100), new Vector2(ImGui.GETFLTMAX()));

            ImGui.SetNextWindowSize(new Vector2(800, 200), ImGuiCond.FirstUseEver);
            float maxHeight = windowSettings.TimelineIconSize + ImGui.GetStyle().FramePadding.Y;
            ImGui.SetNextWindowSizeConstraints(new Vector2(100, maxHeight), new Vector2(ImGui.GETFLTMAX(), maxHeight));

            if (windowSettings.TimelineSize != new Vector2())
            {
                ImGui.SetNextWindowSize(windowSettings.TimelineSize, ImGuiCond.FirstUseEver);
            }

            if (windowSettings.TimelinePosition != new Vector2())
            {
                ImGui.SetNextWindowPos(windowSettings.TimelinePosition, ImGuiCond.FirstUseEver);
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGui.SetNextWindowClass(SkillCastTimelineDisplayClass);

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (!IsEditMode)
            {
                exWindowFlags |= ImGuiWindowFlags.NoInputs;
                exWindowFlags |= ImGuiWindowFlags.NoResize;
                exWindowFlags |= ImGuiWindowFlags.NoMove;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            }

            if (windowSettings.TimelineTransparentBackground || (windowSettings.TimelineUseChromaKey && windowSettings.TimelineWindowChromaKey.W < 1.0f))
            {
                exWindowFlags |= ImGuiWindowFlags.NoBackground;
            }

            bool shouldTransparentBg = windowSettings.TimelineTransparentBackground || windowSettings.TimelineUseChromaKey;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            if (ImGui.Begin("Skill Cast Timeline##SkillCastTimelineDisplay", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoFocusOnAppearing | exWindowFlags))
            {
                if (windowSettings.TimelineWindowModifiedOpacity && LastSetTimelineOpacity != windowSettings.TimelineWindowOpacityValue)
                {
                    if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                    {
                        Utils.SetWindowOpacity(windowSettings.TimelineWindowOpacityValue * 0.01f);
                        LastSetTimelineOpacity = windowSettings.TimelineWindowOpacityValue;
                    }
                }
                else if (!windowSettings.TimelineWindowModifiedOpacity && LastSetTimelineOpacity != 100)
                {
                    if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                    {
                        Utils.SetWindowOpacity(1);
                        LastSetTimelineOpacity = 100;
                    }
                }

                if (windowSettings.TimelineTransparentBackground && !HadTransparentBackground || RecreateWindowOnce)
                {
                    if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                    {
                        HadTransparentBackground = true;
                    }
                }
                else if (!windowSettings.TimelineTransparentBackground && HadTransparentBackground)
                {
                    if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                    {
                        HadTransparentBackground = false;
                    }
                }

                if (windowSettings.TimelineUseChromaKey)
                {
                    ImGui.PushStyleColor(ImGuiCol.WindowBg, windowSettings.TimelineWindowChromaKey);
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, windowSettings.TimelineWindowChromaKey);
                }
                else if (windowSettings.TimelineTransparentBackground)
                {
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0));
                }

                ImGui.BeginChild("TimelineChildWindow", new Vector2(0, 0));

                if (IsEditMode && TimelineEntries.Count == 0)
                {
                    ImGui.TextUnformatted("Awaiting Timeline Data...");
                }

                bool removeLast = false;
                int idx = 0;
                double lastPosition = 0;
                foreach (var entry in TimelineEntries)
                {
                    var tex = ImageArchive.LoadImage(entry.Icon);
                    var texBg = ImageArchive.LoadImage(Path.Combine("Misc", "skill_bg"));
                    if (tex != null && texBg != null)
                    {
                        float texSize = windowSettings.TimelineIconSize;
                        float startX = ImGui.GetWindowWidth() - (texSize * 1);
                        double currentPosition = Lerp(startX, texSize * -2, (DateTime.Now - entry.StartTime).TotalSeconds / windowSettings.TimelineSpeed);

                        if (!windowSettings.TimelineAllowOverlap)
                        {
                            if (idx != 0 && currentPosition < lastPosition + texSize)
                            {
                                currentPosition = lastPosition + texSize;
                            }
                        }

                        bool styleChanged = false;
                        if (currentPosition <= 0)
                        {
                            double progress = currentPosition / (texSize * -1);
                            double alpha = Math.Abs(Lerp(1, 0, progress));
                            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, (float)alpha);
                            styleChanged = true;
                        }

                        if (currentPosition <= texSize * -2)
                        {
                            removeLast = true;
                        }

                        ImGui.SetCursorPosX((float)currentPosition);
                        ImGui.Image((ImTextureRef)texBg, new Vector2(texSize));
                        //ImGui.ImageWithBg((ImTextureRef)texBg, new Vector2(texSize), new Vector2(0), new Vector2(1), new Vector4(0), new Vector4(1,1,1,1));
                        ImGui.SameLine();
                        ImGui.SetCursorPosX((float)currentPosition);
                        ImGui.Image((ImTextureRef)tex, new Vector2(texSize));
                        //ImGui.ImageWithBg((ImTextureRef)tex, new Vector2(texSize), new Vector2(0), new Vector2(1), new Vector4(0), new Vector4(4, 4, 4, 4));
                        ImGui.SameLine();

                        /*ImGui.PushTextWrapPos((float)currentPosition + texSize);
                        ImGui.SetCursorPosX((float)currentPosition);
                        ImGui.TextWrapped(entry.SkillName);
                        ImGui.PopTextWrapPos();
                        ImGui.SameLine();*/

                        if (styleChanged)
                        {
                            ImGui.PopStyleVar();
                        }

                        lastPosition = currentPosition;
                        idx++;
                    }
                }

                if (removeLast)
                {
                    TimelineEntries.TryDequeue(out _);
                }

                ImGui.EndChild();

                if (shouldTransparentBg)
                {
                    ImGui.PopStyleColor();
                }

                windowSettings.TimelinePosition = ImGui.GetWindowPos();
                windowSettings.TimelineSize = ImGui.GetWindowSize();
                ImGui.End();
                ImGui.PopStyleVar();
            }

            if (!IsEditMode)
            {
                ImGui.PopStyleVar();
            }

            ImGui.PopID();
        }

        public static double Lerp(double start, double end, double t)
        {
            return start + (end - start) * t;
        }

        public class TimelineSkillEntry
        {
            public int SkillId = 0;
            public string SkillName = "";
            public string Icon = "";
            public DateTime StartTime;
        }
    }

    public class SkillCastTimelineWindowSettings : WindowSettingsBase
    {
        public bool IsTimelineEnabled = false;
        public bool HideTimelineInTaskBar = false;
        public Vector2 TimelinePosition = new();
        public Vector2 TimelineSize = new();
        public float TimelineSpeed = 5.0f;
        public int TimelineIconSize = 64;
        public bool TimelineAllowOverlap = false;
        public bool TimelineTransparentBackground = false;
        public bool TimelineWindowModifiedOpacity = false;
        public int TimelineWindowOpacityValue = 100;
        public bool TimelineUseChromaKey = false;
        public Vector4 TimelineWindowChromaKey = new(0, 0, 0, 1);
        public bool TimelineOnlyShowInCombat = false;
    }
}
