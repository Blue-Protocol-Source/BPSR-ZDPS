namespace BPSR_ZDPSLib.ServiceMethods
{
    public enum ChitChatProxy
    {
        SendChitChatMsg = 0x1, // 1
        GetChipChatRecords = 0x2, // 2
        GetPrivateChatTargets = 0x3, // 3
        CreatePrivateChatSession = 0x4, // 4
        DeletePrivateChatSession = 0x5, // 5
        SetPrivateChatHasRead = 0x6, // 6
        PrivateChatTargetTop = 0x7, // 7
        PrivateChatTargetBlock = 0x8, // 8
        PrivateChatBlockList = 0x9, // 9
        SetWorldChatChannelId = 0xA, // 10
        GetWorldChatChannelId = 0xB, // 11
        QueryChatMute = 0xC, // 12
        ArkShareWithTencent = 0xF, // 15
        GetArkJsonWithTencent = 0x10, // 16
        SetNewbieChatChannelId = 0x12, // 18
        GetNewbieChatChannelId = 0x13, // 19

    }
}
