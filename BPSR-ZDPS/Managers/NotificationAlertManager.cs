using BPSR_ZDPS.DataTypes;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class NotificationAlertManager
    {
        static string DEFAULT_NOTIFICATION_AUDIO_FILE = Path.Combine(Utils.DATA_DIR_NAME, "Audio", "LetsDoThis.wav");

        static AudioFileReader? NotificationAudioFileReader = null;
        static WaveOutEvent? NotificationWaveOutEvent = null;
        static NotificationType NotificationEventType = NotificationType.Generic;
        static bool ShouldStop = false;

        public enum NotificationType : int
        {
            Generic = 0,
            Matchmake = 1,
            ReadyCheck = 2
        }

        public static void PlayNotifyAudio(NotificationType notificationType = NotificationType.Generic)
        {
            string audioPath = DEFAULT_NOTIFICATION_AUDIO_FILE;
            float volumeScale = 1.0f;
            NotificationEventType = notificationType;

            switch (notificationType)
            {
                case NotificationType.Matchmake:
                    if (!Settings.Instance.PlayNotificationSoundOnMatchmake)
                    {
                        return;
                    }

                    if (!string.IsNullOrEmpty(Settings.Instance.MatchmakeNotificationSoundPath) && File.Exists(Settings.Instance.MatchmakeNotificationSoundPath))
                    {
                        audioPath = Settings.Instance.MatchmakeNotificationSoundPath;
                    }

                    volumeScale = Settings.Instance.MatchmakeNotificationVolume;
                    break;
                case NotificationType.ReadyCheck:
                    if (!Settings.Instance.PlayNotificationSoundOnReadyCheck)
                    {
                        return;
                    }

                    if (!string.IsNullOrEmpty(Settings.Instance.ReadyCheckNotificationSoundPath) && File.Exists(Settings.Instance.ReadyCheckNotificationSoundPath))
                    {
                        audioPath = Settings.Instance.ReadyCheckNotificationSoundPath;
                    }

                    volumeScale = Settings.Instance.ReadyCheckNotificationVolume;
                    break;
            }

            if (audioPath == DEFAULT_NOTIFICATION_AUDIO_FILE)
            {
                if (!File.Exists(DEFAULT_NOTIFICATION_AUDIO_FILE))
                {
                    Log.Error("Unable to locate Default Notification Audio file for NotificationAlertManager playback!");
                    return;
                }
            }
            else if (string.IsNullOrWhiteSpace(audioPath))
            {
                Log.Error("No audio file path was specified for NotificationAlertManager.PlayNotifyAudio!");
                return;
            }

            NotificationAudioFileReader = new AudioFileReader(audioPath);
            ShouldStop = false;

            if (volumeScale > 1.0f)
            {
                // Only go through using this sampler if the volume was changed above "100%" as it incurs a performance penalty to runtime increase beyond 1.0
                var volumeSampleProvider = new VolumeSampleProvider(NotificationAudioFileReader);
                volumeSampleProvider.Volume = volumeScale;

                NotificationWaveOutEvent = new WaveOutEvent();
                NotificationWaveOutEvent.PlaybackStopped += NotificationWaveOutEvent_PlaybackStopped;

                NotificationWaveOutEvent.Init(volumeSampleProvider);
            }
            else
            {
                NotificationWaveOutEvent = new WaveOutEvent();
                NotificationWaveOutEvent.PlaybackStopped += NotificationWaveOutEvent_PlaybackStopped;
                NotificationWaveOutEvent.Init(NotificationAudioFileReader);
                NotificationWaveOutEvent.Volume = volumeScale;
            }

            NotificationWaveOutEvent.Play();
        }

        public static void StopNotifyAudio()
        {
            ShouldStop = true;
            if (NotificationWaveOutEvent != null)
            {
                NotificationWaveOutEvent.Stop();
            }
        }

        private static void NotificationWaveOutEvent_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            bool shouldLoop = false;
            switch (NotificationEventType)
            {
                case NotificationType.Generic:
                    shouldLoop = false;
                    break;
                case NotificationType.Matchmake:
                    shouldLoop = Settings.Instance.LoopNotificationSoundOnMatchmake;
                    break;
                case NotificationType.ReadyCheck:
                    shouldLoop = Settings.Instance.LoopNotificationSoundOnReadyCheck;
                    break;
            }

            if (NotificationWaveOutEvent != null)
            {
                // Only allow looping to occur if we know the PlayerUID
                if (ShouldStop == false && shouldLoop && AppState.PlayerUID != 0)
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
    }
}
