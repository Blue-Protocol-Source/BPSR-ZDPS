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

    public void Apply()
    {
        MessageManager.NetCaptureDeviceName = NetCaptureDeviceName;
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