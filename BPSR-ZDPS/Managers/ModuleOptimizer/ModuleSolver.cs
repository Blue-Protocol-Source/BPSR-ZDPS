using BPSR_ZDPS.DataTypes.Modules;
using Serilog;
using System.Diagnostics;
using System.Numerics;
using ZLinq;

namespace BPSR_ZDPS.Managers.Modules
{
    public class ModuleOptimizerBase
    {
        public const int MAX_STAT_VALUE = 20;
        public const int MAX_SUPPORTED_STATS = 32;
        protected SolverConfig Config;
        protected PlayerModDataSave PlayerModInv;
        protected List<long> Filtered;
        protected Stopwatch StopWatch;
        protected CancellationToken CancellationToken;
        protected List<StatPrio> NormalizedStatPrios;

        public ModuleOptimizerBase(SolverConfig config, PlayerModDataSave playerMods, Stopwatch sw, List<long> filtered, CancellationToken cancelToken)
        {
            Config = config;
            PlayerModInv = playerMods;
            Filtered = filtered;
            StopWatch = sw;
            CancellationToken = cancelToken;
        }

        public virtual SolverResult Solve()
        {
            var limitedAndFiltered = LimitDuplicates(PlayerModInv, Filtered, Config.NumModules);

            Log.Information("NumFilteredMods: {NumFiltered}, LimitedAndFiltered: {limitedAndFiltered}", Filtered.Count, limitedAndFiltered.Count);
            var modStatVecs = ModulesToVectors(PlayerModInv, Filtered);

            var possibleStats = NormalizeStatsLookup(PlayerModInv, Filtered);
            NormalizedStatPrios = NormalizeStats(Config, possibleStats);

            if (!StatsSanityCheck(modStatVecs, NormalizedStatPrios.ToArray()))
            {
                Log.Information("Not able to reach required thresholds.");
                return new SolverResult()
                {
                    FilteredModules = Filtered
                };
            }

            var numCombos = CountCombinations(modStatVecs.Length, Config.NumModules);
            Log.Information($"Searching {numCombos:N0} combos");

            var moduleResults = InnerSolve(modStatVecs);
            var resolved = ResloveResults(moduleResults);

            var result = new SolverResult()
            {
                BestModResults = resolved.ToList(),
                FilteredModules = Filtered
            };

            return result;
        }

