using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Frozen;
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
        public static bool BenchmarkSingleTarget { get; set; }
        public static long BenchmarkSingleTargetUUID { get; set; }

        public static bool IsEncounterSavingPaused { get; set; } = false;
        public static bool WasEncounterSavingPaused { get; set; } = false; // For restoring Pause state after being in an Active Dungeon State

        public static bool MousePassthrough { get; set; } = false;

        public static bool IsUpdateAvailable { get; set; } = false;

        public static bool IsChatEnabled = true;

        public static long PartyTeamId = 0;

        public static Encounter? ActiveEncounter = null;
        public static Encounter? OpenedHistoricalEncounter = null;

        public static void LoadDataTables()
        {
            System.Diagnostics.Stopwatch loadTime = new();
            loadTime.Start();

            // Load table data for resolving with in the future
            string appStringsFile = Path.Combine(Utils.DATA_DIR_NAME, "AppStrings.json");
            if (File.Exists(appStringsFile))
            {
                var appStrings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(appStringsFile));
                AppStrings.Strings = appStrings.ToFrozenDictionary();
                Log.Information("Loaded AppStrings.json");
            }

            LoadAppStringsTable();

            string monsterTableFile = Path.Combine(Utils.DATA_DIR_NAME, "MonsterTable.json");
            if (File.Exists(monsterTableFile))
            {
                var monsters = JsonConvert.DeserializeObject<Dictionary<string, Monster>>(File.ReadAllText(monsterTableFile));
                HelperMethods.DataTables.Monsters.Data = monsters;
                Log.Information("Loaded MonsterTable.json");
            }

            string skillTableFile = Path.Combine(Utils.DATA_DIR_NAME, "SkillTable.json");
            if (File.Exists(skillTableFile))
            {
                var skills = JsonConvert.DeserializeObject<Dictionary<string, Skill>>(File.ReadAllText(skillTableFile));
                HelperMethods.DataTables.Skills.Data = skills;
                Log.Information("Loaded SkillTable.json");
                foreach (var skill in HelperMethods.DataTables.Skills.Data)
                {
                    // This is a useless placeholder value, swap it out for NameDesign which is likely to contain something a bit more useful
                    if (string.IsNullOrEmpty(skill.Value.Name) || skill.Value.Name == "场地标记01")
                    {
                        if (!string.IsNullOrEmpty(skill.Value.NameDesign))
                        {
                            skill.Value.Name = skill.Value.NameDesign;
                        }
                    }
                }
                Log.Information("Finished SkillTable post-processing");
            }

            string modTableFile = Path.Combine(Utils.DATA_DIR_NAME, "ModTable.json");
            if (File.Exists(modTableFile))
            {
                var modules = JsonConvert.DeserializeObject<Dictionary<int, ModuleData>>(File.ReadAllText(modTableFile));
                HelperMethods.DataTables.Modules.Data = modules;
                Log.Information("Loaded ModTable.json");
            }

            string modEffectTableFile = Path.Combine(Utils.DATA_DIR_NAME, "ModEffectTable.json");
            if (File.Exists(modEffectTableFile))
            {
                var modEffects = JsonConvert.DeserializeObject<Dictionary<int, EffectData>>(File.ReadAllText(modEffectTableFile));
                HelperMethods.DataTables.ModEffects.Data = modEffects;
                Log.Information("Loaded ModEffectTable.json");
            }

            string ModLinkEffectsFile = Path.Combine(Utils.DATA_DIR_NAME, "ModLinkEffectTable.json");
            if (File.Exists(modEffectTableFile))
            {
                var modLinkEffects = JsonConvert.DeserializeObject<Dictionary<int, ModLinkEffect>>(File.ReadAllText(ModLinkEffectsFile));
                HelperMethods.DataTables.ModLinkEffects.Data = modLinkEffects;
                Log.Information("Loaded ModLinkEffectTable.json");
            }

            LoadSkillOverridesTable();

            string skillFightLevelTableFile = Path.Combine(Utils.DATA_DIR_NAME, "SkillFightLevelTable.json");
            if (File.Exists(skillTableFile))
            {
                var skillFightLevels = JsonConvert.DeserializeObject<Dictionary<string, SkillFightLevel>>(File.ReadAllText(skillFightLevelTableFile));
                HelperMethods.DataTables.SkillFightLevels.Data = skillFightLevels;
                Log.Information("Loaded SkillFightLevelTable.json");
            }

            string targetTableFile = Path.Combine(Utils.DATA_DIR_NAME, "TargetTable.json");
            if (File.Exists(targetTableFile))
            {
                var targets = JsonConvert.DeserializeObject<Dictionary<string, Target>>(File.ReadAllText(targetTableFile));
                HelperMethods.DataTables.Targets.Data = targets;
                Log.Information("Loaded TargetTable.json");
            }

            string sceneTableFile = Path.Combine(Utils.DATA_DIR_NAME, "SceneTable.json");
            if (File.Exists(sceneTableFile))
            {
                var scenes = JsonConvert.DeserializeObject<Dictionary<string, Scene>>(File.ReadAllText(sceneTableFile));
                HelperMethods.DataTables.Scenes.Data = scenes;
                Log.Information("Loaded SceneTable.json");
            }

            string dungeonsTableFile = Path.Combine(Utils.DATA_DIR_NAME, "DungeonsTable.json");
            if (File.Exists(dungeonsTableFile))
            {
                var dungeons = JsonConvert.DeserializeObject<Dictionary<string, Dungeons>>(File.ReadAllText(dungeonsTableFile));
                HelperMethods.DataTables.Dungeons.Data = dungeons;
                Log.Information("Loaded DungeonsTable.json");
            }

            string buffTableFile = Path.Combine(Utils.DATA_DIR_NAME, "BuffTable.json");
            if (File.Exists(buffTableFile))
            {
                var buffs = JsonConvert.DeserializeObject<Dictionary<string, Buff>>(File.ReadAllText(buffTableFile));
                HelperMethods.DataTables.Buffs.Data = buffs;
                Log.Information("Loaded BuffTable.json");
                foreach (var buff in HelperMethods.DataTables.Buffs.Data)
                {
                    // This is a useless placeholder value, swap it out for NameDesign which is likely to contain something a bit more useful
                    if (string.IsNullOrEmpty(buff.Value.Name) || buff.Value.Name == "气刃突刺计数")
                    {
                        if (!string.IsNullOrEmpty(buff.Value.NameDesign))
                        {
                            buff.Value.Name = buff.Value.NameDesign;
                        }
                    }
                }
                Log.Information("Finished BuffTable post-processing");
            }

            LoadBuffOverridesTable();

            // Note: The typo in the name is correct, that's how it comes from the devs
            string sceneEventDungeonConfigTableFile = Path.Combine(Utils.DATA_DIR_NAME, "SceneEventDuneonConfigTable.json");
            if (File.Exists(sceneEventDungeonConfigTableFile))
            {
                var sceneEventDungeonConfigs = JsonConvert.DeserializeObject<Dictionary<string, SceneEventDungeonConfig>>(File.ReadAllText(sceneEventDungeonConfigTableFile));
                HelperMethods.DataTables.SceneEventDungeonConfigs.Data = sceneEventDungeonConfigs;
                Log.Information("Loaded SceneEventDuneonConfigTable.json");
            }

            string fightAttrTableFile = Path.Combine(Utils.DATA_DIR_NAME, "FightAttrTable.json");
            if (File.Exists(fightAttrTableFile))
            {
                var fightAttrs = JsonConvert.DeserializeObject<Dictionary<string, FightAttr>>(File.ReadAllText(fightAttrTableFile));
                HelperMethods.DataTables.FightAttrs.Data = fightAttrs;
                Log.Information("Loaded FightAttrTable.json");
            }

            string itemTableFile = Path.Combine(Utils.DATA_DIR_NAME, "ItemTable.json");
            if (File.Exists(itemTableFile))
            {
                var items = JsonConvert.DeserializeObject<Dictionary<string, Item>>(File.ReadAllText(itemTableFile));
                HelperMethods.DataTables.Items.Data = items;
                Log.Information("Loaded ItemTable.json");
            }

            string equipTableFile = Path.Combine(Utils.DATA_DIR_NAME, "EquipTable.json");
            if (File.Exists(equipTableFile))
            {
                var equips = JsonConvert.DeserializeObject<Dictionary<string, Equip>>(File.ReadAllText(equipTableFile));
                HelperMethods.DataTables.Equips.Data = equips;
                Log.Information("Loaded EquipTable.json");
            }

            string equipAttrLibTableFile = Path.Combine(Utils.DATA_DIR_NAME, "EquipAttrLibTable.json");
            if (File.Exists(equipAttrLibTableFile))
            {
                var equipAttrLibs = JsonConvert.DeserializeObject<Dictionary<string, EquipAttrLib>>(File.ReadAllText(equipAttrLibTableFile));
                HelperMethods.DataTables.EquipAttrLibs.Data = equipAttrLibs;
                Log.Information("Loaded EquipAttrLibTable.json");
            }

            string equipAttrSchoolLibTableFile = Path.Combine(Utils.DATA_DIR_NAME, "EquipAttrSchoolLibTable.json");
            if (File.Exists(equipAttrSchoolLibTableFile))
            {
                var equipAttrSchoolLib = JsonConvert.DeserializeObject<Dictionary<string, EquipAttrSchoolLib>>(File.ReadAllText(equipAttrSchoolLibTableFile));
                HelperMethods.DataTables.EquipAttrSchoolLibs.Data = equipAttrSchoolLib;
                Log.Information("Loaded EquipAttrSchoolLibTable.json");
            }

            string equipEnchantTableFile = Path.Combine(Utils.DATA_DIR_NAME, "EquipEnchantTable.json");
            if (File.Exists(equipEnchantTableFile))
            {
                var equipEnchants = JsonConvert.DeserializeObject<Dictionary<string, EquipEnchant>>(File.ReadAllText(equipEnchantTableFile));
                HelperMethods.DataTables.EquipEnchants.Data = equipEnchants;
                Log.Information("Loaded EquipEnchantTable.json");
            }

            string equipPerfectLibTableFile = Path.Combine(Utils.DATA_DIR_NAME, "EquipPerfectLibTable.json");
            if (File.Exists(equipPerfectLibTableFile))
            {
                var equipPerfectLibs = JsonConvert.DeserializeObject<Dictionary<string, EquipPerfectLib>>(File.ReadAllText(equipPerfectLibTableFile));
                HelperMethods.DataTables.EquipPerfectLibs.Data = equipPerfectLibs;
                Log.Information("Loaded EquipPerfectLibTable.json");
            }

            string equipBreakThroughTableFile = Path.Combine(Utils.DATA_DIR_NAME, "EquipBreakThroughTable.json");
            if (File.Exists(equipBreakThroughTableFile))
            {
                var equipBreakThroughs = JsonConvert.DeserializeObject<Dictionary<string, EquipBreakThrough>>(File.ReadAllText(equipBreakThroughTableFile));
                HelperMethods.DataTables.EquipBreakThroughs.Data = equipBreakThroughs;
                Log.Information("Loaded EquipBreakThroughTable.json");
            }

            string dbmTableFile = Path.Combine(Utils.DATA_DIR_NAME, "DbmTable.json");
            if (File.Exists(dbmTableFile))
            {
                var dbms = JsonConvert.DeserializeObject<Dictionary<string, Dbm>>(File.ReadAllText(dbmTableFile));
                HelperMethods.DataTables.Dbms.Data = dbms;
                Log.Information("Loaded DbmTable.json");
            }

            string tempAttrTableFile = Path.Combine(Utils.DATA_DIR_NAME, "TempAttrTable.json");
            if (File.Exists(tempAttrTableFile))
            {
                var tempAttrs = JsonConvert.DeserializeObject<Dictionary<string, TempAttr>>(File.ReadAllText(tempAttrTableFile));
                HelperMethods.DataTables.TempAttrs.Data = tempAttrs;
                Log.Information("Loaded TempAttrTable.json");
            }

            string AttrDescriptionFile = Path.Combine(Utils.DATA_DIR_NAME, "AttrDescription.json");
            if (File.Exists(AttrDescriptionFile))
            {
                var attrDescriptions = JsonConvert.DeserializeObject<Dictionary<string, AttrDescription>>(File.ReadAllText(AttrDescriptionFile));
                HelperMethods.DataTables.AttrDescriptions.Data = attrDescriptions;
                Log.Information("Loaded AttrDescription.json");
            }

            string DummyTableFile = Path.Combine(Utils.DATA_DIR_NAME, "DummyTable.json");
            if (File.Exists(DummyTableFile))
            {
                var dummys = JsonConvert.DeserializeObject<Dictionary<string, Dummy>>(File.ReadAllText(DummyTableFile));
                HelperMethods.DataTables.Dummys.Data = dummys;
                Log.Information("Loaded DummyTable.json");
            }

            string dummyOverridesFile = Path.Combine(Utils.DATA_DIR_NAME, "DummyOverrides.en.json");
            if (File.Exists(dummyOverridesFile))
            {
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, Dummy>>(File.ReadAllText(dummyOverridesFile));
                foreach (var item in overrides)
                {
                    if (HelperMethods.DataTables.Dummys.Data.TryGetValue(item.Key, out var dummy))
                    {
                        dummy.Name = string.IsNullOrEmpty(item.Value.Name) ? dummy.Name : item.Value.Name;
                    }
                }
                Log.Information("Loaded DummyOverrides.en.json");
            }

            try
            {
                System.Reflection.FieldInfo fi = typeof(Managers.External.BPTimerManager).GetField(Encoding.UTF8.GetString([0x41, 0x50, 0x49, 0x5f, 0x4b, 0x45, 0x59]),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                fi.SetValue(null, Encoding.ASCII.GetString(Managers.External.BPTimerManager.HandleDataEvent(), 20, 50));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Reflection Error");
            }

            var startupTime = loadTime.Elapsed.TotalSeconds;
            Serilog.Log.Debug($"Took {Math.Round(startupTime, 4)}s to load DataTables.");

            // Load up our offline entity cache if it exists to help with initial data resolving when we're not given all the required details
            EntityCache.Instance.Load();
            //EntityCache.Instance.PortToDB();

            Serilog.Log.Debug($"Took {Math.Round(loadTime.Elapsed.TotalSeconds - startupTime, 4)}s to load EntityCache.");

            loadTime.Stop();
        }

        public static void LoadAppStringsTable()
        {
            // Always load English first as the base since additional configurations (if needed) may exist only in it
            string appStringsExFile = Path.Combine(Utils.DATA_DIR_NAME, "AppStrings.en.json");
            if (File.Exists(appStringsExFile))
            {
                var appStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(appStringsExFile));
                AppStrings.Locs = appStrings.ToFrozenDictionary();
                Log.Information("Loaded AppStrings.en.json");
            }

            if (!string.IsNullOrEmpty(Settings.Instance.Language) && Settings.Instance.Language != "en")
            {
                string appStringsLocFile = Path.Combine(Utils.DATA_DIR_NAME, $"AppStrings.{Settings.Instance.Language}.json");
                if (File.Exists(appStringsLocFile))
                {
                    var appStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(appStringsLocFile));
                    Dictionary<string, string> combinedLocs = AppStrings.Locs.ToDictionary();
                    foreach (var loc in appStrings)
                    {
                        if (combinedLocs.TryGetValue(loc.Key, out var value))
                        {
                            value = loc.Value;
                        }
                        else
                        {
                            combinedLocs.Add(loc.Key, loc.Value);
                        }
                    }
                    AppStrings.Locs = combinedLocs.ToFrozenDictionary();
                    Log.Information($"Loaded {$"AppStrings.{Settings.Instance.Language}.json"}");
                }
                else
                {
                    Log.Error($"Failed to loaded {$"AppStrings.{Settings.Instance.Language}.json"}");
                }
            }
        }

        public static void LoadSkillOverridesTable()
        {
            LoadSkillOverrideFile("SkillOverrides.en.json");
            if (!string.IsNullOrEmpty(Settings.Instance.Language) && Settings.Instance.Language != "en")
            {
                LoadSkillOverrideFile($"SkillOverrides.{Settings.Instance.Language}.json");
            }
        }

        static void LoadSkillOverrideFile(string fileName)
        {
            string skillOverridesFile = Path.Combine(Utils.DATA_DIR_NAME, fileName);
            if (File.Exists(skillOverridesFile))
            {
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, Skill>>(File.ReadAllText(skillOverridesFile));
                foreach (var item in overrides)
                {
                    if (HelperMethods.DataTables.Skills.Data.TryGetValue(item.Key, out var skill))
                    {
                        skill.Name = string.IsNullOrEmpty(item.Value.Name) ? skill.Name : item.Value.Name;
                        skill.Desc = string.IsNullOrEmpty(item.Value.Desc) ? skill.Name : item.Value.Desc;
                        skill.Icon = string.IsNullOrEmpty(item.Value.Icon) ? skill.Icon : item.Value.Icon;

                        if (item.Value.Icon == "-")
                        {
                            skill.Icon = "";
                        }
                        if (item.Value.SkillLevelGroup > 0)
                        {
                            skill.SkillLevelGroup = item.Value.SkillLevelGroup;
                        }
                        if (item.Value.SlotPositionId != null && item.Value.SlotPositionId.Count > 0)
                        {
                            skill.SlotPositionId = new();
                            skill.SlotPositionId.AddRange(item.Value.SlotPositionId);
                        }
                    }
                    else
                    {
                        skill = new Skill();
                        skill.Name = item.Value.Name;
                        skill.Desc = item.Value.Desc;
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
                        if (item.Value.SkillLevelGroup > 0)
                        {
                            skill.SkillLevelGroup = item.Value.SkillLevelGroup;
                        }
                        if (item.Value.SlotPositionId != null && item.Value.SlotPositionId.Count > 0)
                        {
                            skill.SlotPositionId = new();
                            skill.SlotPositionId.AddRange(item.Value.SlotPositionId);
                        }
                        HelperMethods.DataTables.Skills.Data.Add(item.Key, skill);
                    }
                }
                Log.Information($"Loaded {fileName}");
            }
            else
            {
                Log.Error($"Failed to loaded {fileName}");
            }
        }

        public static void LoadBuffOverridesTable()
        {
            LoadBuffOverrideFile("BuffOverrides.en.json");
            if (!string.IsNullOrEmpty(Settings.Instance.Language) && Settings.Instance.Language != "en")
            {
                LoadSkillOverrideFile($"BuffOverrides.{Settings.Instance.Language}.json");
            }
        }

        static void LoadBuffOverrideFile(string fileName)
        {
            string buffOverridesFile = Path.Combine(Utils.DATA_DIR_NAME, fileName);
            if (File.Exists(buffOverridesFile))
            {
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, Buff>>(File.ReadAllText(buffOverridesFile));
                foreach (var item in overrides)
                {
                    if (HelperMethods.DataTables.Buffs.Data.TryGetValue(item.Key, out var buff))
                    {
                        buff.Name = string.IsNullOrEmpty(item.Value.Name) ? buff.Name : item.Value.Name;
                        buff.Desc = string.IsNullOrEmpty(item.Value.Desc) ? buff.Desc : item.Value.Desc;
                        buff.Icon = string.IsNullOrEmpty(item.Value.Icon) ? buff.Icon : item.Value.Icon;
                        buff.ShowHUDIcon = string.IsNullOrEmpty(item.Value.ShowHUDIcon) ? buff.ShowHUDIcon : item.Value.ShowHUDIcon;

                        if (item.Value.Icon == "-")
                        {
                            buff.Icon = "";
                        }
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
                Log.Information($"Loaded {fileName}");
            }
            else
            {
                Log.Error($"Failed to loaded {fileName}");
            }
        }
    }
}
