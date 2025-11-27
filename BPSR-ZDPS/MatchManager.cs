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
        static string DEFAULT_NOTIFICATION_AUDIO_FILE = Path.Combine(Utils.DATA_DIR_NAME, "Audio", "LetsDoThis.wav");

        static AudioFileReader? NotificationAudioFileReader = null;
        static WaveOutEvent? NotificationWaveOutEvent = null;
        static bool ShouldStop = false;

        public static void ProcessEnterMatchResult(MatchNtf.Types.EnterMatchResultNtf vData, ExtraPacketData extraData)
        {
            if (vData.VRequest.MatchInfo.MatchStatus == EMatchStatus.WaitReady)
            {
                // The match queue has "popped" and is now waiting for everyone to accept it
                PlayNotifyAudio();
            }
        }

        public static void ProcessCancelMatchResult(MatchNtf.Types.CancelMatchResultNtf vData, ExtraPacketData extraData)
        {
            if (NotificationWaveOutEvent != null)
            {
                ShouldStop = true;
                NotificationWaveOutEvent.Stop();
            }
        }

        private static void NotificationWaveOutEvent_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (NotificationWaveOutEvent != null)
            {
                if (ShouldStop == false && Settings.Instance.LoopNotificationSoundOnMatchmake)
                {
                    // Keep looping the audio until actually requested to stop
                    NotificationAudioFileReader.Seek(0, SeekOrigin.Begin);
                    NotificationWaveOutEvent.Play();
                    return;
                }

                NotificationWaveOutEvent.PlaybackStopped -= NotificationWaveOutEvent_PlaybackStopped;
                NotificationWaveOutEvent.Dispose();
            }

            if (NotificationAudioFileReader != null)
            {
                NotificationAudioFileReader.Dispose();
            }

            NotificationWaveOutEvent = null;
            NotificationAudioFileReader = null;
        }

        public static void ProcessMatchReadyStatus(MatchNtf.Types.MatchReadyStatusNtf vData, ExtraPacketData extraData)
        {
            foreach (var matchPlayerInfo in vData.VRequest.MatchPlayerInfo)
            {
                if (matchPlayerInfo.CharId == AppState.PlayerUID)
                {
                    if (matchPlayerInfo.ReadyStatus == EMatchReadyStatus.Ready)
                    {
                        if (NotificationWaveOutEvent != null)
                        {
                            ShouldStop = true;
                            NotificationWaveOutEvent.Stop();
                        }
                    }
                }
            }
        }

        public static void PlayNotifyAudio()
        {
            if (Settings.Instance.PlayNotificationSoundOnMatchmake)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.MatchmakeNotificationSoundPath) && File.Exists(Settings.Instance.MatchmakeNotificationSoundPath))
                {
                    NotificationAudioFileReader = new AudioFileReader(Settings.Instance.MatchmakeNotificationSoundPath);
                }
                else
                {
                    if (File.Exists(DEFAULT_NOTIFICATION_AUDIO_FILE))
                    {
                        NotificationAudioFileReader = new AudioFileReader(DEFAULT_NOTIFICATION_AUDIO_FILE);
                    }
                    else
                    {
                        Log.Error("Unable to locate Default Notification Audio file for MatchManager playback!");
                        return;
                    }
                }
                ShouldStop = false;

                if (Settings.Instance.MatchmakeNotificationVolume > 1.0f)
                {
                    // Only go through using this sampler if the volume was changed above "100%" as it incurs a performance penalty to runtime increase beyond 1.0
                    var volumeSampleProvider = new VolumeSampleProvider(NotificationAudioFileReader);
                    volumeSampleProvider.Volume = Settings.Instance.MatchmakeNotificationVolume;

                    NotificationWaveOutEvent = new WaveOutEvent();
                    NotificationWaveOutEvent.PlaybackStopped += NotificationWaveOutEvent_PlaybackStopped;

                    NotificationWaveOutEvent.Init(volumeSampleProvider);
                }
                else
                {
                    NotificationWaveOutEvent = new WaveOutEvent();
                    NotificationWaveOutEvent.PlaybackStopped += NotificationWaveOutEvent_PlaybackStopped;
                    NotificationWaveOutEvent.Init(NotificationAudioFileReader);
                    NotificationWaveOutEvent.Volume = Settings.Instance.MatchmakeNotificationVolume;
                }
                
                NotificationWaveOutEvent.Play();
            }
        }
    }
}
