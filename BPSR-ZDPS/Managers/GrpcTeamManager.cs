using BPSR_DeepsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS
{
    public static class GrpcTeamManager
    {
        public static void ProcessNoticeUpdateTeamInfo(GrpcTeamNtf.Types.NoticeUpdateTeamInfo vData, ExtraPacketData extraData)
        {

        }

        public static void ProcessNoticeUpdateTeamMemberInfo(GrpcTeamNtf.Types.NoticeUpdateTeamMemberInfo vData, ExtraPacketData extraData)
        {

        }

        public static void ProcessNotifyJoinTeam(GrpcTeamNtf.Types.NotifyJoinTeam vData, ExtraPacketData extraData)
        {
            foreach (var member in vData.VRequest.MemberData)
            {
                long uuid = Utils.EntityIdToUuid(member.CharId, (long)EEntityType.EntChar, false, false);

                var cached = EntityCache.Instance.GetOrCreate(uuid);
                if (cached != null)
                {
                    // Have to do a lot of null checks because bots are actually missing a lot of fields in this event

                    if (member.SocialData.BasicData != null)
                    {
                        string name = member.SocialData.BasicData.Name;
                        if (!string.IsNullOrEmpty(name))
                        {
                            cached.Name = member.SocialData.BasicData.Name;
                        }

                        int level = member.SocialData.BasicData.Level;
                        if (level > 0)
                        {
                            cached.Level = level;
                        }
                    }

                    if (member.SocialData.UserAttrData != null)
                    {
                        long abilityscore = member.SocialData.UserAttrData.FightPoint;
                        if (abilityscore > 0)
                        {
                            cached.AbilityScore = (int)abilityscore;
                        }
                    }

                    if (member.SocialData.ProfessionData != null)
                    {
                        int professionId = member.SocialData.ProfessionData.ProfessionId;
                        if (professionId > 0)
                        {
                            cached.ProfessionId = professionId;
                        }
                    }
                }
            }
        }

        public static void ProcessNotifyTeamActivityState(GrpcTeamNtf.Types.NotifyTeamActivityState vData, ExtraPacketData extraData)
        {
            if (vData.VRequest.State.State == ETeamActivityState.EteamActivityVoting)
            {
                // The Voting UI has just opened, check if we're the owner or a member
                // An owner automatically accepts it so no need to alert them
                if (vData.VRequest.State.AssignSceneParams.CreatorCharId == AppState.PlayerUID)
                {
                    // Current player is the creator, so skip the notification alert to accept (we call to stop just to be safe)
                    System.Diagnostics.Debug.WriteLine("Current Player is TeamActivity creator");
                    NotificationAlertManager.StopNotifyAudio();
                }
                else
                {
                    // Alert the player they need to accept the activity
                    System.Diagnostics.Debug.WriteLine("Current Player is TeamActivity member and needs to vote");
                    NotificationAlertManager.PlayNotifyAudio(NotificationAlertManager.NotificationType.Matchmake);
                }
            }
            else if (vData.VRequest.State.State == ETeamActivityState.EteamActivityNo)
            {
                // The activity vote has ended, there is no information when the vote ends to know why it ended each TeamActivityVoteResult must be checked
                // Ensure notification alerts are stopped
                System.Diagnostics.Debug.WriteLine("ProcessNotifyTeamActivityState State is No, activity vote state ended");
                NotificationAlertManager.StopNotifyAudio();
            }
        }

        public static void ProcessTeamActivityResult(GrpcTeamNtf.Types.TeamActivityResult vData, ExtraPacketData extraData)
        {
            
        }

        public static void ProcessTeamActivityListResult(GrpcTeamNtf.Types.TeamActivityListResult vData, ExtraPacketData extraData)
        {
            
        }

        public static void ProcessTeamActivityVoteResult(GrpcTeamNtf.Types.TeamActivityVoteResult vData, ExtraPacketData extraData)
        {
            if (vData.VRequest.VCharId == AppState.PlayerUID)
            {
                if (vData.VRequest.Code == ETeamVoteRet.Agree)
                {
                    // The player has accepted the activity, stop the notification alert
                    NotificationAlertManager.StopNotifyAudio();
                }
                else
                {
                    // The player has "voted" but not to accept, stop the notification alert
                    // TODO: Maybe play a unique sound based on the reason they did not accept
                    NotificationAlertManager.StopNotifyAudio();
                }
            }
        }

        public static void ProcessNotifyCharMatchResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {

        }

        public static void ProcessNotifyTeamMatchResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {

        }

        public static void ProcessNotifyCharAbortMatch(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {

        }

        public static void ProcessNotifyTeamEnterErr(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {

        }
    }
}
