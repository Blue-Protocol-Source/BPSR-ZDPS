using BPSR_ZDPS.DataTypes.Chat;
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
    public bool ShowClassIconsInMeters { get; set; } = true;
    public bool ColorClassIconsByRole { get; set; } = true;
    public bool ShowSkillIconsInDetails { get; set; } = true;
    public bool OnlyShowDamageContributorsInMeters { get; set; } = false;
    public bool ShowAbilityScoreInMeters { get; set; } = true;
    public bool ShowSubProfessionNameInMeters { get; set; } = true;
    public bool UseAutomaticWipeDetection { get; set; } = true;
    public bool SkipTeleportStateCheckInAutomaticWipeDetection { get; set; } = false;
    public bool AllowWipeRecalculationOverwriting { get; set; } = false;
    public bool SplitEncountersOnNewPhases { get; set; } = true;
    public bool DisplayTruePerSecondValuesInMeters { get; set; } = false;
    public bool AllowGamepadNavigationInputInZDPS { get; set; } = false;
    public bool KeepPastEncounterInMeterUntilNextDamage { get; set; } = false;
    public bool UseDatabaseForEncounterHistory { get; set; } = true;
    public int DatabaseRetentionPolicyDays { get; set; } = 0;
    public bool LimitEncounterBuffTrackingWithoutDatabase { get; set; } = false;
    public bool AllowEncounterSavingPausingInOpenWorld { get; set; } = false;

    public bool MeterSettingsTankingShowDeaths { get; set; } = false;
    public bool MeterSettingsNpcTakenShowHpData { get; set; } = false;
    public bool MeterSettingsNpcTakenHideMaxHp { get; set; } = false;
    public bool MeterSettingsNpcTakenUseHpMeter { get; set; } = false;

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
    public bool AlwaysCreateReportAtDungeonEnd { get; set; } = true;

    public bool WebhookReportsEnabled { get; set; } = false;
    public EWebhookReportsMode WebhookReportsMode { get; set; } = EWebhookReportsMode.Discord;
    public string WebhookReportsDeduplicationServerHost { get; set; } = "https://zdps-webfunc.vercel.app";
    public string WebhookReportsDiscordUrl { get; set; } = "";
    public string WebhookReportsCustomUrl { get; set; } = "";

    public bool CheckForZDPSUpdatesOnStartup { get; set; } = false;
    public string LatestZDPSVersionCheckURL { get; set; } = "https://raw.githubusercontent.com/Blue-Protocol-Source/BPSR-ZDPS-Metadata/master/LatestVersion.txt";
    public string ZDPSWebsiteURL { get; set; } = "https://github.com/Blue-Protocol-Source/BPSR-ZDPS";
    public bool HasPromptedEnableUpdateChecks { get; set; } = false;

    // Settings specific to External components
    public SettingsExternal External { get; set; } = new();

    // Settings specific to an individual window
    public WindowSettings WindowSettings { get; set; } = new();

    public bool LowPerformanceMode { get; set; } = false;

    public uint HotkeysEncounterReset { get; set; }
    public uint HotkeysPinnedWindowClickthrough { get; set; }

    public ChatSettings Chat { get; set; } = new();

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
            // TODO: If there is an error loading Settings, instead of crashing, default values should be used and an error prompt displayed to users
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
    public int Opacity = 100;
    public bool TopMost = false;

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
    public RaidManagerRaidWarningWindowSettings RaidManagerRaidWarning { get; set; } = new();
    public RaidManagerCountdownWindowSettings RaidManagerCountdown { get; set; } = new();
    public RaidManagerThreatWindowSettings RaidManagerThreat { get; set; } = new();
    public ChatWindowSettings ChatWindow { get; set; } = new();

    public object Clone()
    {
        var cloned = (WindowSettings)this.MemberwiseClone();
        cloned.MainWindow = (MainWindowWindowSettings)this.MainWindow.Clone();
        cloned.RaidManagerCooldowns = (RaidManagerCooldownsWindowSettings)this.RaidManagerCooldowns.Clone();
        cloned.EntityCacheViewer = (EntityCacheViewerWindowSettings)this.EntityCacheViewer.Clone();
        cloned.SpawnTracker = (SpawnTrackerWindowSettings)this.SpawnTracker.Clone();
        cloned.ChatWindow = (ChatWindowSettings)this.ChatWindow.Clone();
        return cloned;
    }
}