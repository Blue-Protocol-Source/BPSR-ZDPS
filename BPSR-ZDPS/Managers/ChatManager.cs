using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.Chat;
using BPSR_ZDPSLib;
using System.Collections.Concurrent;
using System.IO.Enumeration;
using Zproto;
using static Zproto.ChitChatNtf.Types;

namespace BPSR_ZDPS.Managers
{
    public class ChatManager
    {
        public static ConcurrentDictionary<long, ChatMessage> Messages = [];
        public static ConcurrentDictionary<long, User> Senders = [];
        public static List<ChatTab> ChatTabs = [];

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
                var chatMsg = new ChatMessage(msg.VRequest.ChatMsg.MsgInfo, msg.VRequest.ChannelType, msg.VRequest.ChatMsg.SendCharInfo.CharId, msg.VRequest.ChatMsg.Timestamp);
                User chatUser;

                if (Senders.TryGetValue(msg.VRequest.ChatMsg.SendCharInfo.CharId, out var sender))
                {
                    sender.Info = msg.VRequest.ChatMsg.SendCharInfo;
                    sender.NumSentMessages++;
                    chatUser = sender;
                }
                else
                {
                    var chatSender = new User(msg.VRequest.ChatMsg.SendCharInfo);
                    Senders.TryAdd(msg.VRequest.ChatMsg.SendCharInfo.CharId, chatSender);
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
                                        tab.MessageIds.Remove(msgId);
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
                bool isTextMsg = msg.Msg.MsgType == ChitChatMsgType.ChatMsgTextMessage;
                bool isFromChannel = tab.Config.Channels.Contains(msg.Channel);
                bool isOverLevel = sender.Info.Level >= tab.Config.OverLevel;

                if (!isTextMsg)
                {
                    return isFromChannel && isOverLevel;
                }

                bool matchesContains = string.IsNullOrWhiteSpace(tab.Config.Contains) || FileSystemName.MatchesSimpleExpression(tab.Config.Contains, msg.Msg.MsgText);
                bool matchesDoesNotContain = string.IsNullOrWhiteSpace(tab.Config.DoesNotContain) || !FileSystemName.MatchesSimpleExpression(tab.Config.DoesNotContain, msg.Msg.MsgText);

                return isFromChannel && isOverLevel && matchesContains && matchesDoesNotContain;
            }

            return false;
        }

        public static void RefilterChatTab(ChatTab tab)
        {
            lock (tab.MessageIds)
            {
                tab.MessageIds.Clear();
                foreach (var msg in Messages)
                {
                    if (IsFilteredForChatTab(tab, msg.Value))
                    {
                        tab.MessageIds.Add(msg.Key);
                    }
                }
            }
        }
    }
}
