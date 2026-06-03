using BPSR_ZDPS.DataTypes.Modules;
using Serilog;
using System.Diagnostics;
using System.Numerics;
using ZLinq;

namespace BPSR_ZDPS.Managers
{
    public partial class ModuleOptimizer
    {
        private SolverResult NormalV2(SolverConfig config, PlayerModDataSave playerMods, Stopwatch sw, List<long> filtered, CancellationToken cancelToken)
        {
            //var limitedAndFiltered = LimitDuplicates(playerMods, filtered, config.NumModules);

            //Log.Information("NormalV2 module solver: NumFilteredMods: {NumFiltered}, LimitedAndFiltered: {limitedAndFiltered}", filtered.Count, limitedAndFiltered.Count);
            var modStatVecs = ModulesToVectors(playerMods, filtered);

            var possibleStats = NormalizeStatsLookup(playerMods, filtered);
            var statPrios = NormalizeStats(config, possibleStats);

            var numCombos = CountCombinations(modStatVecs.Length, config.NumModules);
            Log.Information("Searching {NumCombos:N0} combos...", numCombos);
            int numModuleCombosProcessed = 0;

            var items = new List<Module>();
            for (int itemIdx = 0; itemIdx < modStatVecs.Length; itemIdx++)
            {
                Vector<byte> vec = modStatVecs[itemIdx];
                var item = new Module();
                item.Id = itemIdx;

                for (int i = 0; i < Vector<byte>.Count; i++)
                {
                    if (vec[i] > 0)
                    {
                        item.Stats.Add(i, vec[i]);
                    }
                }

                items.Add(item);
            }

            if (!StatsSanityCheck(modStatVecs, statPrios.ToArray()))
            {
                Log.Information("Not able to reach required thresholds.");
                return new SolverResult()
                {
                    FilteredModules = filtered
                };
            }

            // ToDo: Make this and the brute force ones use a more common interface
            var optimizer = new ModuleOptimizerTest();
            optimizer.LinkLevelBonus = config.LinkLevelBonus;
            optimizer.NumModules = config.NumModules;
            var bests = optimizer.FindBestSet(items, statPrios, cancelToken);

            Log.Information("Num Module Combos Processed: {NumCombos:N0}", numModuleCombosProcessed);

            var results = new List<ModComboResult>();
            foreach (var best in bests)
            {
                var result = new ModComboResult();
                result.ModuleSet = new ModuleSet()
                {
                    Mod1 = best.Items.Count >= 1 ? (int)best.Items[0].Id : -1,
                    Mod2 = best.Items.Count >= 2 ? (int)best.Items[1].Id : -1,
                    Mod3 = best.Items.Count >= 3 ? (int)best.Items[2].Id : -1,
                    Mod4 = best.Items.Count >= 4 ? (int)best.Items[3].Id : -1,
                    Mod5 = best.Items.Count >= 5? (int)best.Items[4].Id : -1
                };

                var coreStats = new Dictionary<long, PowerCore>();
                var mods = result.ModuleSet.Mods;
                for (int i = 0; i < mods.Length; i++)
                {
                    if (mods[i] == -1)
                    {
                        break;
                    }

                    var modId = filtered[mods[i]];
                    var powerCores = GetModPowerCores(playerMods, modId);
                    foreach (var powerCore in powerCores)
                    {
                        if (coreStats.TryGetValue(powerCore.Id, out var existingCore))
                        {
                            existingCore.Value += powerCore.Value;
                            coreStats[powerCore.Id] = existingCore;
                        }
                        else
                        {
                            coreStats.Add(powerCore.Id, powerCore);
                        }
                    }
                }

                var reslovedModSet = config.NumModules switch
                {
                    5 => new ModuleSet()
                    {
                        Mod1 = (int)filtered[mods[0]],
                        Mod2 = (int)filtered[mods[1]],
                        Mod3 = (int)filtered[mods[2]],
                        Mod4 = (int)filtered[mods[3]],
                        Mod5 = (int)filtered[mods[4]]
                    },

                    4 => new ModuleSet()
                    {
                        Mod1 = (int)filtered[mods[0]],
                        Mod2 = (int)filtered[mods[1]],
                        Mod3 = (int)filtered[mods[2]],
                        Mod4 = (int)filtered[mods[3]]
                    },

                    3 => new ModuleSet()
                    {
                        Mod1 = (int)filtered[mods[0]],
                        Mod2 = (int)filtered[mods[1]],
                        Mod3 = (int)filtered[mods[2]],
                        Mod4 = -1
                    },

                    2 => new ModuleSet()
                    {
                        Mod1 = (int)filtered[mods[0]],
                        Mod2 = (int)filtered[mods[1]],
                        Mod3 = -1,
                        Mod4 = -1
                    },

                    1 => new ModuleSet()
                    {
                        Mod1 = (int)filtered[mods[0]],
                        Mod2 = -1,
                        Mod3 = -1,
                        Mod4 = -1
                    }
                };

                result.Stats = OrderPowerCoresByPriorities(coreStats.Values.ToArray(), config.StatPriorities);
                result.Score = 0;
                result.CombatScore = CalcCombosCombatScore(playerMods, reslovedModSet);

                results.Add(result);
            }

            return new SolverResult()
            {
                BestModResults = results,
                FilteredModules = filtered
            };
        }

