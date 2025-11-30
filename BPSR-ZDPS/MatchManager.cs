using BPSR_DeepsLib;
using BPSR_ZDPS.DataTypes;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS
{
    public static class MatchManager
    {
        public static void ProcessEnterMatchResult(MatchNtf.Types.EnterMatchResultNtf vData, ExtraPacketData extraData)
        {
            if (vData.VRequest.MatchInfo.MatchStatus == EMatchStatus.WaitReady)
            {
                // The match queue has "popped" and is now waiting for everyone to accept it
                NotificationAlertManager.PlayNotifyAudio(NotificationAlertManager.NotificationType.Matchmake);
            }
        }

        public static void ProcessCancelMatchResult(MatchNtf.Types.CancelMatchResultNtf vData, ExtraPacketData extraData)
        {
            NotificationAlertManager.StopNotifyAudio();
        }

        public static void ProcessMatchReadyStatus(MatchNtf.Types.MatchReadyStatusNtf vData, ExtraPacketData extraData)
        {
            foreach (var matchPlayerInfo in vData.VRequest.MatchPlayerInfo)
            {
                // Unless player moves/teleports after starting ZDPS, the AppState.PlayerUID will be 0 initially
                if (matchPlayerInfo.CharId == AppState.PlayerUID)
                {
                    if (matchPlayerInfo.ReadyStatus == EMatchReadyStatus.Ready)
                    {
                        NotificationAlertManager.StopNotifyAudio();
                    }
                }
            }
        }
    }
}
