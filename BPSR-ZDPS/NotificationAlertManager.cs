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
        static bool ShouldStop = false;

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
    }
}
