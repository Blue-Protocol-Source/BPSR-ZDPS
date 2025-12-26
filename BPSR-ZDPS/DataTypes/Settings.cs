using BPSR_ZDPS.DataTypes.External;
using BPSR_ZDPS.Windows;
using Newtonsoft.Json;
using System.Numerics;

namespace BPSR_ZDPS.DataTypes;

public class Settings
{
    public static Settings Instance = new();
    private static string SETTINGS_FILE_NAME = "Settings.json";

    public int Version { get; set; } = 0;
    public string NetCaptureDeviceName { get; set; } = "";
    public bool NormalizeMeterContributions { get; set; } = true;
    public bool UseShortWidthNumberFormatting { get; set; } = true;
    public bool ColorClassIconsByRole { get; set; } = true;
    public bool ShowSkillIconsInDetails { get; set; } = true;
    public bool OnlyShowDamageContributorsInMeters { get; set; } = false;
    public bool UseAutomaticWipeDetection { get; set; } = true;
    public bool SkipTeleportStateCheckInAutomaticWipeDetection { get; set; } = false;
    public bool SplitEncountersOnNewPhases { get; set; } = true;
    public bool DisplayTruePerSecondValuesInMeters { get; set; } = false;
    public float WindowOpacity = 1.0f;
    public float MeterBarScale = 1.0f;
    public bool UseDatabaseForEncounterHistory { get; set; } = true;
    public int DatabaseRetentionPolicyDays { get; set; } = 0;
    public bool LimitEncounterBuffTrackingWithoutDatabase { get; set; } = false;
    public bool LogToFile { get; set; } = true;
    public EGameCapturePreference GameCapturePreference { get; set; } = EGameCapturePreference.Auto;
    public string GameCaptureCustomExeName { get; set; } = "";
    public bool PlayNotificationSoundOnMatchmake { get; set; } = false;
    public string MatchmakeNotificationSoundPath { get; set; } = "";
    public bool LoopNotificationSoundOnMatchmake { get; set; } = false;
    public float MatchmakeNotificationVolume { get; set;} = 1.0f;
    public bool PlayNotificationSoundOnReadyCheck { get; set; } = false;
    public string ReadyCheckNotificationSoundPath { get; set; } = "";
    public bool LoopNotificationSoundOnReadyCheck { get; set; } = false;
    public float ReadyCheckNotificationVolume { get; set; } = 1.0f;
    public bool SaveEncounterReportToFile { get; set; } = false;
    public int ReportFileRetentionPolicyDays { get; set; } = 0;
    public int MinimumPlayerCountToCreateReport { get; set; } = 0;
    public bool WebhookReportsEnabled { get; set; } = false;
    public EWebhookReportsMode WebhookReportsMode { get; set; } = EWebhookReportsMode.Discord;
    public string WebhookReportsDeduplicationServerHost { get; set; } = "https://zdps-webfunc.vercel.app";
    public string WebhookReportsDiscordUrl { get; set; } = "";
    public string WebhookReportsCustomUrl { get; set; } = "";
    public bool CheckForZDPSUpdatesOnStartup { get; set; } = false;
    public string LatestZDPSVersionCheckURL { get; set; } = "https://raw.githubusercontent.com/Blue-Protocol-Source/BPSR-ZDPS-Metadata/master/LatestVersion.txt";
    public string ZDPSWebsiteURL { get; set; } = "https://github.com/Blue-Protocol-Source/BPSR-ZDPS";

    // Settings specific to External components
    public SettingsExternal External { get; set; } = new();

    // Settings specific to an individual window
    public WindowSettings WindowSettings { get; set; } = new();

    public uint HotkeysEncounterReset { get; set; }
    public uint HotkeysPinnedWindowClickthrough { get; set; }

    public void Apply()
    {
        MessageManager.NetCaptureDeviceName = NetCaptureDeviceName;

        
    }

    public void ApplyHotKeys(MainWindow mainWindow)
    {
        if (HotkeysEncounterReset > 0)
        {
            HotKeyManager.RegisterKey("EncounterReset", mainWindow.CreateNewEncounter, HotkeysEncounterReset);
        }

        if (HotkeysPinnedWindowClickthrough > 0)
        {
            HotKeyManager.RegisterKey("PinnedWindowClickthrough", mainWindow.ToggleMouseClickthrough, HotkeysPinnedWindowClickthrough);
        }
    }

    public static void Load()
    {
        if (File.Exists(Path.Combine(Utils.DATA_DIR_NAME, SETTINGS_FILE_NAME)))
        {
            var settingsTxt = File.ReadAllText(Path.Combine(Utils.DATA_DIR_NAME, SETTINGS_FILE_NAME));
            Instance = JsonConvert.DeserializeObject<Settings>(settingsTxt);
        }
        else
        {
            Save();
        }
    }

    public static void Save()
    {
        var settingsJson = JsonConvert.SerializeObject(Instance, Formatting.Indented);
        File.WriteAllText(Path.Combine(Utils.DATA_DIR_NAME, SETTINGS_FILE_NAME), settingsJson);
    }
}

public enum EGameCapturePreference
{
    Auto,
    Steam,
    Standalone,
    Epic,
    HaoPlaySea,
    XDG,
    Custom = 200
}

public enum EWebhookReportsMode
{
    DiscordDeduplication,
    Discord,
    Custom,
    FallbackDiscordDeduplication,
}

public class SettingsExternal
{
    public BPTimerSettings BPTimerSettings { get; set; } = new();
}

public class WindowSettingsBase : ICloneable
{
    public Vector2 WindowPosition { get; set; } = new();
    public Vector2 WindowSize { get; set; } = new();

    public virtual object Clone()
    {
        return this.MemberwiseClone();
    }
}

public class WindowSettings : ICloneable
{
    public MainWindowWindowSettings MainWindow { get; set; } = new();
    public RaidManagerCooldownsWindowSettings RaidManagerCooldowns { get; set; } = new();
    public EntityCacheViewerWindowSettings EntityCacheViewer { get; set; } = new();
    public SpawnTrackerWindowSettings SpawnTracker { get; set; } = new();
    public ModuleWindowSettings ModuleWindow { get; set; } = new();

    public object Clone()
    {
        var cloned = (WindowSettings)this.MemberwiseClone();
        cloned.MainWindow = (MainWindowWindowSettings)this.MainWindow.Clone();
        cloned.RaidManagerCooldowns = (RaidManagerCooldownsWindowSettings)this.RaidManagerCooldowns.Clone();
        cloned.EntityCacheViewer = (EntityCacheViewerWindowSettings)this.EntityCacheViewer.Clone();
        cloned.SpawnTracker = (SpawnTrackerWindowSettings)this.SpawnTracker.Clone();
        return cloned;
    }
}