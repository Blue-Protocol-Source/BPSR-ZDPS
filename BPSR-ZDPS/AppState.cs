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
        public static string AccountId { get; set; }
        public static string PlayerName { get; set; }
        public static int ProfessionId { get; set; }
        public static string ProfessionName { get; set; }
        public static string SubProfessionName { get; set; }

        public static int PlayerMeterPlacement { get; set; } // Current position on the active meter, 0 means not on it

        public static ulong PlayerTotalMeterValue { get; set; }
        public static double PlayerMeterValuePerSecond { get; set; }

        public static bool IsBenchmarkMode { get; set; }
        public static int BenchmarkTime { get; set; }
        public static bool HasBenchmarkBegun { get; set; }

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
                        skill.Name = string.IsNullOrEmpty(item.Value.Name) ? skill.Name : item.Value.Name;
                        skill.Icon = string.IsNullOrEmpty(item.Value.Icon) ? skill.Icon : item.Value.Icon;
                    }
                    else
                    {
                        skill = new Skill();
                        skill.Name = item.Value.Name;
                        skill.Icon = item.Value.Icon;
                        if (item.Value.Id != 0)
                        {
                            skill.Id = item.Value.Id;
                        }
                        else
                        {
                            if (int.TryParse(item.Key, out int newId))
                            {
                                skill.Id = newId;
                            }
                        }
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

            string buffTableFile = Path.Combine(Utils.DATA_DIR_NAME, "BuffTable.json");
            if (File.Exists(buffTableFile))
            {
                var buffs = JsonConvert.DeserializeObject<Dictionary<string, Buff>>(File.ReadAllText(buffTableFile));
                HelperMethods.DataTables.Buffs.Data = buffs;
                System.Diagnostics.Debug.WriteLine("Loaded BuffTable.json");
            }

            // TODO: Every language can have its own 'Overrides' file
            string buffOverrivesFile = Path.Combine(Utils.DATA_DIR_NAME, "BuffOverrides.en.json");
            if (File.Exists(buffOverrivesFile))
            {
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, Buff>>(File.ReadAllText(buffOverrivesFile));
                foreach (var item in overrides)
                {
                    if (HelperMethods.DataTables.Buffs.Data.TryGetValue(item.Key, out var buff))
                    {
                        buff.Name = string.IsNullOrEmpty(item.Value.Name) ? buff.Name : item.Value.Name;
                        buff.Desc = string.IsNullOrEmpty(item.Value.Desc) ? buff.Desc : item.Value.Desc;
                        buff.Icon = string.IsNullOrEmpty(item.Value.Icon) ? buff.Icon : item.Value.Icon;
                        buff.ShowHUDIcon = string.IsNullOrEmpty(item.Value.ShowHUDIcon) ? buff.ShowHUDIcon : item.Value.ShowHUDIcon;

                        if (item.Value.BuffType.HasValue)
                        {
                            buff.BuffType = item.Value.BuffType.Value;
                        }
                        if (item.Value.BuffPriority.HasValue)
                        {
                            buff.BuffPriority = item.Value.BuffPriority.Value;
                        }
                    }
                    else
                    {
                        buff = new Buff();
                        buff.Name = item.Value.Name;
                        buff.Desc = item.Value.Desc;
                        buff.Icon = item.Value.Icon;
                        buff.ShowHUDIcon = item.Value.ShowHUDIcon;
                        buff.BuffType = item.Value.BuffType;
                        buff.BuffPriority = item.Value.BuffPriority;
                        buff.Id = string.IsNullOrWhiteSpace(item.Value.Id) ? item.Key : item.Value.Id;
                        HelperMethods.DataTables.Buffs.Data.Add(item.Key, buff);
                    }
                }
                System.Diagnostics.Debug.WriteLine("Loaded BuffOverrides.en.json");
            }

            // Load up our offline entity cache if it exists to help with initial data resolving when we're not given all the required details
            EntityCache.Instance.Load();
            //EntityCache.Instance.PortToDB();
        }
    }
}