        private static List<StatPrio> NormalizeStats(SolverConfig config, Dictionary<int, int> possibleStats)
        {
            var statPrios = new List<StatPrio>();
            foreach (var statPrio in config.StatPriorities)
            {
                if (possibleStats.ContainsKey(statPrio.Id))
                {
                    var newStatPrio = new StatPrio();
                    newStatPrio.Id = possibleStats[statPrio.Id];
                    newStatPrio.ReqLevel = statPrio.ReqLevel;
                    newStatPrio.MinLevel = statPrio.MinLevel;
                    newStatPrio.StatMode = statPrio.StatMode;
                    statPrios.Add(newStatPrio);
                }
            }

            return statPrios;
        }

        private static Dictionary<int, int> NormalizeStatsLookup(PlayerModDataSave playerMods, List<long> filtered)
        {
            int statIdx = 0;
            var possibleStats = filtered.AsValueEnumerable().SelectMany(x =>
                playerMods.ModulesPackage.Items[x].ModNewAttr.ModParts)
                    .Distinct().Order().ToDictionary(x => x, y => statIdx++);
            return possibleStats;
        }

        private static Vector<byte>[] ModulesToVectors(PlayerModDataSave playerMods, List<long> filtered)
        {
            int statIdx = 0;
            var possibleStats = filtered.AsValueEnumerable().SelectMany(x =>
                playerMods.ModulesPackage.Items[x].ModNewAttr.ModParts)
                    .Distinct().Order().ToDictionary(x => x, y => statIdx++);

            var vecCount = Vector<byte>.Count;
            Vector<byte>[] modStatValues = new Vector<byte>[filtered.Count];
            for (int i = 0; i < filtered.Count; i++)
            {
                long modId = filtered[i];
                var modStatIds = playerMods.ModulesPackage.Items[modId].ModNewAttr.ModParts;

                int linkIdx = 0;
                var mod1Ids = new byte[vecCount];
                foreach (var statId in modStatIds)
                {
                    var idx = possibleStats[statId];
                    mod1Ids[idx] = (byte)playerMods.Mod.ModInfos[modId].InitLinkNums[linkIdx++];
                }

                var vec = new Vector<byte>(mod1Ids);
                modStatValues[i] = vec;
            }

            return modStatValues;
        }

        private static long CountCombinations(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;

            if (k > n - k)
                k = n - k;

            long result = 1;

            for (int i = 1; i <= k; i++)
            {
                result = result * (n - i + 1) / i;
            }

            return result;
        }

