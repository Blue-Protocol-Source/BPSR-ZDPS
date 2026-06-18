using BPSR_ZDPS.DataTypes.Modules;
using BPSR_ZDPS.Managers.Modules;
using Serilog;
using System.Diagnostics;
using System.Numerics;
using ZLinq;

namespace BPSR_ZDPS.Managers
{
    public class ModuleOptimizerBeam : ModuleOptimizerBase
    {
        public int BeamWidth = 25000;
        private int[] StatScoreLookup;
        private byte[] RequirementMetLookup;
        private ushort[] StatProgressLookup;
        private byte[] StatIndexes;

        public ModuleOptimizerBeam(SolverConfig config, PlayerModDataSave playerMods, Stopwatch sw, List<long> filtered, CancellationToken cancelToken) : base(config, playerMods, sw, filtered, cancelToken)
        {
            var numElements = Vector<byte>.Count * (MAX_STAT_VALUE + 1);
            StatScoreLookup = new int[numElements];
            RequirementMetLookup = new byte[numElements];
            StatProgressLookup = new ushort[numElements];
        }

        public override ModComboResult[] InnerSolve(Vector<byte>[] mods)
        {
            StatIndexes = NormalizedStatPrios.Select(x => (byte)x.Id).ToArray();
            BuildStatScoreLookup(NormalizedStatPrios);

            var beam = new List<BeamNode>();
            beam.Add(new BeamNode() { Score = 0, CurrentSet = new ModuleSetIndices() });

            for (int i = 0; i < Config.NumModules; i++)
            {
                var candidates = new List<BeamNode>();

                Log.Information($"Depth={i}, Beam={beam.Count}, EstimatedCandidates={beam.Count * mods.Length:N0}");

                Parallel.ForEach(beam,
                    () => new TopK(BeamWidth),
                    (node, _, local) =>
                    {
                        for (int modIdx = 0; modIdx < mods.Length; modIdx++)
                        {
                            Vector<byte> mod = mods[modIdx];
                            var skip = false;
                            unsafe
                            {
                                for (int modIdx2 = 0; modIdx2 < Config.NumModules; modIdx2++)
                                {
                                    if (node.CurrentSet.ModArr[modIdx2] == modIdx)
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                            }

                            if (skip)
                                continue;

                            var newCandidate = new BeamNode()
                            {
                                CurrentSet = new ModuleSetIndices()
                            };

                            newCandidate.CurrentSet = node.CurrentSet;
                            unsafe
                            {
                                newCandidate.CurrentSet.ModArr[node.Depth] = (short)modIdx;
                            }

                            newCandidate.Totals = Vector.Add(node.Totals, mod);
                            newCandidate.Depth = (byte)(i + 1);

                            ScoreBeamNode(ref newCandidate);

                            local.Add(newCandidate);
                            if (CancellationToken.IsCancellationRequested)
                                return local;
                        }

                        return local;
                    },
                    local =>
                    {
                        lock (candidates)
                        {
                            candidates.AddRange(local.ToList());
                        }
                    });

                if (CancellationToken.IsCancellationRequested)
                    return [];

                var sw = Stopwatch.StartNew();
                beam = GetTopK(candidates, BeamWidth);

                sw.Stop();

                Log.Information($"Candidate order took: {sw.Elapsed}, depth: {i}");
            }

            var meetsRequirements = beam.Where(x => x.RequirementsMet == NormalizedStatPrios.Count);
            var distinctTop = GetDistinctTopResults(meetsRequirements.DistinctBy(x => x.GetHash()), 10, 10);
            var bestX = distinctTop.AsValueEnumerable()
                .Select(x => BeamToResult(x))
                .ToArray();

            return bestX;
        }

        protected void BuildStatScoreLookup(List<StatPrio> statPrios)
        {
            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                for (int x = 0; x <= MAX_STAT_VALUE; x++)
                {
                    var idx = i * (MAX_STAT_VALUE + 1) + x;
                    var statPrio = statPrios.FirstOrDefault(stat => stat.Id == i);
                    var statIdx = statPrios.IndexOf(statPrio);

                    if (statPrio != null)
                    {
                        var realStatId = Config.StatPriorities[statIdx];
                        var isLegendary = ModuleSolver.LegendaryStats.Contains(realStatId.Id);
                        var reqLevel = Math.Max((byte)0, statPrio.ReqLevel);

                        if (statPrio.StatMode == StatMode.Exactly)
                        {
                            var score = CalcScore(MAX_STAT_VALUE, statIdx, NormalizedStatPrios.Count, isLegendary);

                            if (x == statPrio.ReqLevel)
                            {
                                StatScoreLookup[idx] = score;
                                RequirementMetLookup[idx] = 1;

                                var pct = statPrio.ReqLevel > 0 ? Math.Min(x, statPrio.ReqLevel) * 100 / statPrio.ReqLevel : 100;
                                StatProgressLookup[idx] = (ushort)pct;

                                Debug.WriteLine($"Set idx: {idx} to {score}, stat: {(ModuleSolver.GetModInfo(Config.StatPriorities[statIdx].Id)).Name}");
                            }
                            else
                            {
                                var progress = x / (double)reqLevel;
                                StatScoreLookup[idx] = (int)(score * progress);
                            }
                        }
                        else
                        {
                            var score = CalcScore(x, statIdx, NormalizedStatPrios.Count, isLegendary);

                            if (x >= reqLevel)
                            {
                                StatScoreLookup[idx] = score;
                                RequirementMetLookup[idx] = 1;
                                Debug.WriteLine($"Set idx: {idx} to {score} (base value: {x}), stat: {(ModuleSolver.GetModInfo(Config.StatPriorities[statIdx].Id).Name)}");
                            }
                            else
                            {
                                var progress = x / (double)reqLevel;
                                StatScoreLookup[idx] = (int)(score * progress);
                            }

                            var pct = statPrio.ReqLevel > 0 ? Math.Min(x, statPrio.ReqLevel) * 100 / statPrio.ReqLevel : 100;
                            StatProgressLookup[idx] = (ushort)pct;
                        }
                    }
                    else if (Config.ValueAllStats)
                    {
                        var score = (int)(CalcScore(x, NormalizedStatPrios.Count + 1, NormalizedStatPrios.Count + 1) * 0.95);
                        StatScoreLookup[idx] = score;
                        RequirementMetLookup[idx] = 1;
                        StatProgressLookup[idx] = (ushort)100;
                    }
                }
            }
        }

