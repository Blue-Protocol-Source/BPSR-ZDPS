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

        public static ulong PlayerTotalMeterValue { get; set; }
        public static double PlayerMeterValuePerSecond { get; set; }

        public static void LoadDataTables()
        {
            // Load table data for resolving with in the future
            string appStringsFile = Path.Combine(Utils.DATA_DIR_NAME, "AppStrings.json");
            if (File.Exists(appStringsFile))
            {
                var appStrings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(appStringsFile));
                AppStrings.Strings = appStrings;
                System.Diagnostics.Debug.WriteLine("Loaded AppStrings.json");
            }

            string monsterTableFile = Path.Combine(Utils.DATA_DIR_NAME, "MonsterTable.json");
            if (File.Exists(monsterTableFile))
            {
                var monsters = JsonConvert.DeserializeObject<Dictionary<string, Monster>>(File.ReadAllText(monsterTableFile));
                HelperMethods.DataTables.Monsters.Data = monsters;
                System.Diagnostics.Debug.WriteLine("Loaded MonsterTable.json");
            }

            string skillTableFile = Path.Combine(Utils.DATA_DIR_NAME, "SkillTable.json");
            if (File.Exists(skillTableFile))
            {
                var skills = JsonConvert.DeserializeObject<Dictionary<string, Skill>>(File.ReadAllText(skillTableFile));
                HelperMethods.DataTables.Skills.Data = skills;
                System.Diagnostics.Debug.WriteLine("Loaded SkillTable.json");
            }

            // TODO: Every language can have its own 'Overrides' file
            string skillOverrivesFile = Path.Combine(Utils.DATA_DIR_NAME, "SkillOverrides.en.json");
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


            string targetTableFile = Path.Combine(Utils.DATA_DIR_NAME, "TargetTable.json");
            if (File.Exists(targetTableFile))
            {
                var targets = JsonConvert.DeserializeObject<Dictionary<string, Target>>(File.ReadAllText(targetTableFile));
                HelperMethods.DataTables.Targets.Data = targets;
                System.Diagnostics.Debug.WriteLine("Loaded TargetTable.json");
            }

            string sceneTableFile = Path.Combine(Utils.DATA_DIR_NAME, "SceneTable.json");
            if (File.Exists(sceneTableFile))
            {
                var scenes = JsonConvert.DeserializeObject<Dictionary<string, Scene>>(File.ReadAllText(sceneTableFile));
                HelperMethods.DataTables.Scenes.Data = scenes;
                System.Diagnostics.Debug.WriteLine("Loaded SceneTable.json");
            }

            // Load up our offline entity cache if it exists to help with initial data resolving when we're not given all the required details
            EntityCache.Instance.Load();
        }
    }
}
