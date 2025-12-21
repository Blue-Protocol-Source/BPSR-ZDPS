using BPSR_ZDPS.DataTypes.Modules;
using Serilog;
using System.Configuration;
using System.Diagnostics;
using System.Numerics;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS.Managers
{
    public class ModuleOptimizer
    {
        public SolverResult Solve(SolverConfig config, PlayerModDataSave playerMods, SolverModes mode)
        {
            var sw = Stopwatch.StartNew();
            var filtered = FilterModulesWithStats(config, playerMods);

            SolverResult result = new SolverResult(); 
            if (mode == SolverModes.Legacy)
            {
                result = DebugSlow(config, playerMods, sw, filtered);
            }
            else if (mode == SolverModes.Fallback)
            {
                result = NewSlow(config, playerMods, sw, filtered);
            }
            else if (mode == SolverModes.Normal)
            {
                result = NewFast(config, playerMods, sw, filtered);
            }

            Log.Information($"Combos took: {sw.Elapsed}");

            return result;
        }

        private SolverResult DebugSlow(SolverConfig config, PlayerModDataSave playerMods, Stopwatch sw, List<long> filtered)
        {
            // Create short indices
            // Create base scores for modules
            // Score combos based on the stat priorities and breakpoints reached

            // Build module scores
            ushort[] baseModuleScores = new ushort[filtered.Count];
            for (int i = 0; i < filtered.Count; i++)
            {
                var score = CalcModuleScore(config, playerMods, filtered[i]);
                baseModuleScores[i] = score;
            }

            var numMods = filtered.Count;
            var bestMods = new List<ModComboResult>();
            Parallel.For(0, numMods - 3, i =>
            {
                ModComboResult[] topBest = new ModComboResult[10];

                for (int j = i + 1; j < numMods - 2; j++)
                {
                    for (int k = j + 1; k < numMods - 1; k++)
                    {
                        for (int l = k + 1; l < numMods; l++)
                        {
                            var combo = new ModuleSet()
                            {
                                Mod1 = (ushort)i,
                                Mod2 = (ushort)j,
                                Mod3 = (ushort)k,
                                Mod4 = (ushort)l
                            };

                            var mod1Cores = GetModPowerCores(playerMods, filtered[i]);
                            var mod2Cores = GetModPowerCores(playerMods, filtered[j]);
                            var mod3Cores = GetModPowerCores(playerMods, filtered[k]);
                            var mod4Cores = GetModPowerCores(playerMods, filtered[l]);

                            var allCores = new List<PowerCore>(10);
                            allCores.AddRange(mod1Cores);
                            allCores.AddRange(mod2Cores);
                            allCores.AddRange(mod3Cores);
                            allCores.AddRange(mod4Cores);

                            int totalScore = 0;

                            var grouped = allCores.AsValueEnumerable().GroupBy(x => x.Id);
                            foreach (var group in grouped)
                            {
                                var statMul = GetStatMultiplier(config, group.Key);
                                var total = group.AsValueEnumerable().Select(y => y.Value).Sum();
                                var breakPointBonus = total switch
                                {
                                    >= 20 => 64,
                                    >= 16 => 32,
                                    >= 12 => 16,
                                    >= 8 => 8,
                                    >= 4 => 4,
                                    >= 1 => 2,
                                    _ => 0
                                };

                                var score = (Math.Clamp(total, 0, 20) + breakPointBonus) * statMul;
                                totalScore += score;
                            }
                            
                            for (int bestIdx = 0; bestIdx < topBest.Length; bestIdx++)
                            {
                                var best = topBest[bestIdx];
                                if (totalScore > best.Score)
                                {
                                    topBest[bestIdx].ModuleSet = combo;
                                    topBest[bestIdx].Score = totalScore;
                                }
                            }
                        }
                    }
                }

                lock (bestMods)
                {
                    bestMods.AddRange(topBest);
                }
            });
            sw.Stop();

            var top10 = bestMods.DistinctBy(x => $"{x.ModuleSet.Mod1}_{x.ModuleSet.Mod2}_{x.ModuleSet.Mod3}_{x.ModuleSet.Mod4}").OrderByDescending(x => x.Score).Take(10).ToList();
            for (int i1 = 0; i1 < top10.Count; i1++)
            {
                ModComboResult modSet = top10[i1];
                var coreStats = new Dictionary<long, PowerCore>();
                var mods = modSet.ModuleSet.Mods;
                for (int i = 0; i < mods.Length; i++)
                {
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

                modSet.Stats = coreStats.Values.OrderByDescending(x => x.Value).ToArray();

                var reslovedModSet = new ModuleSet()
                {
                    Mod1 = (ushort)filtered[mods[0]],
                    Mod2 = (ushort)filtered[mods[1]],
                    Mod3 = (ushort)filtered[mods[2]],
                    Mod4 = (ushort)filtered[mods[3]]
                };
                modSet.CombatScore = CalcCombosCombatScore(playerMods, reslovedModSet);

                top10[i1] = modSet;
            }


            var result = new SolverResult()
            {
                BestModResults = top10,
                FilteredModules = filtered
            };

            return result;
        }

        private SolverResult NewSlow(SolverConfig config, PlayerModDataSave playerMods, Stopwatch sw, List<long> filtered)
        {
            var numMods = filtered.Count;
            var bestMods = new List<ModComboResult>();
            Parallel.For(0, numMods - 3, i =>
            {
                ModComboResult[] topBest = new ModComboResult[10];
                var statLimits = config.StatPriorities.ToDictionary(x => x.Id, y => y.MinLevel);

                for (int j = i + 1; j < numMods - 2; j++)
                {
                    for (int k = j + 1; k < numMods - 1; k++)
                    {
                        for (int l = k + 1; l < numMods; l++)
                        {
                            var combo = new ModuleSet()
                            {
                                Mod1 = (ushort)i,
                                Mod2 = (ushort)j,
                                Mod3 = (ushort)k,
                                Mod4 = (ushort)l
                            };

                            var mod1Cores = GetModPowerCores(playerMods, filtered[i]);
                            var mod2Cores = GetModPowerCores(playerMods, filtered[j]);
                            var mod3Cores = GetModPowerCores(playerMods, filtered[k]);
                            var mod4Cores = GetModPowerCores(playerMods, filtered[l]);

                            var allCores = new List<PowerCore>(10);
                            allCores.AddRange(mod1Cores);
                            allCores.AddRange(mod2Cores);
                            allCores.AddRange(mod3Cores);
                            allCores.AddRange(mod4Cores);

                            int totalScore = 0;

                            var grouped = allCores.AsValueEnumerable().GroupBy(x => x.Id);
                            foreach (var group in grouped)
                            {
                                var statMul = GetStatMultiplier(config, group.Key);
                                var total = group.AsValueEnumerable().Select(y => y.Value).Sum();

                                var bonusIdx = total switch
                                {
                                    >= 20 => 5,
                                    >= 16 => 4,
                                    >= 12 => 3,
                                    >= 8 => 2,
                                    >= 4 => 1,
                                    >= 1 => 0,
                                    _ => 0
                                };

                                var breakPointBonus = (byte)config.LinkLevelBonus[bonusIdx];

                                var score = (Math.Clamp(total, 0, 20) + breakPointBonus) * statMul;
                                var hasStatLimit = statLimits.TryGetValue(group.Key, out var statLimit);
                                if (!hasStatLimit || total >= statLimit)
                                {
                                    totalScore += score;
                                }
                            }

                            for (int bestIdx = 0; bestIdx < topBest.Length; bestIdx++)
                            {
                                var best = topBest[bestIdx];
                                if (totalScore > best.Score)
                                {
                                    topBest[bestIdx].ModuleSet = combo;
                                    topBest[bestIdx].Score = totalScore;
                                }
                            }
                        }
                    }
                }

                lock (bestMods)
                {
                    bestMods.AddRange(topBest);
                }
            });
            sw.Stop();

            var top10 = bestMods.DistinctBy(x => $"{x.ModuleSet.Mod1}_{x.ModuleSet.Mod2}_{x.ModuleSet.Mod3}_{x.ModuleSet.Mod4}").OrderByDescending(x => x.Score).Take(10).ToList();
            for (int i1 = 0; i1 < top10.Count; i1++)
            {
                ModComboResult modSet = top10[i1];
                var coreStats = new Dictionary<long, PowerCore>();
                var mods = modSet.ModuleSet.Mods;
                for (int i = 0; i < mods.Length; i++)
                {
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

                modSet.Stats = coreStats.Values.OrderByDescending(x => x.Value).ToArray();

                var reslovedModSet = new ModuleSet()
                {
                    Mod1 = (ushort)filtered[mods[0]],
                    Mod2 = (ushort)filtered[mods[1]],
                    Mod3 = (ushort)filtered[mods[2]],
                    Mod4 = (ushort)filtered[mods[3]]
                };
                modSet.CombatScore = CalcCombosCombatScore(playerMods, reslovedModSet);

                top10[i1] = modSet;
            }


            var result = new SolverResult()
            {
                BestModResults = top10,
                FilteredModules = filtered
            };

            return result;
        }

        private SolverResult NewFast(SolverConfig config, PlayerModDataSave playerMods, Stopwatch sw, List<long> filtered)
        {
            const int MAX_LEVEL = 20;
            int statIdx = 0;
            var possableStats = filtered.AsValueEnumerable().SelectMany(x =>
                playerMods.ModulesPackage.Items[x].ModNewAttr.ModParts)
                    .Distinct().Order().ToDictionary(x => x, y => statIdx++);

            var vecCount = Vector<byte>.Count;
            List<Vector<byte>> modStatValues = new List<Vector<byte>>();
            foreach (var modId in filtered)
            {
                var modStatIds = playerMods.ModulesPackage.Items[modId].ModNewAttr.ModParts;

                int linkIdx = 0;
                var mod1Ids = new byte[vecCount];
                foreach (var statId in modStatIds)
                {
                    var idx = possableStats[statId];
                    mod1Ids[idx] = (byte)playerMods.Mod.ModInfos[modId].InitLinkNums[linkIdx++];
                }

                var vec = new Vector<byte>(mod1Ids);
                modStatValues.Add(vec);
            }

            var modStatMultplierArr = new byte[vecCount];
            foreach (var statKvp in possableStats)
            {
                modStatMultplierArr[statKvp.Value] = (byte)GetStatMultiplier(config, statKvp.Key);
            }
            Vector<byte> modStatMultplier = new Vector<byte>(modStatMultplierArr);

            Vector<byte> statCap = new Vector<byte>(MAX_LEVEL);

            var statMins = new byte[vecCount];
            var statReqs = new byte[vecCount];
            var statMask = new byte[vecCount];
            foreach (var statPrio in config.StatPriorities)
            {
                if (possableStats.TryGetValue(statPrio.Id, out var idx))
                {
                    statMins[idx] = (byte)statPrio.MinLevel;
                    statReqs[idx] = (byte)statPrio.ReqLevel;
                    statMask[idx] = 1;
                }
            }
            Vector<byte> statMinsVec = new Vector<byte>(statMins);
            Vector<byte> statReqsVec = new Vector<byte>(statReqs);
            Vector<byte> statMaskVec = new Vector<byte>(statMask);

            var breakPointBoosts = new byte[vecCount];
            for (int i = 0; i < vecCount; i++)
            {
                var bonusIdx = i switch
                {
                    >= 20 => 5,
                    >= 16 => 4,
                    >= 12 => 3,
                    >= 8 => 2,
                    >= 4 => 1,
                    >= 1 => 0,
                    _ => 0
                };

                breakPointBoosts[i] = (byte)config.LinkLevelBonus[bonusIdx];
            }

            var numMods = filtered.Count;
            var bestMods = new List<ModComboResult>();
            Parallel.For(0, numMods - 3, i =>
            {
                ModComboResult[] topBest = new ModComboResult[10];

                for (int j = i + 1; j < numMods - 2; j++)
                {
                    for (int k = j + 1; k < numMods - 1; k++)
                    {
                        for (int l = k + 1; l < numMods; l++)
                        {
                            var combo = new ModuleSet()
                            {
                                Mod1 = (ushort)i,
                                Mod2 = (ushort)j,
                                Mod3 = (ushort)k,
                                Mod4 = (ushort)l
                            };

                            var sums = (modStatValues[(int)i] +
                                modStatValues[j] +
                                modStatValues[k] +
                                modStatValues[l]);

                            var sumsMined = Vector.Min(sums, statCap);

                            var statIsGreaterThanReq = Vector.LessThan<byte>(sumsMined, statReqsVec);
                            var statIsGreaterThanReqSumed = Vector.Sum(Vector.AsVectorUInt16(statIsGreaterThanReq));
                            if (statIsGreaterThanReqSumed > 0)
                            {
                                continue;
                            }

                            var statIsGreaterThan = Vector.GreaterThan<byte>(sumsMined, statMinsVec);
                            var passedMinValues = Vector.ConditionalSelect(statIsGreaterThan, sumsMined, new Vector<byte>(0));
                            //var multied = passedMinValues * modStatMultplier;

                            int breakPointBonus = 0;
                            var breakPointValues = passedMinValues;
                            for (int idx = 0; idx < Vector<byte>.Count; idx++)
                            {
                                var val = (breakPointBoosts[breakPointValues[idx]] * modStatMultplier[idx]);
                                breakPointBonus += val;
                            }

                            int score = Vector.Sum(sums) + breakPointBonus;

                            for (int bestIdx = 0; bestIdx < topBest.Length; bestIdx++)
                            {
                                var best = topBest[bestIdx];
                                if (score > best.Score)
                                {
                                    topBest[bestIdx].ModuleSet = combo;
                                    topBest[bestIdx].Score = score;
                                    break;
                                }
                            }

                            /*topBest[0] = new ModComboResult()
                            {
                                ModuleSet = combo,
                                Score = score,
                            };*/
                        }
                    }
                }

                lock (bestMods)
                {
                    bestMods.AddRange(topBest);
                }
            });
            sw.Stop();

            var top10 = bestMods.AsValueEnumerable().Where(x => x.Score > 0).DistinctBy(x => $"{x.ModuleSet.Mod1}_{x.ModuleSet.Mod2}_{x.ModuleSet.Mod3}_{x.ModuleSet.Mod4}").OrderByDescending(x => x.Score).Take(10).ToList();
            for (int i1 = 0; i1 < top10.Count; i1++)
            {
                ModComboResult modSet = top10[i1];
                var coreStats = new Dictionary<long, PowerCore>();
                var mods = modSet.ModuleSet.Mods;
                for (int i = 0; i < mods.Length; i++)
                {
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

                modSet.Stats = coreStats.Values.OrderByDescending(x => x.Value).ToArray();

                var reslovedModSet = new ModuleSet()
                {
                    Mod1 = (ushort)filtered[mods[0]],
                    Mod2 = (ushort)filtered[mods[1]],
                    Mod3 = (ushort)filtered[mods[2]],
                    Mod4 = (ushort)filtered[mods[3]]
                };
                modSet.CombatScore = CalcCombosCombatScore(playerMods, reslovedModSet);

                top10[i1] = modSet;
            }

            var result = new SolverResult()
            {
                BestModResults = top10,
                FilteredModules = filtered
            };

            return result;
        }

        private List<long> FilterModulesWithStats(SolverConfig config, PlayerModDataSave playerMods)
        {
            var modules = new List<long>();
            foreach (var item in playerMods.ModulesPackage.Items)
            {
                var qualityValue = Math.Clamp(item.Value.Quality, 0, 4);
                if (config.QualitiesV2.TryGetValue(qualityValue, out var quality) ? quality : false)
                {
                    if (item.Value.ModNewAttr.ModParts.Any(x => config.StatPriorities.Any(y => y.Id == x)))
                    {
                        modules.Add(item.Key);
                    }
                }
            }

            return modules.ToList();
        }

        private ushort GetStatMultiplier(SolverConfig config, int statId)
        {
            for (int i = 0; i < config.StatPriorities.Count; i++)
            {
                if (config.StatPriorities[i].Id == statId)
                {
                    return (ushort)((config.StatPriorities.Count - i) + 1);
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
    }
}