        public static float GetOrderBoost(float strength, int itemPos, int numItems)
        {
            // var weight = Math.Exp(-strength * itemPos);
            var weight = 1.0 / Math.Pow(itemPos + 1, strength);
            var boost = (numItems) * weight;

            return (float)Math.Max(1, boost);
        }

        protected int CalcScore(int statValue, int statIdx, int numStats, bool isLegendary = false)
        {
            var breakPointBonus = GetLinkLevelBoost(statValue);
            float stat = Math.Min(statValue, MAX_STAT_VALUE);
            var statOrder = (NormalizedStatPrios.Count - (statIdx));
            var orderBoost = GetOrderBoost(Config.OrderBoostStrength, statIdx, numStats);

            if (isLegendary)
            {
                stat = stat * Config.LegendaryStatMultiplier;
            }

            int score = 0;

            if (Config.ScoreMode == SolverConfig.ScoringMode.Stat_Order_Boost_Mul)
            {
                score = (int)(stat * orderBoost * breakPointBonus);
            }
            else if (Config.ScoreMode == SolverConfig.ScoringMode.Stat_Boost_Add_Order)
            {
                score = (int)(stat * breakPointBonus) + (int)orderBoost;
            }

            return score;
        }

        protected void ScoreBeamNode(ref BeamNode beamNode)
        {
            beamNode.Score = 0;
            beamNode.RequirementsMet = 0;
            beamNode.RequirementProgress = 0;

            for (int i = 0; i < StatIndexes.Length; i++)
            {
                var statIdx = StatIndexes[i];
                var totalVal = Math.Min((byte)MAX_STAT_VALUE, beamNode.Totals[statIdx]);
                var lookupIdx = statIdx * (MAX_STAT_VALUE + 1) + totalVal;
                var statScore = StatScoreLookup[lookupIdx];
                beamNode.Score += statScore;
                beamNode.RequirementsMet += RequirementMetLookup[lookupIdx];
                beamNode.RequirementProgress += StatProgressLookup[lookupIdx];
            }
        }

        protected ModComboResult BeamToResult(BeamNode beam)
        {
            var result = new ModComboResult();
            unsafe
            {
                result.ModuleSet.Mod1 = beam.CurrentSet.ModArr[0];
                result.ModuleSet.Mod2 = beam.CurrentSet.ModArr[1];
                result.ModuleSet.Mod3 = beam.CurrentSet.ModArr[2];
                result.ModuleSet.Mod4 = beam.CurrentSet.ModArr[3];
                result.ModuleSet.Mod5 = beam.CurrentSet.ModArr[4];
            }

            result.Score = (int)beam.Score;
            return result;
        }