        public virtual ModComboResult[] InnerSolve(Vector<byte>[] mods)
        {

            return new ModComboResult[1];
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

        public static ulong GetModuleId(PlayerModDataSave playerMods, long id)
        {
            var modItem = playerMods.ModulesPackage.Items[id];
            var modInfo = playerMods.Mod.ModInfos[id];

            var modIds = new (byte, byte)[3];
            for (int i = 0; i < modItem.ModNewAttr.ModParts.Count; i++)
            {
                var idAndVal = ((byte)modItem.ModNewAttr.ModParts[i], (byte)modInfo.InitLinkNums[i]);
                modIds[i] = idAndVal;
            }

            modIds = modIds.OrderBy(x => x.Item1).ToArray();

            ulong moduleId = 0;

            foreach (var (modId, modVal) in modIds)
            {
                moduleId = (moduleId << 8) | modId;
                moduleId = (moduleId << 8) | modVal;
            }

            return moduleId;
        }

        private ushort GetStatMultiplier(SolverConfig config, int statId)
        {
            for (int i = 0; i < config.StatPriorities.Count; i++)
            {
                if (config.StatPriorities[i].Id == statId)
                {
                    var idx = config.StatPriorities.Count - i;
                    return (ushort)(idx + 1);
                }
            }

            return (ushort)(config.ValueAllStats ? 1 : 0);
        }

        private ushort CalcModuleScore(SolverConfig config, PlayerModDataSave playerMods, long id)
        {
            var modItem = playerMods.ModulesPackage.Items[id];
            var modInfo = playerMods.Mod.ModInfos[id];
            ushort score = 0;
            for (int i = 0; i < modItem.ModNewAttr.ModParts.Count; i++)
            {
                var statId = modItem.ModNewAttr.ModParts[i];
                var statValue = modInfo.InitLinkNums[i];
                var statMultiplier = GetStatMultiplier(config, statId);
                score += (ushort)(statValue * statMultiplier);
            }

            return score;
        }

        private List<PowerCore> GetModPowerCores(PlayerModDataSave playerMods, long id)
        {
            var powerCores = new List<PowerCore>();

            if (id == -1)
            {
                return powerCores;
            }

            var modItem = playerMods.ModulesPackage.Items[id];
            var modInfo = playerMods.Mod.ModInfos[id];
            for (int i = 0; i < modItem.ModNewAttr.ModParts.Count; i++)
            {
                var statId = modItem.ModNewAttr.ModParts[i];
                var statValue = modInfo.InitLinkNums[i];

                var powerCore = new PowerCore()
                {
                    Id = statId,
                    Value = statValue,
                };

                powerCores.Add(powerCore);
            }

            return powerCores;
        }

        public int CalcCombosCombatScore(PlayerModDataSave playerMods, ModuleSet modSet)
        {
            Dictionary<int, PowerCore> coresDict = [];
            foreach (var mod in modSet.Mods)
            {
                if (mod != 0)
                {
                    var modCores = GetModPowerCores(playerMods, mod);
                    foreach (var modCore in modCores)
                    {
                        if (coresDict.ContainsKey(modCore.Id))
                        {
                            var core = coresDict[modCore.Id];
                            core.Value += modCore.Value;
                            coresDict[modCore.Id] = core;
                        }
                        else
                        {
                            coresDict[modCore.Id] = modCore;
                        }
                    }
                }
            }

            int cs = 0;
            foreach (var core in coresDict.Values)
            {
                var enhanceLevel = 0;
                int[] enhanceLevels = [1, 4, 8, 12, 16, 20];
                for (int i = 0; i < 6; i++)
                {
                    if (core.Value >= enhanceLevels[i])
                    {
                        enhanceLevel = enhanceLevels[i];
                    }
                    else
                    {
                        break;
                    }
                }

                var statCs = ModuleSolver.StatCombatScores.TryGetValue($"{core.Id}_{enhanceLevel}", out var temp) ? temp : 0;
                cs += statCs;
            }

            var enhancementLevels = coresDict.Values.Sum(x => x.Value);
            var enhancementScore = HelperMethods.DataTables.ModLinkEffects.Data.TryGetValue(enhancementLevels + 1, out var tempScore) ? tempScore?.FightValue ?? 0 : 0;

            return cs + enhancementScore;
        }

        protected ModComboResult[] ResloveResults(ModComboResult[] results)
        {
            var resolvedSets = new ModComboResult[results.Length];

            for (int i1 = 0; i1 < results.Length; i1++)
            {
                ModComboResult modSet = results[i1];
                var coreStats = new Dictionary<long, PowerCore>();
                var mods = modSet.ModuleSet.Mods;
                for (int i = 0; i < mods.Length; i++)
                {
                    if (mods[i] == -1)
                    {
                        break;
                    }

                    var modId = Filtered[mods[i]];
                    var powerCores = GetModPowerCores(PlayerModInv, modId);
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

                modSet.Stats = OrderPowerCoresByPriorities(coreStats.Values.ToArray(), Config.StatPriorities);

                var reslovedModSet = new ModuleSet()
                {
                    Mod1 = mods[0] != -1 ? (int)Filtered[mods[0]] : -1,
                    Mod2 = mods[1] != -1 ? (int)Filtered[mods[1]] : -1,
                    Mod3 = mods[2] != -1 ? (int)Filtered[mods[2]] : -1,
                    Mod4 = mods[3] != -1 ? (int)Filtered[mods[3]] : -1,
                    Mod5 = mods[4] != -1 ? (int)Filtered[mods[4]] : -1
                };

                modSet.CombatScore = CalcCombosCombatScore(PlayerModInv, reslovedModSet);

                resolvedSets[i1] = modSet;
            }

            return resolvedSets;
        }

        public static PowerCore[] OrderPowerCoresByPriorities(PowerCore[] cores, List<StatPrio> priorities)
        {
            var order = priorities.Select(x => x.Id).ToList();
            var ordered = cores.OrderBy(x => (uint)order.IndexOf(x.Id)).ToArray();

            return ordered;
        }

        protected int GetLinkLevelBoost(int linkLevel)
        {
            var bonusIdx = linkLevel switch
            {
                >= 20 => 5,
                >= 16 => 4,
                >= 12 => 3,
                >= 8 => 2,
                >= 4 => 1,
                >= 1 => 0,
                _ => 0
            };

            var breakPointBonus = (byte)Config.LinkLevelBonus[bonusIdx];

            return breakPointBonus;
        }

        #region Classes / Structs
        public unsafe struct ModuleSetIndices
        {
            public ModuleSetIndices()
            {
                unsafe
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ModArr[i] = -1;
                    }
                }
            }

            public fixed short ModArr[5];

            public short Mod1 { get { unsafe { return ModArr[0]; } } set { unsafe { ModArr[0] = value; } } }
            public short Mod2 { get { unsafe { return ModArr[1]; } } set { unsafe { ModArr[1] = value; } } }
            public short Mod3 { get { unsafe { return ModArr[2]; } } set { unsafe { ModArr[2] = value; } } }
            public short Mod4 { get { unsafe { return ModArr[3]; } } set { unsafe { ModArr[3] = value; } } }
            public short Mod5 { get { unsafe { return ModArr[4]; } } set { unsafe { ModArr[4] = value; } } }

            unsafe bool Equals(ModuleSetIndices other)
            {
                Span<short> a = stackalloc short[5];
                Span<short> b = stackalloc short[5];

                for (int i = 0; i < 5; i++)
                {
                    a[i] = ModArr[i];
                    b[i] = other.ModArr[i];
                }

                a.Sort();
                b.Sort();

                return a.SequenceEqual(b);
            }

            public override bool Equals(object? obj)
            {
                return obj is ModuleSetIndices other && Equals(other);
            }

            public override int GetHashCode()
            {
                Span<short> values = stackalloc short[5];

                for (int i = 0; i < 5; i++)
                    values[i] = ModArr[i];

                values.Sort();

                HashCode hash = new();

                foreach (short value in values)
                    hash.Add(value);

                return hash.ToHashCode();
            }

            public static bool operator ==(ModuleSetIndices left, ModuleSetIndices right)
                => left.Equals(right);

            public static bool operator !=(ModuleSetIndices left, ModuleSetIndices right)
                => !left.Equals(right);
        }
        #endregion
    }
}
