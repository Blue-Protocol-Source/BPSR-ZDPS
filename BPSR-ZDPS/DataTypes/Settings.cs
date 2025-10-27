using Newtonsoft.Json;

namespace BPSR_ZDPS.DataTypes;

public class Settings
{
    public static Settings Inst = new();
    private static string SETTINGS_FILE_NAME = "Settings.json";
    
    public string NetCaptureDeviceName          { get; set; }
    public bool   NormalizeMeterContributions   { get; set; } = true;
    public bool   UseShortWidthNumberFormatting { get; set; } = true;
    public bool   ColorClassIconsByRole         { get; set; } = true;
    public bool   ShowSkillIconsInDetails       { get; set; } = true;

    public static void Load()
    {
        if (File.Exists(SETTINGS_FILE_NAME))
        {
            var settingsTxt = File.ReadAllText(SETTINGS_FILE_NAME);
            Inst = JsonConvert.DeserializeObject<Settings>(settingsTxt);
        }
        else {
            Save();
        }
    }
    
    public static void Save()
    {
        var settingsJson = JsonConvert.SerializeObject(Inst, Formatting.Indented);
        File.WriteAllText(SETTINGS_FILE_NAME, settingsJson);
    }
}