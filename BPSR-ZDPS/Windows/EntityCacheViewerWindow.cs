using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Managers.External;
using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

namespace BPSR_ZDPS
{
    public static class EntityCacheViewerWindow
    {
        public const string LAYER = "EntityCacheViewerWindowLayer";
        public static string TITLE_ID = "###EntityCacheViewerWindow";
        public static string TITLE = "Entity Cache Viewer";
        public static bool IsOpened = false;
        public static bool CollapseToContentOnly = false;
        public static Vector2 DefaultWindowSize = new Vector2(700, 600);
        public static bool ResetWindowSize = false;

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;
        static int LastPinnedOpacity = 100;
        static bool IsPinned = false;

        static KeyValuePair<long, EntityCacheLine>[] EntityFilterMatches = [];
        static string EntityNameFilter = "";

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            IsPinned = false;
            InitializeBindings();
            ImGui.PopID();
        }

        public static void InitializeBindings()
        {
            if (HasInitBindings == false)
            {
                HasInitBindings = true;
            }
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            var windowSettings = Settings.Instance.WindowSettings.EntityCacheViewer;

            ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 300), new Vector2(ImGui.GETFLTMAX()));

            if (windowSettings.WindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(windowSettings.WindowSize, ImGuiCond.FirstUseEver);
            }

            if (ResetWindowSize)
            {
                ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.Always);
                ResetWindowSize = false;
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (AppState.MousePassthrough && windowSettings.TopMost)
            {
                exWindowFlags |= ImGuiWindowFlags.NoInputs;
            }

            if (ImGui.Begin($"{TITLE}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking | exWindowFlags))
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

                    if (windowSettings.TopMost && !IsPinned)
                    {
                        IsPinned = true;
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }
                else if (RunOnceDelayed >= 2)
                {
                    if (windowSettings.TopMost && LastPinnedOpacity != windowSettings.Opacity)
                    {
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }

                DrawMenuBar();

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Entity Filter: ");
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputText("##EntityFilterText", ref EntityNameFilter, 64))
                {
                    if (EntityNameFilter.Length == 0)
                    {
                        // We don't want to show the entire cache as it comes with a massive performance cost currently (we don't use virtualization/clippers yet)
                        EntityFilterMatches = [];
                    }
                    else
                    {
                        bool isNum = EntityNameFilter.Length > 0 && Char.IsNumber(EntityNameFilter[0]);
                        EntityFilterMatches = EntityCache.Instance.Cache.Lines.AsValueEnumerable().Where(x => isNum ? x.Key.ToString().Contains(EntityNameFilter) : x.Value.Name != null && x.Value.Name.Contains(EntityNameFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }
                }

                if (ImGui.BeginListBox("##SearchResultsListBox", new Vector2(-1, -1)))
                {
                    foreach (var item in EntityFilterMatches)
                    {
                        ImGui.BeginGroup();
                        var sb = new StringBuilder();
                        sb.AppendLine($"Name: {item.Value.Name}");
                        sb.AppendLine($"UID: {item.Value.UID}");
                        sb.AppendLine($"Ability Score: {item.Value.AbilityScore} (+ {item.Value.SeasonStrength})");
                        sb.AppendLine($"Profession: {Professions.GetProfessionNameFromId(item.Value.ProfessionId)}{(item.Value.SubProfessionId > 0 ? $" - {Professions.GetSubProfessionNameFromId(item.Value.SubProfessionId)}" : "")}");
                        sb.AppendLine($"Level: {item.Value.Level} (+ {item.Value.SeasonLevel})");

                        ImGui.Selectable($"{sb}");
                        ImGui.EndGroup();
                    }

                    ImGui.EndListBox();
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
                var windowSettings = Settings.Instance.WindowSettings.EntityCacheViewer;

                MenuBarSize = ImGui.GetWindowSize();

                ImGui.Text($"{TITLE}");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, windowSettings.TopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}##TopMostBtn"))
                {
                    if (!windowSettings.TopMost)
                    {
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                        windowSettings.TopMost = true;
                        IsPinned = true;
                    }
                    else
                    {
                        Utils.UnsetWindowTopmost();
                        Utils.SetWindowOpacity(1.0f);
                        windowSettings.TopMost = false;
                        IsPinned = false;
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Pin Window As Top Most");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, CollapseToContentOnly ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{(CollapseToContentOnly ? FASIcons.AnglesDown : FASIcons.AnglesUp)}##CollapseToContentBtn"))
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
                if (ImGui.MenuItem($"X##CloseBtn"))
                {
                    windowSettings.WindowSize = ImGui.GetWindowSize();
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }
    }

    public class EntityCacheViewerWindowSettings : WindowSettingsBase
    {

    }
}
