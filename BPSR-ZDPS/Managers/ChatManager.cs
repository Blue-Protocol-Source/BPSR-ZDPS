using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.Chat;
using BPSR_ZDPSLib;
using System.Collections.Concurrent;
using Zproto;
using static Zproto.ChitChatNtf.Types;

namespace BPSR_ZDPS.Managers
{
    public class ChatManager
    {
        public static ConcurrentDictionary<long, ChatMessage> Messages = [];
        public static ConcurrentDictionary<long, User> Senders = [];
        public static List<ChatTab> ChatTabs = [];
        private static long SessionMsgIdSrc = 0;

        public static event Action<User, ChatMessage, NotifyNewestChitChatMsgs> OnChatMessage;

        private static Dictionary<ChitChatChannelType, ConcurrentQueue<long>> ChatChannelMsgIds = new Dictionary<ChitChatChannelType, ConcurrentQueue<long>>();

        static ChatManager()
        {
            //LoadChatTabs();
            //ApplyNewSettings(Settings.Instance.Chat);
        }

        public static void ProcessChatMessage(ReadOnlySpan<byte> span, ExtraPacketData extraData)
        {
            if (!AppState.IsChatEnabled)
            {
                return;
            }

            var msg = NotifyNewestChitChatMsgs.Parser.ParseFrom(span);
            if (msg != null)
            {
                // Override with a session global id as the games ids are only unique to a channel and session
                msg.VRequest.ChatMsg.MsgId = SessionMsgIdSrc++;

                var chatMsg = new ChatMessage(msg.VRequest.ChatMsg.MsgInfo, msg.VRequest.ChannelType, msg.VRequest.ChatMsg.SendCharInfo.CharID, msg.VRequest.ChatMsg.Timestamp);
                User chatUser;

                if (Senders.TryGetValue(msg.VRequest.ChatMsg.SendCharInfo.CharID, out var sender))
                {
                    sender.Info = msg.VRequest.ChatMsg.SendCharInfo;
                    sender.NumSentMessages++;
                    chatUser = sender;
                }
                else
                {
                    var chatSender = new User(msg.VRequest.ChatMsg.SendCharInfo);
                    Senders.TryAdd(msg.VRequest.ChatMsg.SendCharInfo.CharID, chatSender);
                    chatUser = chatSender;
                }

                if (Messages.TryAdd(msg.VRequest.ChatMsg.MsgId, chatMsg))
                {
                    lock (ChatChannelMsgIds)
                    {
                        if (ChatChannelMsgIds.TryGetValue(msg.VRequest.ChannelType, out var chanMsgIds))
                        {
                            chanMsgIds.Enqueue(msg.VRequest.ChatMsg.MsgId);

                            if (chanMsgIds.Count > Settings.Instance.Chat.MaxChatHistory)
                            {
                                if (chanMsgIds.TryDequeue(out var msgId))
                                {
                                    Messages.Remove(msgId, out var _);

                                    foreach (var tab in ChatTabs)
                                    {
                                        lock (tab.MessageIds)
                                        {
                                            tab.MessageIds.Remove(msgId);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var newChanMsgIds = new ConcurrentQueue<long>();
                            newChanMsgIds.Enqueue(msg.VRequest.ChatMsg.MsgId);
                            ChatChannelMsgIds.Add(msg.VRequest.ChannelType, newChanMsgIds);
                        }
                    }
                }

                OnChatMessage?.Invoke(chatUser, chatMsg, msg);

                lock (ChatTabs)
                {
                    foreach (var tab in ChatTabs)
                    {
                        if (IsFilteredForChatTab(tab, chatMsg))
                        {
                            lock (tab.MessageIds)
                            {
                                tab.MessageIds.Add(msg.VRequest.ChatMsg.MsgId);
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveMessagesOverCap(int cap)
        {
            lock (ChatChannelMsgIds)
            {
                foreach (var chatChannel in ChatChannelMsgIds)
                {
                    while (chatChannel.Value.Count() > cap)
                    {
                        if (chatChannel.Value.TryDequeue(out var msgId))
                        {
                            Messages.Remove(msgId, out var _);
                        }
                    }
                }
            }

            foreach (var tab in ChatTabs)
            {
                RefilterChatTab(tab);
            }
        }

        public static void AddChatTab(ChatTabConfig config)
        {
            var chatTab = new ChatTab(config);
            lock (ChatTabs)
            {
                ChatTabs.Add(chatTab);
            }
        }

        public static void RemoveChatTab(ChatTabConfig config)
        {
            lock (ChatTabs)
            {
                ChatTabs.RemoveAll(x => x.Config.Id == config.Id);
            }
        }

        public static bool IsFilteredForChatTab(ChatTab tab, ChatMessage msg)
        {
            if (Senders.TryGetValue(msg.SenderId, out var sender))
            {
                bool isBlocked = IsUserBlocked(msg.SenderId);
                if (isBlocked)
                {
                    return false;
                }

                bool isTextMsg = msg.Msg.MsgType == ChitChatMsgType.ChatMsgTextMessage;
                bool isFromChannel = tab.Config.Channels.Contains(msg.Channel);
                bool isOverLevel = sender.Info.Level >= tab.Config.OverLevel;

                if (!isTextMsg)
                {
                    if (msg.Msg.MsgType == ChitChatMsgType.ChatMsgPictureEmoji && Settings.Instance.WindowSettings.ChatWindow.HideStickers)
                    {
                        return false;
                    }

                    return isFromChannel && isOverLevel;
                }

                bool matchesContains = string.IsNullOrWhiteSpace(tab.Config.Contains) || Utils.SafeRegexIsMatch(msg.Msg.MsgText, tab.Config.Contains);
                bool matchesDoesNotContain = string.IsNullOrWhiteSpace(tab.Config.DoesNotContain) || !Utils.SafeRegexIsMatch(msg.Msg.MsgText, tab.Config.DoesNotContain);

                return isFromChannel && isOverLevel && matchesContains && matchesDoesNotContain;
            }

            return false;
        }

        public static void RefilterChatTab(ChatTab tab)
        {
            lock (tab.MessageIds)
            {
                tab.MessageIds.Clear();
                foreach (var msg in Messages.OrderBy(x => x.Value.TimeStamp))
                {
                    if (IsFilteredForChatTab(tab, msg.Value))
                    {
                        tab.MessageIds.Add(msg.Key);
                    }
                }
            }
        }

        public static void RefilterAllTabs()
        {
            foreach (var tab in ChatTabs)
            {
                RefilterChatTab(tab);
            }
        }

        public static void BlockUser(User user)
        {
            var blockedUser = new UserBlock()
            {
                ID = user.Info.CharID,
                Name = user.Info.Name,
                BlockedAt = DateTime.Now
            };

            Settings.Instance.Chat.BlockedUsers.TryAdd(user.Info.CharID, blockedUser);
        }

        public static void UnblockUser(long userId)
        {
            Settings.Instance.Chat.BlockedUsers.TryRemove(userId, out var blockedUser);
        }

        public static bool IsUserBlocked(long userId)
        {
            bool isBlocked = Settings.Instance.Chat.BlockedUsers.ContainsKey(userId);
            return isBlocked;
        }
    }
}
