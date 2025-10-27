using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class AppState
    {
        public static long PlayerUUID { get; set; } // Original raw UUID
        public static long PlayerUID { get; set; }  // Resolved UUID into UID
        public static string PlayerName { get; set; }
        public static int ProfessionId { get; set; }
        public static string ProfessionName { get; set; }
        public static string SubProfessionName { get; set; }

        public static int PlayerMeterPlacement { get; set; } // Current position on the active meter, 0 means not on it

        public static long PlayerTotalMeterValue { get; set; }
        public static bool NormalizeMeterContributions { get; set; }
        public static bool UseShortWidthNumberFormatting { get; set; }
        public static bool ColorClassIconsByRole { get; set; }
        public static bool ShowSkillIconsInDetails { get; set; }


        public static void LoadDataTables()
        {
            // Load table data for resolving with in the future
            string appStringsFile = Path.Combine("Data", "AppStrings.json");
            if (File.Exists(appStringsFile))
            {
                var appStrings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(appStringsFile));
                AppStrings.Strings = appStrings;
                System.Diagnostics.Debug.WriteLine("Loaded AppStrings.json");
            }

            string monsterTableFile = Path.Combine("Data", "MonsterTable.json");
            if (File.Exists(monsterTableFile))
            {
                var monsters = JsonConvert.DeserializeObject<Dictionary<string, Monster>>(File.ReadAllText(monsterTableFile));
                HelperMethods.DataTables.Monsters.Data = monsters;
                System.Diagnostics.Debug.WriteLine("Loaded MonsterTable.json");
            }

            string skillTableFile = Path.Combine("Data", "SkillTable.json");
            if (File.Exists(skillTableFile))
            {
                var skills = JsonConvert.DeserializeObject<Dictionary<string, Skill>>(File.ReadAllText(skillTableFile));
                HelperMethods.DataTables.Skills.Data = skills;
                System.Diagnostics.Debug.WriteLine("Loaded SkillTable.json");
            }

            // TODO: Every language can have its own 'Overrides' file
            string skillOverrivesFile = Path.Combine("Data", "SkillOverrides.en.json");
            if (File.Exists(skillOverrivesFile))
            {
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, Skill>>(File.ReadAllText(skillOverrivesFile));
                foreach (var item in overrides)
                {
                    if (HelperMethods.DataTables.Skills.Data.TryGetValue(item.Key, out var skill))
                    {
                        skill.Name = item.Value.Name;
                        skill.Icon = item.Value.Icon;
                    }
                    else
                    {
                        skill = new Skill();
                        skill.Name = item.Value.Name;
                        skill.Icon = item.Value.Icon;
                        HelperMethods.DataTables.Skills.Data.Add(item.Key, skill);
                    }
                }
                System.Diagnostics.Debug.WriteLine("Loaded SkillOverrides.en.json");
            }
            // TODO: Map Icon from SkillTable to SkillId lookups and trim path to final part after a '/'

            // Load up our offline entity cache if it exists to help with initial data resolving when we're not given all the required details
            EntityCache.Instance.Load();
        }
    }
}
