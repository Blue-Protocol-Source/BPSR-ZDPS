namespace BPSR_ZDPSLib.ServiceMethods;

public enum GrpcTeamNtf
{
    NoticeUpdateTeamInfo = 0x1, // 1
    NoticeUpdateTeamMemberInfo = 0x2, // 2
    NotifyJoinTeam = 0x3, // 3
    NotifyLeaveTeam = 0x4, // 4
    NotifyApplyJoin = 0x5, // 5
    NotifyInvitation = 0x6, // 6
    NotifyRefuseInvite = 0x7, // 7
    NotifyLeaderApplyListSize = 0x8, // 8
    NotifyApplyBeLeader = 0x9, // 9
    NotifyRejectApplicant = 0xA, // 10
    NotifyBeTransferLeader = 0xB, // 11
    NotifyRefuseBeTransferLeader = 0xC, // 12
    NoticeTeamDissolve = 0xD, // 13
    NotifyTeamActivityState = 0xE, // 14
    TeamActivityResult = 0xF, // 15
    TeamActivityListResult = 0x10, // 16
    TeamActivityVoteResult = 0x11, // 17
    NotifyCharMatchResult = 0x12, // 18
    NotifyTeamMatchResult = 0x13, // 19
    NotifyCharAbortMatch = 0x14, // 20
    UpdateTeamMemBeCall = 0x15, // 21
    NotifyTeamMemBeCall = 0x16, // 22
    NotifyTeamMemBeCallResult = 0x17, // 23
    NotifyTeamEnterErr = 0x18, // 24
    NotifyTeamMemMicrophoneStatusChange = 0x19, // 25
    NotifyTeamMemsSpeakStatusChange = 0x1A, // 26
    NotifyTeamMemVoiceIdChange = 0x1B, // 27
    NotifyTeamChangeMemberType = 0x1C, // 28
    NotifyTeamGroupUpdate = 0x1D, // 29
    NotifyInviteJoinDungeons = 0x1E, // 30
    NotifyCountDownStart = 0x1F, // 31
    NotifyTeamJoinApplyChanged = 0x20, // 32
}