        // Check if it is possible at all to hit this stat threshold
        private static bool StatsSanityCheck(ReadOnlySpan<Vector<byte>> modules, ReadOnlySpan<StatPrio> prios)
        {
            var total = new Vector<byte>();
            var status = new bool[prios.Length];

            for (int i = 0; i < modules.Length; i++)
            {
                total = Vector.Add(total, modules[i]);

                for (int x = 0; x < prios.Length; x++)
                {
                    var prio = prios[x];
                    if (status[x])
                    {
                        continue;
                    }

                    if (total[prio.Id] >= prio.ReqLevel)
                    {
                        status[x] = true;
                    }
                }

                var allReached = true;
                for (int x = 0; x < status.Length; x++)
                {
                    if (!status[x])
                        allReached = false;
                }

                if (allReached)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<long> LimitDuplicates(PlayerModDataSave inv, List<long> ids, int maxPerType = 5)
        {
            var grouped = new Dictionary<ulong, List<long>>();
            foreach (var id in ids)
            {
                var moduleId = GetModuleId(inv, id);
                if (grouped.ContainsKey(moduleId))
                {
                    grouped[moduleId].Add(id);
                }
                else
                {
                    var list = new List<long>();
                    list.Add(id);
                    grouped[moduleId] = list;
                }
            }

            var filtered = new List<long>();
            foreach (var group in grouped)
            {
                var limited = group.Value.Take(maxPerType);
                filtered.AddRange(limited);
            }

            return filtered;
        }

        public class Module
        {
            public int Id;
            public Dictionary<int, int> Stats = new();
        }

        public class ModuleSetResult
        {
            public List<Module> Items;
            public int[] Score;
        }

        public class ModuleOptimizerTest
        {
            readonly PriorityScoreComparer ScoreComparer = new();
            public const int MAX_STAT_VALUE = 20;
            public const int MAX_NUM_STATS = 21; // Update to pull from json

            public List<ModuleSetResult> TopResults = new();
            public int[] BestScore;
            public int NumModules = 5;
            public int MaxCanadates = 1000000;
            public int NumTopResults = 10;
            public byte[] LinkLevelBonus = [];

            public List<ModuleSetResult> FindBestSet(List<Module> items, List<StatPrio> priorities, CancellationToken cancelToken)
            {
                TopResults = new();
                BestScore = new int[priorities.Count];

                items = items.OrderBy(i => EstimateItemScore(i, priorities), ScoreComparer).ToList();
                Search(items, priorities, 0, new List<Module>(), new int[MAX_NUM_STATS], cancelToken);

                return TopResults;
            }

            void Search(List<Module> items, List<StatPrio> priorities, int start, List<Module> currentSet, int[] totals, CancellationToken cancelToken)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                if (currentSet.Count == NumModules)
                {
                    var score = ScoreTotals(totals, priorities);
                    AddSolution(currentSet, score);
                    return;
                }

                if (start >= items.Count)
                {
                    return;
                }

                /*
                for (int i = start; i < items.Count; i++)
                {
                    var item = items[i];

                    foreach (var stat in item.Stats)
                    {
                        totals[stat.Key] += stat.Value;
                    }

                    currentSet.Add(item);

                    // Branch-and-bound prune
                    if (CanStillBeatBest(items, priorities, i + 1, currentSet.Count, totals))
                    {
                        Search(items, priorities, i + 1, currentSet, totals, cancelToken);
                    }

                    currentSet.RemoveAt(currentSet.Count - 1);

                    foreach (var stat in item.Stats)
                    {
                        totals[stat.Key] -= stat.Value;
                    }
                }*/

                var candidates = new List<(int Index, int[] Score)>();

                for (int i = start; i < items.Count; i++)
                {
                    Module item = items[i];

                    var projected = new int[MAX_NUM_STATS];
                    Array.Copy(totals, projected, MAX_NUM_STATS);

                    foreach (var stat in item.Stats)
                    {
                        projected[stat.Key] += stat.Value;
                    }

                    var projectedScore = ScoreTotals(projected, priorities);

                    candidates.Add((i, projectedScore));
                }

                var topCandidates = candidates.OrderBy(x => x.Score, ScoreComparer).Take(MaxCanadates);

                foreach (var candidate in topCandidates)
                {
                    var i = candidate.Index;

                    Module item = items[i];

                    foreach (var stat in item.Stats)
                    {
                        totals[stat.Key] += stat.Value;
                    }

                    currentSet.Add(item);

                    if (CanStillBeatBest(items, priorities, i + 1, currentSet.Count, totals))
                    {
                        Search(items, priorities, i + 1, currentSet, totals, cancelToken);
                    }

                    currentSet.RemoveAt(currentSet.Count - 1);

                    foreach (var stat in item.Stats)
                    {
                        totals[stat.Key] -= stat.Value;
                    }
                }
            }

            int[] ScoreTotals(int[] totals, List<StatPrio> priorities, bool noPenalty = false)
            {
                var scores = new int[priorities.Count];

                for (int i = 0; i < priorities.Count; i++)
                {
                    var p = priorities[i];

                    int target = Math.Max(1, p.ReqLevel);
                    int val = Math.Min(totals[p.Id], MAX_STAT_VALUE);

                    if (!noPenalty && (val < p.ReqLevel || (p.StatMode == StatMode.Exactly && p.ReqLevel != val )))
                    {
                        var minValue = new int[priorities.Count];
                        for (int x = 0; x < minValue.Length; x++)
                        {
                            minValue[x] = int.MinValue;
                        }

                        return new int[priorities.Count];
                        //scores[i] = int.MinValue;
                    }
                    else
                    {
                        var bonusIdx = val switch
                        {
                            >= 20 => 5,
                            >= 16 => 4,
                            >= 12 => 3,
                            >= 8 => 2,
                            >= 4 => 1,
                            >= 1 => 0,
                            _ => 0
                        };

                        var breakPointBonus = (byte)LinkLevelBonus[bonusIdx];

                        scores[i] = val + breakPointBonus;
                    }
                }

                return scores;
            }

            bool CanStillBeatBest(List<Module> items, List<StatPrio> priorities, int nextIndex, int chosenCount, int[] currentTotals)
            {
                int remaining = NumModules - chosenCount;

                if (remaining <= 0)
                    return true;

                int[] optimistic = new int[MAX_NUM_STATS];
                Array.Copy(currentTotals, optimistic, MAX_NUM_STATS);

                foreach (var p in priorities)
                {
                    int added = 0;
                    int taken = 0;

                    for (int i = nextIndex; i < items.Count && taken < remaining; i++)
                    {
                        if (items[i].Stats.TryGetValue(p.Id, out int val))
                        {
                            added += val;
                            taken++;
                        }
                    }

                    var bonusIdx = added switch
                    {
                        >= 20 => 5,
                        >= 16 => 4,
                        >= 12 => 3,
                        >= 8 => 2,
                        >= 4 => 1,
                        >= 1 => 0,
                        _ => 0
                    };

                    var breakPointBonus = (byte)LinkLevelBonus[bonusIdx];

                    optimistic[p.Id] = added + breakPointBonus;
                }

                int[] upperBound = ScoreTotals(optimistic, priorities, true);

                return ScoreComparer.Compare(upperBound, BestScore) < 0;
            }

            int[] EstimateItemScore(Module item, List<StatPrio> priorities)
            {
                var scores = new int[priorities.Count];

                for (int i = 0; i < priorities.Count; i++)
                {
                    var p = priorities[i];

                    if (item.Stats.TryGetValue(p.Id, out int val))
                    {
                        scores[i] = Math.Min(val, MAX_STAT_VALUE);
                    }
                    else
                    {
                        scores[i] = 0;
                    }
                }

                return scores;
            }

            void AddSolution(List<Module> currentSet, int[] score)
            {
                // TODO: Come back and make this less allocation messy, eg. not strings
                var ids = currentSet.Select(x => x.Id).OrderBy(x => x).ToArray();

                string key = string.Join(",", ids);

                if (TopResults.Any(s => string.Join(",", s.Items.Select(i => i.Id).OrderBy(x => x)) == key))
                    return;

                TopResults.Add(
                    new ModuleSetResult
                    {
                        Items = new (currentSet),
                        Score = score
                    });

                TopResults = TopResults.OrderBy(s => s.Score, ScoreComparer).Take(NumTopResults).ToList();

                if (TopResults.Count == NumTopResults)
                    BestScore = TopResults.Last().Score;
            }
        }

        sealed class PriorityScoreComparer : IComparer<int[]>
        {
            public int Compare(int[]? a, int[]? b)
            {
                if (ReferenceEquals(a, b))
                    return 0;

                if (a is null)
                    return 1;

                if (b is null)
                    return -1;

                int len = Math.Min(a.Length, b.Length);

                for (int i = 0; i < len; i++)
                {
                    if (a[i] > b[i])
                        return -1;

                    if (a[i] < b[i])
                        return 1;
                }

                return b.Length.CompareTo(a.Length);
            }
        }
    }
}
