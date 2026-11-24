using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Managers.External;
using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;
using static HexaGen.Runtime.MemoryPool;

namespace BPSR_ZDPS
{
    public static class PlayerMetricsWindow
    {
        public const string LAYER = "PlayerMetricsWindowLayer";
        public static string TITLE_ID = "###PlayerMetricsWindow";
        public static string TITLE = "Player Metrics";
        public static bool IsOpened = false;
        public static bool CollapseToContentOnly = false;
        public static Vector2 DefaultWindowSize = new Vector2(700, 600);
        public static bool ResetWindowSize = false;

        static int RunOnceDelayed = 0;
        static Vector2 MenuBarSize;
        static bool HasInitBindings = false;
        static int LastPinnedOpacity = 100;
        static bool IsPinned = false;

        static bool TransparentBackground = false;

        static bool ShouldProcessData = false;
        static Dictionary<string, InstancedContent> Instances = new();
        static Dictionary<int, ImagineEntry> Imagines = new();

        static Zproto.CharSerialize? charSerialize = null;

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            IsPinned = false;
            InitializeBindings();
            ImGui.PopID();
        }

        public static void InitializeBindings()
        {
            if (HasInitBindings == false)
            {
                HasInitBindings = true;
            }
        }

        public static void LoadCharData(Zproto.CharSerialize data)
        {
            charSerialize = data;

            ShouldProcessData = true;
        }

