using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Managers;
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
    public static class RaidManagerCountdownWindow
    {
        public const string LAYER = "RaidManagerCountdownWindowLayer";
        public static string TITLE_ID = "###RaidManagerCountdownWindow";
        public static bool IsOpened = false;

        static int RunOnceDelayed = 0;
        static bool HasInitBindings = false;
        static bool IsEditMode = false;
        static Vector2? NewCountdownWindowLocation = null;
        static int CountdownRunOnceDelayed = 0;

        static bool HasCountdown = false;
        static DateTime CountdownEnd;
        static DateTime EditCountdown;
        static Vector2 CountdownImageGroupSize = new();
        static Vector2 CountdownImageEditGroupSize = new();

        static string EntityNameFilter = "";
        static KeyValuePair<long, EntityCacheLine>[]? EntityFilterMatches;

        static ImGuiWindowClassPtr CountdownDisplayClass = ImGui.ImGuiWindowClass();
        static ImGuiWindowClassPtr CountdownEditorClass = ImGui.ImGuiWindowClass();

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

                CountdownDisplayClass.ClassId = ImGuiP.ImHashStr("CountdownClass");
                CountdownDisplayClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.TopMost | ImGuiViewportFlags.NoTaskBarIcon | ImGuiViewportFlags.NoInputs;// | ImGuiViewportFlags.NoRendererClear;

                CountdownEditorClass.ClassId = ImGuiP.ImHashStr("CountdownEditorClass");
                CountdownEditorClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.TopMost;

                ChatManager.OnChatMessage += RaidManager_OnChatMessage;
            }
        }

        private static void RaidManager_OnChatMessage(DataTypes.Chat.User arg1, DataTypes.Chat.ChatMessage arg2, Zproto.ChitChatNtf.Types.NotifyNewestChitChatMsgs arg3)
        {
            if (arg2.Msg.MsgType == Zproto.ChitChatMsgType.ChatMsgTextMessage && arg2.Msg.MsgText.Length > 4)
            {
                if (Settings.Instance.WindowSettings.RaidManagerCountdown.ChatChannels.Contains(arg2.Channel))
                {
                    bool isAllowed = !Settings.Instance.WindowSettings.RaidManagerCountdown.PlayerUIDBlacklist.Contains(arg1.Info.CharId);
                    if (isAllowed && arg2.Msg.MsgText.StartsWith("/countdown ", StringComparison.OrdinalIgnoreCase) ||
                        arg2.Msg.MsgText.StartsWith("/ct ", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = arg2.Msg.MsgText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length > 1)
                        {
                            if (char.IsNumber(parts[1][0]))
                            {
                                int.TryParse(parts[1], out int value);
                                // Only allow a Countdown to be set between 3 and 30 seconds
                                if (value >= 3 && value <= 30)
                                {
                                    StartCountdown(value);
                                }
                            }
                            else
                            {
                                string[] cancelKeywords = { "cancel", "abort", "stop" };
                                if (cancelKeywords.Contains(parts[1], StringComparer.OrdinalIgnoreCase))
                                {
                                    StopCountdown();
                                }
                            }
                        }
                    }
                }
            }
        }

        static void StartCountdown(int seconds)
        {
            CountdownEnd = DateTime.Now.AddSeconds(seconds);
            CountdownRunOnceDelayed = 0;
            HasCountdown = true;
        }

        static void StopCountdown()
        {
            HasCountdown = false;
        }

        public static void Draw(MainWindow mainWindow)
        {
            InitializeBindings();

            var windowSettings = Settings.Instance.WindowSettings.RaidManagerCountdown;

            if (windowSettings.CountdownPosition == new Vector2())
            {
                CenterDisplay();
            }

            if (HasCountdown)
            {
                ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

                var countdownRemaining = CountdownEnd.Subtract(DateTime.Now);
                if (countdownRemaining.TotalSeconds > 0)
                {
                    ImGui.SetNextWindowClass(CountdownDisplayClass);

                    ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.Always);

                    if (windowSettings.CountdownPosition != new Vector2())
                    {
                        ImGui.SetNextWindowPos(windowSettings.CountdownPosition, ImGuiCond.Appearing);
                    }

                    if (NewCountdownWindowLocation != null)
                    {
                        ImGui.SetNextWindowPos(NewCountdownWindowLocation.Value, ImGuiCond.Always);
                        NewCountdownWindowLocation = null;
                    }

                    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1 / 255.0f, 1 / 255.0f, 1 / 255.0f, 0.0f));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                    if (ImGui.Begin("Countdown", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs))
                    {
                        if (CountdownRunOnceDelayed == 0)
                        {
                            CountdownRunOnceDelayed++;
                        }
                        else if (CountdownRunOnceDelayed <= 2)
                        {
                            CountdownRunOnceDelayed++;
                            unsafe
                            {
                                // This is how we support transparency effects of just the background and not the text content.
                                // SetLayeredWindowAttributes will chromakey the given 0xAABBGGRR value anywhere on the window and also set the Alpha of the window between 0-255
                                // This is needed due to Nvidia drivers incorrectly behaving with performing an ImGui drawlist clear via Window Resize and using cached frames instead of drawing new ones like all other GPU vendors
                                Hexa.NET.ImGui.Backends.Win32.ImGuiImplWin32.EnableAlphaCompositing(ImGui.GetWindowViewport().PlatformHandleRaw);
                                Utils.SetWindowLong(User32.GWL_EXSTYLE, User32.GetWindowLong((nint)ImGui.GetWindowViewport().PlatformHandleRaw, User32.GWL_EXSTYLE) | (nint)User32.WS_EX_LAYERED);
                                User32.SetLayeredWindowAttributes((nint)ImGui.GetWindowViewport().PlatformHandleRaw, 0x00010101, 255, User32.LWA_COLORKEY | User32.LWA_ALPHA);
                            }
                        }
                        else if (CountdownRunOnceDelayed < 3)
                        {
                            CountdownRunOnceDelayed++;
                        }

                        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0.0f));
                        if (ImGui.BeginChild("##CountdownChild", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.NoInputs))
                        {
                            if (windowSettings.UseStylizedNumbers)
                            {
                                string remainingStr = ((int)Math.Ceiling(countdownRemaining.TotalSeconds)).ToString();
                                var avail = ImGui.GetContentRegionAvail();
                                ImGui.SetCursorPos(new Vector2(MathF.Max(0.0f, (avail.X - CountdownImageGroupSize.X) * 0.5f), MathF.Max(0.0f, (avail.Y - CountdownImageGroupSize.Y) * 0.5f)));
                                ImGui.PushItemWidth(-1);
                                ImGui.BeginGroup();
                                for (int i = 0; i < remainingStr.Length; i++)
                                {
                                    var tex = ImageHelper.GetTextureByKey($"BasicNumber{remainingStr[i]}");
                                    if (tex != null)
                                    {
                                        if (i != 0)
                                        {
                                            ImGui.SameLine();
                                        }
                                        ImGui.ImageWithBg(tex.Value, new Vector2(70 * 1.75f, 110 * 1.75f), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), Colors.OrangeRed);
                                    }
                                }
                                ImGui.EndGroup();
                                CountdownImageGroupSize = ImGui.GetItemRectSize();
                            }
                            else
                            {
                                ImGui.PushFont(null, 250);
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.OrangeRed);
                                ImGui.TextAligned(0.5f, -1, $"{(int)Math.Ceiling(countdownRemaining.TotalSeconds)}");
                                ImGui.PopStyleColor();
                                ImGui.PopFont();
                            }

                            ImGui.EndChild();
                        }
                        ImGui.PopStyleColor();

                        ImGui.End();
                    }
                    ImGui.PopStyleVar();
                    ImGui.PopStyleColor();
                }
                else
                {
                    HasCountdown = false;
                }

                ImGui.PopID();
            }
            else
            {
                CountdownRunOnceDelayed = 0;
            }

            if (!IsOpened)
            {
                return;
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGui.SetNextWindowSize(new Vector2(650, 550), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(550, 450), new Vector2(ImGui.GETFLTMAX()));

            if (ImGui.Begin($"Raid Manager - Countdowns{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 2)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();
                }
                else if (RunOnceDelayed < 3)
                {
                    RunOnceDelayed++;
                }

                ImGui.TextWrapped("Countdowns are numeric countdown timers displayed in a defined section of the screen. These are triggered by a message sent with '/countdown' or '/ct'. Countdowns can be between 3 and 30 seconds long.");
                ImGui.BulletText("'/countdown 30' (or '/ct 30') will display a 30 second countdown on the screen.");
                ImGui.BulletText("'/countdown cancel' will cancel the current countdown.");

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Allow Countdowns: ");
                ImGui.SameLine();
                ImGui.Checkbox("##CountdownsEnabled", ref windowSettings.AllowCountdowns);
                ImGui.Indent();
                ImGui.BeginDisabled(true);
                ImGui.TextWrapped("When enabled, allows Countdowns to be processed and displayed.");
                ImGui.EndDisabled();
                ImGui.Unindent();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Use Stylized Numbers: ");
                ImGui.SameLine();
                ImGui.Checkbox("##UseStylizedNumbers", ref windowSettings.UseStylizedNumbers);
                ImGui.Indent();
                ImGui.BeginDisabled(true);
                ImGui.TextWrapped("When enabled, the numbers for the Countdown time will be stylized images instead of plain text.");
                ImGui.EndDisabled();
                ImGui.Unindent();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Select Chat Channels To Use: ");
                ChatChannelToggle("Local", Zproto.ChitChatChannelType.ChannelScene);
                ImGui.SameLine();
                ChatChannelToggle("Team", Zproto.ChitChatChannelType.ChannelTeam);
                ImGui.SameLine();
                ChatChannelToggle("Guild", Zproto.ChitChatChannelType.ChannelUnion);
                ImGui.SameLine();
                ChatChannelToggle("Group", Zproto.ChitChatChannelType.ChannelGroup);

                string toggleEditModeText = !IsEditMode ? "Edit Countdowns Location" : "Stop Editing Location";
                if (ImGui.Button($"{toggleEditModeText}##ToggleEditModeBtn"))
                {
                    IsEditMode = !IsEditMode;
                    EditCountdown = DateTime.Now.AddSeconds(30);
                }

                if (ImGui.Button("Center Countdown Timer"))
                {
                    CenterDisplay();
                }

                if (ImGui.Button("Test 10 Second Countdown"))
                {
                    StartCountdown(10);
                }

                ImGui.SameLine();
                if (ImGui.Button("Test 30 Second Countdown"))
                {
                    StartCountdown(30);
                }

                if (ImGui.CollapsingHeader("Player Blacklist##PlayerBlacklistSection", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.TextUnformatted("Select Players from the list below to add them to a Blacklist that ignores Countdowns from them.");

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Entity Filter: ");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputText("##EntityFilterText", ref EntityNameFilter, 64))
                    {
                        if (EntityNameFilter.Length > 0)
                        {
                            bool isNum = Char.IsNumber(EntityNameFilter[0]);
                            EntityFilterMatches = EntityCache.Instance.Cache.Lines.AsValueEnumerable().Where(x => isNum ? x.Value.UID.ToString().Contains(EntityNameFilter) : x.Value.Name != null && x.Value.Name.Contains(EntityNameFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
                        }
                        else
                        {
                            EntityFilterMatches = null;
                        }
                    }

                    if (ImGui.BeginListBox("##PlayerListBox", new Vector2(ImGui.GetContentRegionAvail().X, 120)))
                    {
                        if (EntityFilterMatches != null && (EntityFilterMatches.Length < 100 || EntityNameFilter.Length > 2))
                        {
                            if (EntityFilterMatches.Any())
                            {
                                long matchIdx = 0;
                                foreach (var match in EntityFilterMatches)
                                {
                                    bool isSelected = windowSettings.PlayerUIDBlacklist.Contains(match.Value.UID);

                                    if (isSelected)
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                        if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{matchIdx}", new Vector2(30, 30)))
                                        {
                                            windowSettings.PlayerUIDBlacklist.Remove(match.Value.UID);
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
                                            windowSettings.PlayerUIDBlacklist.Add(match.Value.UID);
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
                }

                ImGui.End();
            }

            if (IsEditMode)
            {
                ImGui.SetNextWindowClass(CountdownEditorClass);

                ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.Always);

                if (windowSettings.CountdownPosition != new Vector2())
                {
                    ImGui.SetNextWindowPos(windowSettings.CountdownPosition, ImGuiCond.Appearing);
                }

                if (NewCountdownWindowLocation != null)
                {
                    ImGui.SetNextWindowPos(NewCountdownWindowLocation.Value, ImGuiCond.Always);
                    NewCountdownWindowLocation = null;
                }

                if (ImGui.Begin("Countdown - Edit Position##CountdownPositionEditor", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
                {
                    ImGui.TextAligned(0.5f, -1, "Countdown - Edit Position");
                    ImGui.SetCursorPos(new(0));
                    windowSettings.CountdownPosition = ImGui.GetWindowPos();

                    
                    var remaining = (int)Math.Ceiling(EditCountdown.Subtract(DateTime.Now).TotalSeconds);
                    if (remaining <= 0)
                    {
                        EditCountdown = DateTime.Now.AddSeconds(30);
                    }

                    if (windowSettings.UseStylizedNumbers)
                    {
                        string remainingStr = remaining.ToString();
                        var avail = ImGui.GetContentRegionAvail();
                        ImGui.SetCursorPos(new Vector2(MathF.Max(0.0f, (avail.X - CountdownImageEditGroupSize.X) * 0.5f), MathF.Max(0.0f, (avail.Y - CountdownImageEditGroupSize.Y) * 0.5f)));
                        ImGui.PushItemWidth(-1);
                        ImGui.BeginGroup();
                        for (int i = 0; i < remainingStr.Length; i++)
                        {
                            var tex = ImageHelper.GetTextureByKey($"BasicNumber{remainingStr[i]}");
                            if (tex != null)
                            {
                                if (i != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.ImageWithBg(tex.Value, new Vector2(70 * 1.75f, 110 * 1.75f), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), Colors.OrangeRed);
                            }
                        }
                        ImGui.EndGroup();
                        CountdownImageEditGroupSize = ImGui.GetItemRectSize();
                    }
                    else
                    {
                        ImGui.PushFont(null, 250);
                        ImGui.PushStyleColor(ImGuiCol.Text, Colors.OrangeRed_Transparent);
                        ImGui.TextAligned(0.5f, -1, $"{remaining}");
                        ImGui.PopStyleColor();
                        ImGui.PopFont();
                    }
                    
                    ImGui.End();
                }
            }

            ImGui.PopID();
        }

        static void CenterDisplay()
        {
            var gameProc = BPSR_ZDPSLib.Utils.GetCachedProcessEntry();
            if (gameProc != null && gameProc.ProcessId > 0 && !string.IsNullOrEmpty(gameProc.ProcessName))
            {
                try
                {
                    System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(gameProc.ProcessId);
                    User32.RECT procRect = new();
                    User32.GetWindowRect(process.MainWindowHandle, ref procRect);
                    float centerX = MathF.Floor((procRect.left + procRect.right) * 0.5f);
                    float centerY = MathF.Floor((procRect.top + procRect.bottom) * 0.5f);
                    Vector2 centerPoint = new Vector2(centerX, centerY);
                    var newPosition = centerPoint - new Vector2(150, 150);
                    Settings.Instance.WindowSettings.RaidManagerCountdown.CountdownPosition = newPosition;
                    NewCountdownWindowLocation = newPosition;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error using game process for centering Countdown Timer.");
                }
            }
            else
            {
                // Game process was not found, use the primary monitor bounds instead
                var glfwMonitor = Hexa.NET.GLFW.GLFW.GetPrimaryMonitor();
                var glfwVidMode = Hexa.NET.GLFW.GLFW.GetVideoMode(glfwMonitor);
                Vector2 centerPoint = new Vector2(MathF.Floor(glfwVidMode.Width * 0.5f), MathF.Floor(glfwVidMode.Height * 0.5f));
                var newPosition = centerPoint - new Vector2(150, 150);
                Settings.Instance.WindowSettings.RaidManagerCountdown.CountdownPosition = newPosition;
                NewCountdownWindowLocation = newPosition;
            }
        }

        static void ChatChannelToggle(string name, Zproto.ChitChatChannelType channel)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"{name}: ");
            ImGui.SameLine();
            var isEnabled = Settings.Instance.WindowSettings.RaidManagerCountdown.ChatChannels.Contains(channel);
            if (ImGui.Checkbox($"##Channel_{name}", ref isEnabled))
            {
                if (isEnabled)
                {
                    Settings.Instance.WindowSettings.RaidManagerCountdown.ChatChannels.Add(channel);
                }
                else
                {
                    Settings.Instance.WindowSettings.RaidManagerCountdown.ChatChannels.Remove(channel);
                }
            }
        }
    }

    public class RaidManagerCountdownWindowSettings : WindowSettingsBase
    {
        public bool AllowCountdowns = true;
        public Vector2 CountdownPosition = new();
        public bool UseStylizedNumbers = true;
        public HashSet<Zproto.ChitChatChannelType> ChatChannels = new() { Zproto.ChitChatChannelType.ChannelTeam };
        public List<long> PlayerUIDBlacklist = new();
    }
}