        protected List<BeamNode> GetTopK(IEnumerable<BeamNode> candidates, int size)
        {
            var heap = new PriorityQueue<BeamNode, double>();

            foreach (var candidate in candidates)
            {
                if (heap.Count < size)
                {
                    heap.Enqueue(candidate, candidate.Score);
                    continue;
                }

                heap.TryPeek(out var worst, out var worstScore);

                if (!IsBetter(candidate, worst))
                    continue;

                heap.Dequeue();
                heap.Enqueue(candidate, CreatePriority(candidate));
            }

            var result = new List<BeamNode>(heap.Count);

            while (heap.Count > 0)
            {
                result.Add(heap.Dequeue());
            }

            result.Sort((a, b) => b.Score.CompareTo(a.Score));

            return result;
        }

        protected static bool IsBetter(BeamNode a, BeamNode b)
        {
            if (a.RequirementProgress != b.RequirementProgress)
                return a.RequirementProgress > b.RequirementProgress;

            if (a.RequirementsMet != b.RequirementsMet)
                return a.RequirementsMet > b.RequirementsMet;

            return a.Score > b.Score;
        }

        protected static long CreatePriority(BeamNode x)
        {
            return ((long)x.RequirementsMet << 48) | ((long)x.RequirementProgress << 32) | (uint)Math.Max(0, (int)(x.Score * 1000));
        }

        protected List<BeamNode> GetDistinctTopResults(IEnumerable<BeamNode> candidates, int maxResults = 10, int minStatDifference = 10)
        {
            var results = new List<BeamNode>();

            foreach (var candidate in candidates.OrderBy(x => x, Comparer<BeamNode>.Create(CompareBeamStates)))
            {
                bool distinct = true;

                foreach (var existing in results)
                {
                    if (StatDifference(existing, candidate, StatIndexes) < minStatDifference)
                    {
                        distinct = false;
                        break;
                    }
                }

                if (!distinct)
                    continue;

                results.Add(candidate);

                if (results.Count == maxResults)
                    break;
            }

            return results;
        }

        protected int StatDifference(BeamNode a, BeamNode b, byte[] statIndexes)
        {
            int diff = 0;

            foreach (var statIdx in statIndexes)
            {
                var statA = Math.Min(a.Totals[statIdx], (byte)MAX_STAT_VALUE);
                var statB = Math.Min(b.Totals[statIdx], (byte)MAX_STAT_VALUE);
                diff += Math.Abs(statA = statB);
            }

            return diff;
        }

        private int CompareBeamStates(BeamNode a, BeamNode b)
        {
            if (a.RequirementsMet != b.RequirementsMet)
                return b.RequirementsMet.CompareTo(a.RequirementsMet);

            if (a.RequirementProgress != b.RequirementProgress)
                return b.RequirementProgress.CompareTo(a.RequirementProgress);

            /*foreach (var stat in NormalizedStatPrios)
            {
                int comparison = b.Totals[stat.Id].CompareTo(a.Totals[stat.Id]);

                if (comparison != 0)
                    return comparison;
            }*/

            return b.Score.CompareTo(a.Score);
        }

        public sealed class TopK
        {
            private int Size;
            private PriorityQueue<BeamNode, double> Heap = new();

            public TopK(int size)
            {
                Size = size;
            }

            public void Add(BeamNode candidate)
            {
                if (Heap.Count < Size)
                {
                    Heap.Enqueue(candidate, candidate.Score);
                    return;
                }

                Heap.TryPeek(out var worst, out var worstScore);

                if (!IsBetter(candidate, worst))
                    return;

                Heap.Dequeue();
                Heap.Enqueue(candidate, CreatePriority(candidate));
            }

            public List<BeamNode> ToList()
            {
                var result = new List<BeamNode>(Heap.Count);

                while (Heap.Count > 0)
                {
                    result.Add(Heap.Dequeue());
                }

                return result;
            }
        }

        public struct BeamNode
        {
            public ModuleSetIndices CurrentSet;
            public byte Depth;
            public int Score;
            public Vector<byte> Totals;
            public byte RequirementsMet;
            public ushort RequirementProgress;

            public ulong GetHash()
            {
                ulong hash = 14695981039346656037UL;

                for (int i = 0; i < Vector<byte>.Count; i++)
                {
                    hash ^= Math.Min(Totals[i], (byte)20);
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }
    }
}
