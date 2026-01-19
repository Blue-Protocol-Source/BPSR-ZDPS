using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.Chat;
using BPSR_ZDPS.Managers;
using Hexa.NET.ImGui;
using System.Globalization;
using System.Numerics;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS.Windows
{
    public class ChatWindow
    {
        public const string LAYER = "ChatWindowLayer";
        public static string TITLE_ID = "###ChatWindow";
        public static bool IsOpened = false;

        static int RunOnceDelayed = 0;
        public static Vector2 DefaultWindowSize = new Vector2(700, 600);
        public static bool ResetWindowSize = false;

        static ImGuiWindowClassPtr ChatWindowClass = ImGui.ImGuiWindowClass();
        static ChatTab SelectedChatTab = null;
        static bool IsEditingNewTab = false;
        static ChatTab EditingTab = null;

        static string EntityNameFilter = "";
        static KeyValuePair<long, EntityCacheLine>[]? EntityFilterMatches = [];

        static ChatWindow()
        {
            if (Settings.Instance.WindowSettings.ChatWindow.ChatTabs == null || Settings.Instance.WindowSettings.ChatWindow.ChatTabs.Count() == 0)
            {
                ChatManager.AddChatTab(new ChatTabConfig()
                {
                    Name = "World",
                    Channels = [ChitChatChannelType.ChannelWorld]
                });

                ChatManager.AddChatTab(new ChatTabConfig()
                {
                    Name = "Guild / Team",
                    Channels = [ChitChatChannelType.ChannelUnion, ChitChatChannelType.ChannelGroup, ChitChatChannelType.ChannelTeam]
                });

                ChatManager.AddChatTab(new ChatTabConfig()
                {
                    Name = "All",
                    Channels =
                    [
                        ChitChatChannelType.ChannelWorld,
                        ChitChatChannelType.ChannelGroup,
                        ChitChatChannelType.ChannelUnion,
                        ChitChatChannelType.ChannelTeam,
                        ChitChatChannelType.ChannelPrivate,
                        ChitChatChannelType.ChannelScene,
                        ChitChatChannelType.ChannelSystem,
                        ChitChatChannelType.ChannelNull,
                        ChitChatChannelType.ChannelTopNotice
                    ]
                });

                foreach (var tab in ChatManager.ChatTabs)
                {
                    Settings.Instance.WindowSettings.ChatWindow.ChatTabs.Add(tab.Config);
                }

                Settings.Instance.WindowSettings.ChatWindow.LastSelectedTabId = ChatManager.ChatTabs[0].Config.Id;
            }
            else
            {
                foreach (var tab in Settings.Instance.WindowSettings.ChatWindow.ChatTabs)
                {
                    ChatManager.AddChatTab(tab);
                }
            }
        }

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;

            ChatWindowClass.ClassId = ImGuiP.ImHashStr("ChatWindowClass");
            ChatWindowClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.None;

            SelectedChatTab = ChatManager.ChatTabs.FirstOrDefault(x => x.Config.Id == Settings.Instance.WindowSettings.ChatWindow.LastSelectedTabId);

            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            var windowSettings = Settings.Instance.WindowSettings.ChatWindow;
            PreDraw(windowSettings);
            InnerDraw(windowSettings);

            ImGui.PopID();
        }

        private static void InnerDraw(ChatWindowSettings windowSettings)
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(17 / 255.0f, 17 / 255.0f, 17 / 255.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            if (ImGui.Begin($"Chat##ChatWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoScrollbar))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 2)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();

                    if (windowSettings.TopMost)
                    {
                        Utils.SetWindowTopmost();
                    }
                }

                unsafe
                {
                    // This is how we support transparency effects of just the background and not the text content.
                    // SetLayeredWindowAttributes will chromakey the given 0xAABBGGRR value anywhere on the window and also set the Alpha of the window between 0-255
                    // This is needed due to Nvidia drivers incorrectly behaving with performing an ImGui drawlist clear via Window Resize and using cached frames instead of drawing new ones like all other GPU vendors
                    Hexa.NET.ImGui.Backends.Win32.ImGuiImplWin32.EnableAlphaCompositing(ImGui.GetWindowViewport().PlatformHandleRaw);
                    Utils.SetWindowLong(User32.GWL_EXSTYLE, User32.GetWindowLong((nint)ImGui.GetWindowViewport().PlatformHandleRaw, User32.GWL_EXSTYLE) | (nint)User32.WS_EX_LAYERED);
                    User32.SetLayeredWindowAttributes((nint)ImGui.GetWindowViewport().PlatformHandleRaw, 0x00111111, (byte)(windowSettings.Opacity == 100 ? 255 : 210), User32.LWA_COLORKEY | User32.LWA_ALPHA);
                }

                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, windowSettings.Opacity * 0.01f));
                if (ImGui.BeginChild("##ChatWindowChild", new Vector2(0, windowSettings.WindowSize.Y - 8), ImGuiChildFlags.AutoResizeY))
                {
                    DrawChatTabs();
                    DrawManageButtons();
                    DrawEditChatTab();
                    DrawDeleteChatTabConfirm();
                    DrawChatMessages();
                    ImGui.EndChild();
                }

                ImGui.PopStyleColor();

                windowSettings.WindowPosition = ImGui.GetWindowPos();
                windowSettings.WindowSize = ImGui.GetWindowSize();

                ImGui.End();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }

        private static void PreDraw(ChatWindowSettings windowSettings)
        {
            ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(450, 360), new Vector2(ImGui.GETFLTMAX()));

            if (windowSettings.WindowPosition != new Vector2())
            {
                ImGui.SetNextWindowPos(windowSettings.WindowPosition, ImGuiCond.FirstUseEver);
            }

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

            ImGui.SetNextWindowClass(ChatWindowClass);
        }

        private static void DrawChatTabs()
        {
            lock (ChatManager.ChatTabs)
            {
                foreach (var tab in ChatManager.ChatTabs)
                {
                    DrawChatTabButton(tab);
                }
            }

            ImGui.SameLine();
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
            if (ImGui.Button("+"))
            {
                EditingTab = new ChatTab(new ChatTabConfig()
                {
                    Name = "New Tab"
                });

                IsEditingNewTab = true;
                ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
                ImGui.OpenPopup("EditChatTab");
                ImGui.PopID();
            }
            ImGui.PopFont();
            ImGui.SetItemTooltip("Add a new chat tab");
            ImGui.SameLine();
        }

        private static void DrawChatTabButton(ChatTab tab)
        {
            ImGui.PushID((int)tab.Config.Id);
            bool tabIsSelected = SelectedChatTab == tab;

            if (tabIsSelected) ImGui.PushStyleColor(ImGuiCol.Button, Colors.DimGray);
            if (ImGui.Button(tab.Config.Name))
            {
                SelectedChatTab = tab;
                Settings.Instance.WindowSettings.ChatWindow.LastSelectedTabId = tab.Config.Id;
            }
            if (tabIsSelected) ImGui.PopStyleColor();

            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem("Edit"))
                {
                    ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
                    EditingTab = tab;
                    IsEditingNewTab = false;
                    ImGui.OpenPopup("EditChatTab");
                    ImGui.PopID();
                }

                if (ImGui.MenuItem("Delete"))
                {
                    ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
                    EditingTab = tab;
                    ImGui.OpenPopup("DeleteConfirm");
                    ImGui.PopID();
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();

            ImGui.SameLine();
        }

        private static void DrawManageButtons()
        {
            var chatWindowSettings = Settings.Instance.WindowSettings.ChatWindow;
            var windowViewport = ImGui.GetWindowViewport();
            bool openBlockedUsersPopup = false;

            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (25));
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
            if (ImGui.Button($"{FASIcons.Gear}##OptionsMenu"))
            {
                ImGui.OpenPopup("SettingsPopup");
            }
            ImGui.PopFont();
            ImGui.SameLine();

            //ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 200);
            ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new Vector2(ImGui.GetWindowSize().X - 350, 30f));
            if (ImGui.BeginPopup("SettingsPopup"))
            {
                ImGui.TextUnformatted("Chat Settings");
                ImGui.Separator();

                /*
                //ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 200);
                float backgroundOpacity = chatWindowSettings.BackgroundOpacity;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Background Opacity:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("##BackgroundOpacity", ref backgroundOpacity, 0.03f, 1f, $"{(int)(backgroundOpacity * 100)}%%", ImGuiSliderFlags.ClampOnInput))
                {
                    chatWindowSettings.BackgroundOpacity = MathF.Round(backgroundOpacity, 2);
                }
                ImGui.PopStyleColor(2);
                */
                int opacity = chatWindowSettings.Opacity;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Window Opacity:");
                ImGui.SameLine();
                ImGui.Dummy(new Vector2(17, 0));
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("##Opacity", ref opacity, 10, 100, $"{opacity}%%", ImGuiSliderFlags.ClampOnInput))
                {
                    chatWindowSettings.Opacity = opacity;
                    //Utils.SetWindowOpacity(chatWindowSettings.Opacity * 0.01f, windowViewport);
                }
                ImGui.PopStyleColor(2);

                var isTopMost = chatWindowSettings.TopMost;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Top Most: ");
                ImGui.SameLine();
                if (ImGui.Checkbox("##TopMost", ref isTopMost))
                {
                    if (!chatWindowSettings.TopMost)
                    {
                        Utils.SetWindowTopmost(windowViewport);
                        Utils.SetWindowOpacity(chatWindowSettings.Opacity * 0.01f, windowViewport);
                        chatWindowSettings.TopMost = true;
                    }
                    else
                    {
                        Utils.UnsetWindowTopmost(windowViewport);
                        Utils.SetWindowOpacity(1.0f, windowViewport);
                        chatWindowSettings.TopMost = false;
                    }
                }

                ImGui.SameLine();

                var compactMode = chatWindowSettings.CompactMode;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Compact Mode:");
                ImGui.SameLine();
                ImGui.Checkbox("##CompactMode", ref compactMode);
                chatWindowSettings.CompactMode = compactMode;

                var showTime = chatWindowSettings.ShowTime;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Show Time:");
                ImGui.SameLine();
                ImGui.Checkbox("##ShowTime", ref showTime);
                chatWindowSettings.ShowTime = showTime;

                ImGui.SameLine();

                var showTimeAsXAgo = chatWindowSettings.ShowTimeAsXAgo;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Show Time as X seconds ago:");
                ImGui.SameLine();
                ImGui.Checkbox("##ShowTimeAsXAgo", ref showTimeAsXAgo);
                chatWindowSettings.ShowTimeAsXAgo = showTimeAsXAgo;

                var maxChatHistory = Settings.Instance.Chat.MaxChatHistory;
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Max Chat History:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(200);
                var wasMaxChatHistoryChanged = ImGui.SliderInt("##MaxChatHistory", ref maxChatHistory, 1, 500);
                Settings.Instance.Chat.MaxChatHistory = maxChatHistory;
                ImGui.PopStyleColor(2);

                if (wasMaxChatHistoryChanged)
                {
                    ChatManager.RemoveMessagesOverCap(Settings.Instance.Chat.MaxChatHistory);
                }

                ImGui.Dummy(new Vector2(0, 5));
                ImGui.Separator();
                if (ImGui.Button("Manage Blocked Users", new Vector2(-1, 0)))
                {
                    openBlockedUsersPopup = true;
                }

                ImGui.Separator();
                if (ImGui.MenuItem("Close Chat Window"))
                {
                    chatWindowSettings.WindowPosition = ImGui.GetWindowPos();
                    chatWindowSettings.WindowSize = ImGui.GetWindowSize();
                    IsOpened = false;
                }

                ImGui.EndPopup();
            }

            if (openBlockedUsersPopup)
            {
                ImGui.OpenPopup("BlockedUsersPopup");
            }

            ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new Vector2(ImGui.GetWindowSize().X - 550, 30f));
            if (ImGui.BeginPopup("BlockedUsersPopup"))
            {
                ImGui.TextUnformatted("Blocked Users");
                ImGui.Separator();

                var height = windowViewport.Size.Y - 100;
                if (ImGui.BeginChild("BlockedUserArea", new Vector2(500, height), ImGuiChildFlags.None, ImGuiWindowFlags.NoDecoration))
                {
                    if (ImGui.CollapsingHeader("Blocked Users", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        if (ImGui.BeginTable("BlockedUserTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                        {
                            ImGui.TableSetupColumn("Blocked At", ImGuiTableColumnFlags.WidthFixed, 100f);
                            ImGui.TableSetupColumn("UID", ImGuiTableColumnFlags.WidthFixed, 100f);
                            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80f);
                            ImGui.TableHeadersRow();

                            foreach (var blockedUser in Settings.Instance.Chat.BlockedUsers.Values)
                            {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{(blockedUser.BlockedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{blockedUser.ID}");

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{blockedUser.Name}");

                                ImGui.TableNextColumn();
                                if (ImGui.Button($"Unblock##{blockedUser.ID}", new Vector2(-1, 0)))
                                {
                                    ChatManager.UnblockUser(blockedUser.ID);
                                }
                            }

                            ImGui.EndTable();
                        }
                    }

                    if (ImGui.CollapsingHeader("Add Player Search"))
                    {
                        ImGui.TextUnformatted("Select Players from the list below to add them to the chat block list.");

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
                                        if (!ChatManager.IsUserBlocked(match.Value.UID))
                                        {
                                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green_Transparent);
                                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                            if (ImGui.Button($"{FASIcons.Plus}##AddBtn_{matchIdx}", new Vector2(30, 30)))
                                            {
                                                var user = new User(new BasicShowInfo()
                                                {
                                                    CharId = match.Value.UID,
                                                    Name = match.Value.Name
                                                });
                                                ChatManager.BlockUser(user);
                                            }
                                            ImGui.PopFont();
                                            ImGui.PopStyleColor();
                                        }
                                        else
                                        {
                                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                                            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                                            if (ImGui.Button($"{FASIcons.Minus}##RemoveBtn_{matchIdx}", new Vector2(30, 30)))
                                            {
                                                ChatManager.UnblockUser(match.Value.UID);
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
                }
                ImGui.EndChild();

                ImGui.EndPopup();
            }
        }

        private static void DrawEditChatTab()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            if (EditingTab != null)
            {
                ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new Vector2(10, 30f));
                if (ImGui.BeginPopup("EditChatTab"))
                {
                    ImGui.TextUnformatted($"Chat Tab Settings for {EditingTab.Config.Name}");
                    ImGui.Separator();

                    if (DrawChatTabConfig(EditingTab.Config))
                    {
                        ChatManager.RefilterChatTab(EditingTab);
                    }

                    if (IsEditingNewTab)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
                        if (ImGui.Button("Save", new Vector2(-1, 0)))
                        {
                            ChatManager.AddChatTab(EditingTab.Config);
                            Settings.Instance.WindowSettings.ChatWindow.ChatTabs.Add(EditingTab.Config);
                            Settings.Instance.WindowSettings.ChatWindow.LastSelectedTabId = EditingTab.Config.Id;
                            Settings.Save();
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.PopStyleColor();
                    }

                    ImGui.EndPopup();
                }
            }
            ImGui.PopID();
        }

        private static void DrawDeleteChatTabConfirm()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new Vector2(10, 30f));
            ImGui.SetNextWindowSize(new Vector2(0, 0));
            if (ImGui.BeginPopup("DeleteConfirm", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking))
            {
                var windowWidth = ImGui.GetContentRegionAvail().X - 10;
                ImGui.TextUnformatted($"Are you sure you want to delete: {EditingTab.Config.Name}");
                if (ImGui.Button("Yes", new Vector2(windowWidth / 2, 0)))
                {
                    if (EditingTab == SelectedChatTab)
                    {
                        SelectedChatTab = ChatManager.ChatTabs.FirstOrDefault();
                        Settings.Instance.WindowSettings.ChatWindow.LastSelectedTabId = SelectedChatTab.Config.Id;
                    }

                    ChatManager.RemoveChatTab(EditingTab.Config);
                    Settings.Instance.WindowSettings.ChatWindow.ChatTabs.Remove(EditingTab.Config);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("No", new Vector2(windowWidth / 2, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
            ImGui.PopID();
        }

        private static void DrawChatMessages()
        {
            ImGui.SetCursorPos(new Vector2(0, 27));
            var shouldScrollToBottom = true;
            if (ImGui.BeginChild("Messages", ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoBackground))
            {
                if (SelectedChatTab != null)
                {
                    lock (SelectedChatTab.MessageIds)
                    {
                        foreach (var msgId in SelectedChatTab.MessageIds)
                        {
                            if (ChatManager.Messages.TryGetValue(msgId, out var msg))
                            {
                                DrawChatMessage(msg);
                            }
                        }
                    }

                    if (ImGui.GetScrollMaxY() != ImGui.GetScrollY())
                    {
                        shouldScrollToBottom = false;
                    }

                    if (shouldScrollToBottom)
                    {
                        ImGui.SetScrollHereY();
                    }
                }
            }
            ImGui.EndChild();
        }

        private static void DrawChatMessage(ChatMessage msg)
        {
            ImGui.BeginGroup();

            ImGui.PushStyleColor(ImGuiCol.Text, GetChannelColor(msg.Channel));
            ImGui.TextUnformatted($"[{GetChannelNameShort(msg.Channel)}]");
            ImGui.PopStyleColor();
            ImGui.SameLine();

            if (Settings.Instance.WindowSettings.ChatWindow.CompactMode)
            {
                DrawChatTime(msg);
                ImGui.SameLine();

                DrawChatSenderName(msg);
                ImGui.SameLine();
            }
            else
            {
                DrawChatSenderName(msg);
                if (Settings.Instance.WindowSettings.ChatWindow.ShowTime)
                {
                    ImGui.SameLine();
                }

                DrawChatTime(msg);
            }

            if (msg.Msg.MsgType == ChitChatMsgType.ChatMsgPictureEmoji)
            {
                ImGui.TextUnformatted($"[Image({msg.Msg.PictureEmoji.ConfigId})]");
            }
            else if (msg.Msg.MsgType == ChitChatMsgType.ChatMsgTextMessage)
            {
                ImGui.PushTextWrapPos(0f);
                ImGui.TextUnformatted(msg.Msg.MsgText);
                ImGui.PopTextWrapPos();
            }

            ImGui.EndGroup();
        }

        private static void DrawChatSenderName(ChatMessage msg)
        {
            ChatManager.Senders.TryGetValue(msg.SenderId, out var sender);
            ImGui.PushStyleColor(ImGuiCol.TextLink, new Vector4(0.40f, 0.70f, 1.00f, 1.00f));
            if (ImGui.TextLink($"[{sender.Info.Name}]##ChatMsgName_{msg.GetHashCode().ToString()}"))
            {
                
            }
            ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem(ImGuiPopupFlags.MouseButtonLeft))
            {
                if (ImGui.MenuItem("Copy Name"))
                {
                    ImGui.SetClipboardText(sender.Info.Name);
                }
                ImGui.SetItemTooltip($"Copies [ {sender.Info.Name} ] to the clipboard.");

                if (ImGui.MenuItem("Copy UID"))
                {
                    ImGui.SetClipboardText(sender.Info.CharId.ToString());
                }
                ImGui.SetItemTooltip($"Copies [ {sender.Info.CharId} ] to the clipboard.");

                if (ImGui.MenuItem("Block User"))
                {
                    ChatManager.BlockUser(sender);
                }
                ImGui.SetItemTooltip($"Blocks this users ({sender.Info.Name}) messages from showing in your ZDPS chat.");

                ImGui.EndPopup();
            }
        }

        private static void DrawChatTime(ChatMessage msg)
        {
            if (Settings.Instance.WindowSettings.ChatWindow.ShowTime)
            {
                var timeStr = Settings.Instance.WindowSettings.ChatWindow.ShowTimeAsXAgo ?
                    Utils.FormatTimeAgo(msg.TimeStamp) :
                    msg.TimeStamp.ToString("HH:mm");

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.60f, 0.65f, 0.75f, 1.00f));
                ImGui.TextUnformatted(timeStr);
                ImGui.PopStyleColor();
            }
        }

        public static bool DrawChatTabConfig(ChatTabConfig config)
        {
            var haveFiltersChanged = false;
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Name:");
            ImGui.SameLine();
            ImGui.InputText("##Name", ref config.Name, 256);

            ImGui.SeparatorText("Channels");

            if (ImGui.BeginTable("ChannelTable", 9, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableNextRow();
                haveFiltersChanged |= ChannelToggle(config, "World", ChitChatChannelType.ChannelWorld);
                haveFiltersChanged |= ChannelToggle(config, "Local", ChitChatChannelType.ChannelScene);
                haveFiltersChanged |= ChannelToggle(config, "Group", ChitChatChannelType.ChannelGroup);
                haveFiltersChanged |= ChannelToggle(config, "Team", ChitChatChannelType.ChannelTeam);

                ImGui.TableNextRow();
                haveFiltersChanged |= ChannelToggle(config, "Private", ChitChatChannelType.ChannelPrivate);
                haveFiltersChanged |= ChannelToggle(config, "Union", ChitChatChannelType.ChannelUnion);
                haveFiltersChanged |= ChannelToggle(config, "System", ChitChatChannelType.ChannelSystem);
                haveFiltersChanged |= ChannelToggle(config, "Top Notice", ChitChatChannelType.ChannelTopNotice);

                ImGui.EndTable();
            }

            ImGui.SeparatorText("Level");
            var minLevel = config.OverLevel;
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Min Level:");
            ImGui.SameLine();
            haveFiltersChanged |= ImGui.SliderInt("##MinLevel", ref minLevel, 1, 60);
            config.OverLevel = minLevel;

            ImGui.SeparatorText("Regex Filters");

            var containsFilter = config.Contains;
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Show If Matches:");
            ImGui.SameLine();
            haveFiltersChanged |= ImGui.InputText("##ShowIfMatches", ref containsFilter, 1024);
            config.Contains = containsFilter;

            var doesNotContainFilter = config.DoesNotContain;
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Hide If Matches: ");
            ImGui.SameLine();
            haveFiltersChanged |= ImGui.InputText("##HideIfMatches", ref doesNotContainFilter, 1024);
            config.DoesNotContain = doesNotContainFilter;

            return haveFiltersChanged;
        }

        private static bool ChannelToggle(ChatTabConfig config, string name, ChitChatChannelType channel)
        {
            var channelEnabled = config.Channels.Contains(channel);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"{name}:");
            ImGui.TableNextColumn();
            if (ImGui.Checkbox($"##{name}", ref channelEnabled))
            {
                if (channelEnabled)
                {
                    config.Channels.Add(channel);
                }
                else
                {
                    config.Channels.Remove(channel);
                }

                return true;
            }

            return false;
        }

        private static string GetChannelNameShort(ChitChatChannelType chan)
        {
            var name = chan switch
            {
                ChitChatChannelType.ChannelNull => "Null",
                ChitChatChannelType.ChannelWorld => "World",
                ChitChatChannelType.ChannelScene => "Local",
                ChitChatChannelType.ChannelTeam => "Team",
                ChitChatChannelType.ChannelUnion => "Union",
                ChitChatChannelType.ChannelPrivate => "Private",
                ChitChatChannelType.ChannelGroup => "Group",
                ChitChatChannelType.ChannelTopNotice => "Notice",
                ChitChatChannelType.ChannelSystem => "System",
                _ => "Unknown"
            };

            return name;
        }

        private static Vector4 GetChannelColor(ChitChatChannelType chan)
        {
            var color = chan switch
            {
                ChitChatChannelType.ChannelNull => new Vector4(0.50f, 0.50f, 0.50f, 1.0f),
                ChitChatChannelType.ChannelWorld => new Vector4(0.39f, 0.78f, 1.00f, 1.0f),
                ChitChatChannelType.ChannelScene => new Vector4(0.56f, 0.93f, 0.56f, 1.0f),
                ChitChatChannelType.ChannelTeam => new Vector4(1.00f, 0.71f, 0.76f, 1.0f),
                ChitChatChannelType.ChannelUnion => new Vector4(1.00f, 0.84f, 0.00f, 1.0f),
                ChitChatChannelType.ChannelPrivate => new Vector4(1.00f, 0.63f, 1.00f, 1.0f),
                ChitChatChannelType.ChannelGroup => new Vector4(0.68f, 0.85f, 0.90f, 1.0f),
                ChitChatChannelType.ChannelTopNotice => new Vector4(1.00f, 0.55f, 0.00f, 1.0f),
                ChitChatChannelType.ChannelSystem => new Vector4(1.00f, 0.39f, 0.28f, 1.0f),
                _ => new Vector4(0.78f, 0.78f, 0.78f, 1.0f)
            };

            return color;
        }
    }

    public class ChatWindowSettings : WindowSettingsBase
    {
        public float BackgroundOpacity { get; set; } = 0.5f;
        public long LastSelectedTabId { get; set; } = -1;
        public bool CompactMode { get; set; } = true;
        public bool ShowTime { get; set; } = true;
        public bool ShowTimeAsXAgo { get; set; } = true;
        public List<ChatTabConfig> ChatTabs { get; set; } = [];
    }
}
