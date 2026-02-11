using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Data.Common;

namespace BPSR_ZDPS.Database.Migrations
{
    public class SkillStatsMigration : BaseMigration
    {
        public SkillStatsMigration()
        {
            Name = "Skill Stats Migration";
            Description = "Update from using SkillStats to SkillMetrics";
            MinVersion = 1.1f;
            NewVersion = 1.2f;
        }

        public override bool RunMigration(DbConnection dbConn, SqliteTransaction tx)
        {
            try
            {
                var encounters = DB.LoadEncounterSummaries();
                int processedEncounters = 0;
                foreach (var encounter in encounters)
                {
                    processedEncounters++;
                    if (!encounter.HasStatsBeenRecorded(true))
                    {
                        continue;
                    }

                    try
                    {
                        var fullEncounter = DB.LoadEncounter(encounter.EncounterId);
                        foreach (var entity in fullEncounter.Entities.Values)
                        {
                            foreach (var skillStat in entity.SkillStats)
                            {
                                foreach (var snapshot in skillStat.Value.SkillSnapshots)
                                {
                                    if (!entity.SkillMetrics.TryGetValue(skillStat.Key, out var metrics))
                                    {
                                        metrics = new MetricsContainer();
                                    }

                                    CombatStats? combatStats = null;
                                    var skillType = skillStat.Value.SkillType;

                                    if (skillStat.Value.SkillType == ESkillType.Taken)
                                    {
                                        combatStats = metrics.Taken;
                                        skillType = ESkillType.Taken;
                                    }
                                    else if (snapshot.DamageType == Zproto.EDamageType.Normal)
                                    {
                                        combatStats = metrics.Damage;
                                        skillType = ESkillType.Damage;
                                    }
                                    else if (snapshot.DamageType == Zproto.EDamageType.Heal)
                                    {
                                        combatStats = metrics.Healing;
                                        skillType = ESkillType.Healing;
                                    }
                                    else if (skillStat.Value.SkillType == ESkillType.Damage)
                                    {
                                        combatStats = metrics.Damage;
                                        skillType = ESkillType.Damage;
                                    }
                                    else if (skillStat.Value.SkillType == ESkillType.Healing)
                                    {
                                        combatStats = metrics.Healing;
                                        skillType = ESkillType.Healing;
                                    }
                                    else
                                    {
                                        Log.Error($"SkillId: {skillStat.Key}, skillStat.Value.SkillType: {skillStat.Value.SkillType}, snapshot.DamageType: {snapshot.DamageType}");
                                        continue;
                                    }

                                    combatStats.SetName(skillStat.Value.Name);
                                    combatStats.SetSummonData(skillStat.Value.SummonUUID, skillStat.Value.TierLevel);
                                    combatStats.SetSkillType(skillType);
                                    combatStats.AddData(0,
                                        skillStat.Key,
                                        skillStat.Value.Level,
                                        snapshot.Value,
                                        snapshot.IsCrit,
                                        snapshot.IsLucky,
                                        snapshot.Value,
                                        0,
                                        snapshot.IsCauseLucky,
                                        snapshot.DamageElement,
                                        snapshot.DamageType,
                                        snapshot.DamageMode,
                                        snapshot.IsKill,
                                        new Zproto.Vec3(),
                                        null,
                                        null,
                                        new BPSR_ZDPSLib.ExtraPacketData(snapshot.Timestamp.Value),
                                        0.0,
                                        snapshot.Timestamp.Value);

                                    entity.SkillMetrics[skillStat.Key] = metrics;
                                }
                            }

                            entity.SkillStats.Clear();
                        }

                        var entityBlob = DB.CreateEntityBlobForEncounter(fullEncounter);
                        var result = dbConn.Execute("UPDATE Entities SET Data = @Data WHERE EncounterId = @EncounterId", entityBlob, tx);
                        if (result != 1)
                        {
                            Log.Error($"Error updating Entities table for Encounter {encounter.EncounterId}. Result was {result} but expected 1.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error migrating Encounter {encounter.EncounterId}. Encounter will be dropped from the new ZDatabase.");

                        dbConn.Execute(DBSchema.Encounter.RemoveEncounter, new { EncounterId = encounter.EncounterId }, tx);
                        dbConn.Execute(DBSchema.Entities.RemoveByEncounterId, new { EncounterId = encounter.EncounterId }, tx);
                    }

                    Progress = (float)processedEncounters / encounters.Count;
                }

                Progress = 1f;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error migrating DB for {Name}", Name);
                ErrorMsg = "Failed to migrate DB, please try again.\nIf it fails again please delete ZDatabase.db in Data.";
                return false;
            }
        }
    }
}
