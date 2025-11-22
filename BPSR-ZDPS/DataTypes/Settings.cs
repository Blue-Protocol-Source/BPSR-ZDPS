using BPSR_ZDPS.Windows;
using Newtonsoft.Json;

namespace BPSR_ZDPS.DataTypes;

public class Settings
{
    public static Settings Instance = new();
    private static string SETTINGS_FILE_NAME = "Settings.json";

    public int Version { get; set; } = 0;
    public string NetCaptureDeviceName { get; set; }
    public bool NormalizeMeterContributions { get; set; } = true;
    public bool UseShortWidthNumberFormatting { get; set; } = true;
    public bool ColorClassIconsByRole { get; set; } = true;
    public bool ShowSkillIconsInDetails { get; set; } = true;
    public bool OnlyShowDamageContributorsInMeters { get; set; } = false;
    public bool UseAutomaticWipeDetection { get; set; } = true;
    public bool SkipTeleportStateCheckInAutomaticWipeDetection { get; set; } = false;
    public bool SplitEncountersOnNewPhases { get; set; } = true;
    public float WindowOpacity = 1.0f;
    public bool UseDatabaseForEncounterHistory { get; set; } = true;
    public int DatabaseRetentionPolicyDays { get; set; } = 0;
    public bool LimitEncounterBuffTrackingWithoutDatabase { get; set; } = false;
    public bool LogToFile { get; set; } = false;
    public GameCapturePreference GameCapturePreference { get; set; } = GameCapturePreference.Auto;

    public uint HotkeysEncounterReset { get; set; }

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

public enum GameCapturePreference
{
    Auto,
    Steam,
    Standalone
}