        private static void ProcessData()
        {
            if (ShouldProcessData && charSerialize != null)
            {
                ShouldProcessData = false;

                Instances = new();
                Imagines = new();

                int currentSeason = charSerialize.SeasonCenter.SeasonId;

                for (int i = 0; i <= currentSeason; i++)
                {
                    string name = "";
                    if (HelperMethods.DataTables.SeasonGlobals.Data.TryGetValue(i.ToString(), out var seasonGlobalData))
                    {
                        name = seasonGlobalData.SeasonName;
                    }
                    Instances.Add($"{i}", new() { SeasonName = name });
                }

                foreach (var dungeonEntry in charSerialize.DungeonList.CompleteDungeon)
                {
                    string dungeonName = "";
                    int season = 0;
                    int functionId = 0;
                    string functionName = "";
                    string dungeonTypeName = "";
                    if (HelperMethods.DataTables.Dungeons.Data.TryGetValue(dungeonEntry.Key.ToString(), out var dungeonData))
                    {
                        if (dungeonData.PlayType != 17)
                        {
                            if (HelperMethods.DataTables.Scenes.Data.TryGetValue(dungeonEntry.Key.ToString(), out var sceneData))
                            {
                                dungeonName = sceneData.Name;
                            }
                            else
                            {
                                // Fallback to Dungeon Name even though it won't be as accurate of a name now
                                dungeonName = dungeonData.Name;
                            }
                        }
                        else
                        {
                            // TODO: Attempt to determine Master level from Score value range
                            // MasterChallengeDungeonTable has a unique value for MonsterLv, Score, and AssessId
                            // Score appears to not be a reasonable value for a Master run
                            dungeonName = dungeonData.Name;
                        }

                        if (dungeonData.Season.Count > 0)
                        {
                            season = dungeonData.Season.First();
                        }

                        functionId = dungeonData.FunctionID;
                        if (HelperMethods.DataTables.Functions.Data.TryGetValue(functionId.ToString(), out var functionData))
                        {
                            functionName = functionData.Name;
                        }

                        dungeonTypeName = dungeonData.DungeonTypeName;
                    }

                    var data = new DungeonData()
                    {
                        DungeonId = dungeonEntry.Key,
                        DungeonName = dungeonName,
                        Completions = dungeonEntry.Value.CompleteCount,
                        PassTime = dungeonEntry.Value.PassTime,
                        BestRecord = TimeSpan.FromSeconds(dungeonEntry.Value.PassTime),
                        Score = dungeonEntry.Value.Score,
                    };

                    if (!Instances[season.ToString()].Dungeons.TryGetValue(functionId.ToString(), out var dungeon))
                    {
                        dungeon = new DungeonEntry()
                        {
                            FunctionId = functionId,
                            FunctionName = functionName,
                            Id = dungeonEntry.Key,
                            Name = dungeonName,
                            DifficultyName = dungeonTypeName,
                        };
                    }

                    dungeon.Dungeons.Add(dungeonEntry.Key, data);

                    Instances[season.ToString()].Dungeons[functionId.ToString()] = dungeon;
                }

                foreach (var raidEntry in charSerialize.DungeonList.RaidRecordTable)
                {
                    var raidId = raidEntry.Key;

                    string raidName = "";
                    int season = 0;
                    if (HelperMethods.DataTables.Dungeons.Data.TryGetValue(raidId.ToString(), out var dungeonData))
                    {
                        raidName = dungeonData.Name;
                        if (dungeonData.Season.Count > 0)
                        {
                            season = dungeonData.Season.First();
                        }
                    }

                    SortedSet<RaidBossData> bosses = new();

                    foreach (var bossProgress in raidEntry.Value.BossProgress)
                    {
                        var bossId = bossProgress.Key;
                        string bossName = "";
                        if (HelperMethods.DataTables.Monsters.Data.TryGetValue(bossId.ToString(), out var monsterData))
                        {
                            bossName = monsterData.Name;
                        }

                        var bossKillCount = bossProgress.Value.TotalAwardCnt;

                        bosses.Add(new()
                        {
                            Id = bossId,
                            Name = bossName,
                            AwardCount = bossKillCount,
                        });
                    }

                    var newRaid = new RaidEntry()
                    {
                        Id = raidId,
                        Name = raidName,
                        Bosses = bosses,
                    };

                    Instances[season.ToString()].Raids.Add(raidId.ToString(), newRaid);
                }

                foreach (var masterModeInfos in charSerialize.MasterModeDungeonInfo.MasterModeDungeonInfo_)
                {
                    var season = masterModeInfos.Key;

                    var highestClear = new SortedDictionary<int, MasterScoreEntry>();

                    foreach (var masterDiffInfoEntry in masterModeInfos.Value.MasterModeDiffInfo)
                    {
                        var dungeonId = masterDiffInfoEntry.Key;
                        string dungeonName = "";
                        if (HelperMethods.DataTables.Dungeons.Data.TryGetValue(dungeonId.ToString(), out var dungeonData))
                        {
                            dungeonName = dungeonData.Name;
                        }

                        SortedSet<MasterData> masters = new();

                        foreach (var dungeonInfo in masterDiffInfoEntry.Value.DungeonInfo)
                        {
                            var masterLevel = dungeonInfo.Key; // is also stored as: dungeonInfo.Value.DungeonID
                            var completeCount = dungeonInfo.Value.CompleteCount;
                            var passTime = dungeonInfo.Value.PassTime;
                            var score = dungeonInfo.Value.Score;

                            masters.Add(new MasterData()
                            {
                                MasterLevel = masterLevel,
                                CompleteCount = completeCount,
                                PassTime = passTime,
                                BestRecord = TimeSpan.FromSeconds(passTime),
                                Score = score
                            });

                            if (highestClear.TryGetValue(dungeonId, out var masterScoreEntry))
                            {
                                if (masterScoreEntry.MasterLevel < masterLevel)
                                {
                                    highestClear[dungeonId] = new MasterScoreEntry()
                                    {
                                        DungeonId = dungeonId,
                                        Name = dungeonName,
                                        MasterLevel = masterLevel,
                                        CompleteCount = completeCount,
                                        PassTime = passTime,
                                        BestRecord = TimeSpan.FromSeconds(passTime),
                                        Score = score
                                    };
                                }
                            }
                            else
                            {
                                highestClear.Add(dungeonId, new MasterScoreEntry()
                                {
                                    DungeonId = dungeonId,
                                    Name = dungeonName,
                                    MasterLevel = masterLevel,
                                    CompleteCount = completeCount,
                                    PassTime = passTime,
                                    BestRecord = TimeSpan.FromSeconds(passTime),
                                    Score = score
                                });
                            }
                        }

                        var masterEntry = new MasterEntry()
                        {
                            Id = dungeonId,
                            Name = dungeonName,
                            Masters = masters,
                        };

                        Instances[season.ToString()].Masters.Add(dungeonId.ToString(), masterEntry);
                    }

                    SortedDictionary<int, MasterAwardEntry> masterAwardEntries = new();
                    foreach (var masterAwards in masterModeInfos.Value.SeasonAwards)
                    {
                        var awardId = masterAwards.Key + 1;
                        var isUnlocked = masterAwards.Value;

                        masterAwardEntries.Add(awardId, new() { AwardId = awardId, IsUnlocked = isUnlocked });
                    }

                    int totalScore = 0;

                    foreach (var entry in highestClear)
                    {
                        totalScore += entry.Value.Score;
                    }

                    Instances[season.ToString()].MasterScores.TotalScore = totalScore;
                    Instances[season.ToString()].MasterScores.HighestScores = highestClear;

                    Instances[season.ToString()].MasterScores.MasterAwards = masterAwardEntries;
                }

                foreach (var imagineEntry in charSerialize.ProfessionList.AoyiSkillInfoMap)
                {
                    string name = "";
                    if (HelperMethods.DataTables.Skills.Data.TryGetValue(imagineEntry.Value.SkillId.ToString(), out var skillData))
                    {
                        name = skillData.Name;
                    }

                    string MonsterName = "";
                    int rarity = 0;
                    if (HelperMethods.DataTables.SkillAoyis.Data.TryGetValue(imagineEntry.Value.SkillId.ToString(), out var skillAoyiData))
                    {
                        MonsterName = skillAoyiData.ResonanceObject;
                        rarity = skillAoyiData.RarityType;
                    }

                    ImagineEntry imagine = new()
                    {
                        Id = imagineEntry.Value.SkillId,
                        Name = name,
                        Tier = imagineEntry.Value.RemodelLevel,
                        MonsterName = MonsterName,
                        Rarity = rarity,
                    };

                    Imagines.Add(imagineEntry.Key, imagine);
                }
                Imagines = Imagines.OrderByDescending(x => x.Value.Rarity).ThenByDescending(x => x.Value.Tier).ThenBy(x => x.Value.Id).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened)
            {
                return;
            }

            var windowSettings = Settings.Instance.WindowSettings.PlayerMetrics;

            ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 100), new Vector2(ImGui.GETFLTMAX()));

            if (windowSettings.WindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(windowSettings.WindowSize, ImGuiCond.FirstUseEver);
            }

            if (ResetWindowSize)
            {
                ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.Always);
                ResetWindowSize = false;
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (AppState.MousePassthrough && windowSettings.TopMost)
            {
                exWindowFlags |= ImGuiWindowFlags.NoInputs;
            }

            if (TransparentBackground)
            {
                exWindowFlags |= ImGuiWindowFlags.NoBackground;
            }

            if (ImGui.Begin($"{TITLE}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking | exWindowFlags))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();

                    if (windowSettings.TopMost && !IsPinned)
                    {
                        IsPinned = true;
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }
                else if (RunOnceDelayed >= 2)
                {
                    if (windowSettings.TopMost && LastPinnedOpacity != windowSettings.Opacity)
                    {
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }

                DrawMenuBar();

                if (charSerialize != null)
                {
                    //File.WriteAllText("_CharSerialize.json", Newtonsoft.Json.JsonConvert.SerializeObject(charSerialize));
                    if (!CollapseToContentOnly)
                    {
                        ImGui.TextUnformatted("Name:");
                        ImGui.SameLine();
                        ImGui.TextUnformatted($"{charSerialize.CharBase.Name}");

                        ImGui.TextUnformatted("Title Id: ");
                        ImGui.SameLine();
                        ImGui.TextUnformatted($"{charSerialize.PersonalZone.TitleData.TitleId}");

                        ImGui.TextUnformatted("Total Online Time:");
                        ImGui.SameLine();
                        ImGui.TextUnformatted($"{TimeSpan.FromSeconds(charSerialize.CharBase.TotalOnlineTime).ToString(@"d\d\ h\h\ m\m\ s\s")}");
                        ImGui.SetItemTooltip($"Total Seconds: {charSerialize.CharBase.TotalOnlineTime:N0}\nThis value is only updated each time you log into the game.");
                    }

                    ProcessData();

                    if (ImGui.CollapsingHeader("Imagines"))
                    {
                        ImGui.Indent();

                        ImGui.TextUnformatted($"Imagines Owned: {Imagines.Count}");
                        if (ImGui.BeginTable("##ImaginesTable", 4, ImGuiTableFlags.SizingFixedFit))
                        {
                            ImGui.TableSetupColumn("Id");
                            ImGui.TableSetupColumn("Imagine");
                            ImGui.TableSetupColumn("Name");
                            ImGui.TableSetupColumn("Tier");

                            ImGui.TableHeadersRow();

                            foreach (var entry in Imagines)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Selectable($"{entry.Value.Id}", true, ImGuiSelectableFlags.SpanAllColumns);

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(entry.Value.Name);

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(entry.Value.MonsterName);

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{entry.Value.Tier}");
                            }

                            ImGui.EndTable();
                        }

                        ImGui.Unindent();
                    }

                    if (ImGui.CollapsingHeader("Content Progression"))
                    {
                        ImGui.Indent();
                        foreach (var season in Instances)
                        {
                            string seasonName = "";
                            if (season.Key == "0")
                            {
                                seasonName = $"CROSS SEASONAL";
                            }
                            else
                            {
                                
                                if (!string.IsNullOrEmpty(season.Value.SeasonName))
                                {
                                    seasonName = $"SEASON {season.Key} ({season.Value.SeasonName})";
                                }
                                else
                                {
                                    seasonName = $"SEASON {season.Key}";
                                }
                            }

                            if (ImGui.CollapsingHeader($"{seasonName}##Season_{season.Key}_Header"))
                            {
                                ImGui.Indent();

                                if (season.Key == "0")
                                {
                                    ImGui.TextDisabled("Content entries here span across multiple seasons.");
                                }

                                if (season.Value.Masters.Count > 0)
                                {
                                    if (ImGui.CollapsingHeader($"MASTER DUNGEONS##Season_{season.Key}_MasterDungeons_Headers"))
                                    {
                                        ImGui.Indent();

                                        if (ImGui.CollapsingHeader("MASTER SEAL"))
                                        {
                                            ImGui.Indent();
                                            if (season.Value.MasterScores.MasterAwards.Count > 0)
                                            {
                                                ImGui.TextUnformatted($"Master Score Rewards");
                                                if (ImGui.BeginTable($"##Season_{season.Key}_MasterAwards_table", season.Value.MasterScores.MasterAwards.Count, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV))
                                                {
                                                    foreach (var masterAward in season.Value.MasterScores.MasterAwards)
                                                    {
                                                        ImGui.TableSetupColumn($"{masterAward.Key}");
                                                    }

                                                    ImGui.TableHeadersRow();

                                                    foreach (var masterAward in season.Value.MasterScores.MasterAwards)
                                                    {
                                                        ImGui.TableNextColumn();
                                                        bool isUnlocked = masterAward.Value.IsUnlocked == 0 ? false : true;
                                                        ImGui.Checkbox($"##AwardBox_{masterAward.Key}", ref isUnlocked);
                                                    }
                                                    ImGui.EndTable();
                                                }

                                                ImGui.TextUnformatted("Highest Scores");
                                                if (ImGui.BeginTable($"##Season_{season.Key}_MasterScore_table", 5, ImGuiTableFlags.SizingFixedFit))
                                                {
                                                    ImGui.TableSetupColumn("Name");
                                                    ImGui.TableSetupColumn("Master Level");
                                                    ImGui.TableSetupColumn("Clears");
                                                    ImGui.TableSetupColumn("Best Record");
                                                    ImGui.TableSetupColumn("Score");

                                                    ImGui.TableHeadersRow();

                                                    int idx = 0;
                                                    foreach (var entry in season.Value.MasterScores.HighestScores)
                                                    {
                                                        ImGui.TableNextColumn();
                                                        ImGui.Selectable($"{entry.Value.Name}##{entry.Value.Name}_{entry.Value.MasterLevel}_{idx}", true, ImGuiSelectableFlags.SpanAllColumns);

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.MasterLevel}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.CompleteCount}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.BestRecord.ToString(@"m\ms\s")}");
                                                        ImGui.SetItemTooltip($"Total Seconds: {entry.Value.PassTime}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.Score}");

                                                        idx++;
                                                    }

                                                    ImGui.TableNextColumn();
                                                    ImGui.PushStyleColor(ImGuiCol.Header, Colors.DimGray);
                                                    ImGui.Selectable("Total Season Score", true, ImGuiSelectableFlags.SpanAllColumns);
                                                    ImGui.PopStyleColor();

                                                    ImGui.TableNextColumn(); // MasterLevel
                                                    ImGui.TableNextColumn(); // CompleteCount
                                                    ImGui.TableNextColumn(); // BestRecord

                                                    ImGui.TableNextColumn();
                                                    ImGui.TextUnformatted($"{season.Value.MasterScores.TotalScore}");

                                                    ImGui.EndTable();
                                                }
                                            }
                                            ImGui.Unindent();
                                        }

                                        foreach (var master in season.Value.Masters)
                                        {
                                            if (ImGui.CollapsingHeader($"{master.Value.Name} [{master.Key}]##Season_{season.Key}_Master_{master.Key}_header"))
                                            {
                                                ImGui.Indent();
                                                if (ImGui.BeginTable($"##Season_{season.Key}_Master_{master.Key}_table", 4, ImGuiTableFlags.SizingFixedFit))
                                                {
                                                    ImGui.TableSetupColumn("Master Level");
                                                    ImGui.TableSetupColumn("Clears");
                                                    ImGui.TableSetupColumn("Best Record");
                                                    ImGui.TableSetupColumn("Score");

                                                    ImGui.TableHeadersRow();

                                                    foreach (var entry in master.Value.Masters)
                                                    {
                                                        ImGui.TableNextColumn();
                                                        ImGui.Selectable($"{entry.MasterLevel}", true, ImGuiSelectableFlags.SpanAllColumns);

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.CompleteCount}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.BestRecord.ToString(@"m\ms\s")}");
                                                        ImGui.SetItemTooltip($"Total Seconds: {entry.PassTime}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Score}");
                                                    }
                                                    ImGui.EndTable();
                                                }
                                                ImGui.Unindent();
                                            }
                                        }
                                        ImGui.Unindent();
                                    }
                                }

                                if (season.Value.Raids.Count > 0)
                                {
                                    if (ImGui.CollapsingHeader($"RAIDS##Season_{season.Key}_Raids_Headers"))
                                    {
                                        ImGui.Indent();
                                        foreach (var raid in season.Value.Raids)
                                        {
                                            if (ImGui.CollapsingHeader($"{raid.Value.Name} [{raid.Key}]##Season_{season.Key}_Raid_{raid.Key}_header"))
                                            {
                                                ImGui.Indent();
                                                if (ImGui.BeginTable($"##Season_{season.Key}_Raid_{raid.Key}_table", 3, ImGuiTableFlags.SizingFixedFit))
                                                {
                                                    ImGui.TableSetupColumn("Id");
                                                    ImGui.TableSetupColumn("Name");
                                                    ImGui.TableSetupColumn("Clears");

                                                    ImGui.TableHeadersRow();

                                                    foreach (var boss in raid.Value.Bosses)
                                                    {
                                                        ImGui.TableNextColumn();
                                                        ImGui.Selectable($"{boss.Id}", true, ImGuiSelectableFlags.SpanAllColumns);

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{boss.Name}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{boss.AwardCount}");
                                                    }
                                                    ImGui.EndTable();
                                                }
                                                ImGui.Unindent();
                                            }
                                        }
                                        ImGui.Unindent();
                                    }
                                }

                                if (season.Value.Dungeons.Count > 0)
                                {
                                    if (ImGui.CollapsingHeader($"INSTANCES##Season_{season.Key}_Instances_Headers"))
                                    {
                                        ImGui.Indent();
                                        foreach (var dungeon in season.Value.Dungeons)
                                        {
                                            if (ImGui.CollapsingHeader($"{dungeon.Value.FunctionName} [{dungeon.Key}]##Season_{season.Key}_Function_{dungeon.Key}_header"))
                                            {
                                                ImGui.Indent();
                                                if (ImGui.BeginTable($"##Season_{season.Key}_Function_{dungeon.Key}_table", 5, ImGuiTableFlags.SizingFixedFit))
                                                {
                                                    ImGui.TableSetupColumn("Id");
                                                    ImGui.TableSetupColumn("Dungeon");
                                                    ImGui.TableSetupColumn("Clears");
                                                    ImGui.TableSetupColumn("Best Record");
                                                    ImGui.TableSetupColumn("Score");

                                                    ImGui.TableHeadersRow();

                                                    int idx = 0;
                                                    foreach (var entry in dungeon.Value.Dungeons)
                                                    {
                                                        ImGui.TableNextColumn();
                                                        ImGui.Selectable($"{entry.Value.DungeonId}", true, ImGuiSelectableFlags.SpanAllColumns);

                                                        ImGui.TableNextColumn();
                                                        string name = entry.Value.DungeonName;
                                                        if (dungeon.Value.DifficultyName != "")
                                                        {
                                                            name = $"{name} [{dungeon.Value.DifficultyName}]";
                                                        }
                                                        //ImGui.Selectable($"{name}##entry_idx_{idx}", true, ImGuiSelectableFlags.SpanAllColumns);
                                                        ImGui.TextUnformatted($"{name}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.Completions}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.BestRecord.ToString(@"m\ms\s")}");
                                                        ImGui.SetItemTooltip($"Total Seconds: {entry.Value.PassTime}");

                                                        ImGui.TableNextColumn();
                                                        ImGui.TextUnformatted($"{entry.Value.Score}");

                                                        idx++;
                                                    }

                                                    ImGui.EndTable();
                                                }
                                                ImGui.Unindent();
                                            }
                                        }
                                        ImGui.Unindent();
                                    }
                                }

                                ImGui.Unindent();
                            }
                        }
                        ImGui.Unindent();
                    }
                }

                ImGui.End();
            }

            ImGui.PopID();
        }

        class InstancedContent
        {
            public string SeasonName = "";
            public SortedDictionary<string, RaidEntry> Raids = new(); // Key = DungeonId
            public SortedDictionary<string, MasterEntry> Masters = new(); // Key = DungeonId
            public SortedDictionary<string, DungeonEntry> Dungeons = new(); // Key = FunctionId
            public MasterScoreData MasterScores = new();
            //public SortedDictionary<int, MasterAwardEntry> MasterAwards = new(); // Key = MasterAwardId
        }

        class ContentEntry
        {
            public int FunctionId = 0;
            public string FunctionName = "";
            //public string ContentType = "";
            public int Id = 0;
            public string Name = "";
            //public int DifficultyInt = 0;
            public string DifficultyName = "";
        }

        class MasterScoreData
        {
            public int TotalScore = 0;
            public SortedDictionary<int, MasterScoreEntry> HighestScores = new(); // Key = DungeonId
            public SortedDictionary<int, MasterAwardEntry> MasterAwards = new(); // Key = MasterAwardId
        }

        class MasterAwardEntry
        {
            public int AwardId = 0;
            public int IsUnlocked = 0;
        }

        class RaidEntry : ContentEntry
        {
            public SortedSet<RaidBossData> Bosses = new();
        }

        class MasterEntry : ContentEntry
        {
            public SortedSet<MasterData> Masters = new();
        }

        class DungeonEntry : ContentEntry
        {
            public SortedDictionary<int, DungeonData> Dungeons = new(); // Key = DungeonId
        }

        class RaidBossData : IComparable<RaidBossData>
        {
            public int Id = 0;
            public string Name = "";
            public int AwardCount = 0;

            public int CompareTo(RaidBossData? other)
            {
                if (other == null)
                {
                    return 1;
                }

                int result = this.Id.CompareTo(other.Id);

                if (result == 0)
                {
                    result = string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
                }

                return result;
            }
        }

        class MasterData : IComparable<MasterData>
        {
            public int MasterLevel = 0;
            public int CompleteCount = 0;
            public int PassTime = 0;
            public TimeSpan BestRecord = TimeSpan.Zero; // PassTime stored as TimeSpan
            public int Score = 0;

            public int CompareTo(MasterData? other)
            {
                if (other == null)
                {
                    return 1;
                }

                int result = this.MasterLevel.CompareTo(other.MasterLevel);

                return result;
            }
        }

        class DungeonData : IComparable<DungeonData>
        {
            public int DungeonId = 0;
            public string DungeonName = "";
            public int Completions = 0;
            public int PassTime = 0;
            public TimeSpan BestRecord = TimeSpan.Zero; // PassTime stored as TimeSpan
            public int Score = 0;

            public int CompareTo(DungeonData? other)
            {
                if (other == null)
                {
                    return 1;
                }

                int result = this.DungeonId.CompareTo(other.DungeonId);

                if (result == 0)
                {
                    result = string.Compare(this.DungeonName, other.DungeonName, StringComparison.OrdinalIgnoreCase);
                }

                return result;
            }
        }

        class ImagineEntry
        {
            public int Id = 0;
            public string Name = "";
            public int Tier = 0;
            public string MonsterName = "";
            public int Rarity = 0;
        }

        class MasterScoreEntry : MasterData
        {
            public int DungeonId = 0;
            public string Name = "";
        }

        static float MenuBarButtonWidth = 0.0f;
        public static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                var windowSettings = Settings.Instance.WindowSettings.PlayerMetrics;

                MenuBarSize = ImGui.GetWindowSize();

                ImGui.Text($"{TITLE}");

                bool debug = false;
                if (debug)
                {
                    ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 6));
                    ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                    if (ImGui.MenuItem($"{FASIcons.Download}##LoadFromFileBtn"))
                    {
                        charSerialize = Newtonsoft.Json.JsonConvert.DeserializeObject<Zproto.CharSerialize>(File.ReadAllText("_CharSerialize.json"));
                        ShouldProcessData = true;
                    }
                    ImGui.PopFont();
                    ImGui.SetItemTooltip("Load _CharSerialize.json");
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 5));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.BeginDisabled(charSerialize == null);
                if (ImGui.MenuItem($"{FASIcons.Copy}##CopyDataBtn"))
                {
                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(Instances);
                    ImGui.SetClipboardText(data);
                }
                ImGui.EndDisabled();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Copy 'Content Progression' to Clipboard.");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 4));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, TransparentBackground ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Crop}##ToggleTransparentBackgroundBtn"))
                {
                    TransparentBackground = !TransparentBackground;
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Toggle window background transparency.");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, AppState.MousePassthrough ? 0.0f : 1.0f, windowSettings.TopMost ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{FASIcons.Thumbtack}##TopMostBtn"))
                {
                    if (!windowSettings.TopMost)
                    {
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                        windowSettings.TopMost = true;
                        IsPinned = true;
                    }
                    else
                    {
                        Utils.UnsetWindowTopmost();
                        Utils.SetWindowOpacity(1.0f);
                        windowSettings.TopMost = false;
                        IsPinned = false;
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Pin Window As Top Most");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, CollapseToContentOnly ? 1.0f : 0.5f));
                if (ImGui.MenuItem($"{(CollapseToContentOnly ? FASIcons.AnglesDown : FASIcons.AnglesUp)}##CollapseToContentBtn"))
                {
                    CollapseToContentOnly = !CollapseToContentOnly;
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                if (CollapseToContentOnly)
                {
                    ImGui.SetItemTooltip("Expand To Full Options");
                }
                else
                {

                    ImGui.SetItemTooltip("Collapse To Content Only");
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"X##CloseBtn"))
                {
                    windowSettings.WindowSize = ImGui.GetWindowSize();
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }
    }

    public class PlayerMetricsWindowSettings : WindowSettingsBase
    {

    }
}
