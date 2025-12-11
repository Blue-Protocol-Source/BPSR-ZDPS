using BPSR_DeepsLib;
using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS
{
    public static class EncounterManager
    {
        public static int SelectedEncounter = -1;

        public static Encounter? Current = null;

        public static int CurrentEncounter = 0;
        public static int CurrentBattleId = 0;
        public static uint LevelMapId { get; private set; }
        public static string SceneName { get; private set; }
        public delegate void BattleStartEventHandler(EventArgs e);
        public static event BattleStartEventHandler BattleStart;
        public delegate void EncounterStartEventHandler(EventArgs e);
        public static event EncounterStartEventHandler EncounterStart;
        public delegate void EncounterEndEventHandler(EventArgs e);
        public static event EncounterEndEventHandler EncounterEnd;
        public delegate void EncounterEndFinalEventHandler(EncounterEndFinalData e);
        public static event EncounterEndFinalEventHandler EncounterEndFinal;

        public static CancellationTokenSource UpdateTruePerValuesCTS = new CancellationTokenSource();

        static EncounterManager()
        {
            // Give a default encounter for now
            StartNewBattle();
            StartEncounter();
            IntegrationManager.InitBindings();
        }

        public static void StartEncounter(bool force = false, EncounterStartReason reason = EncounterStartReason.None)
        {
            string priorBossName = "";
            int priorEncounterPhase = 0;

            if (Current != null)
            {
                bool hasStatsBeenRecorded = Current.HasStatsBeenRecorded();
                if (force || (Current.EndTime == DateTime.MinValue && hasStatsBeenRecorded))
                {
                    // We called StartEncounter without first stopping the current one
                    StopEncounter(true);
                }
                else if (Current.EndTime == DateTime.MinValue && !hasStatsBeenRecorded)
                {
                    // Nothing has actually happened in this encounter, so let's just reset the time and reuse it
                    Current.SetStartTime(DateTime.Now);
                    Current.SetEndTime(DateTime.MinValue);
                    if (LevelMapId > 0)
                    {
                        SetSceneId(LevelMapId);
                    }
                    return;
                }

                if (reason == EncounterStartReason.Wipe)
                {
                    // Wipes occur after the boss has reset so we need to now restore the last HP state before that
                    if (Current.Entities.TryGetValue(Current.BossUUID, out var bossEntity))
                    {
                        long lowestHp = bossEntity.MaxHp;

                        int stackSize = bossEntity.RecentHpHistory.Count > 4 ? 4 : bossEntity.RecentHpHistory.Count;
                        for (int i = 0; i < stackSize; i++)
                        {
                            long historicalHp = bossEntity.RecentHpHistory.ElementAt(i);
                            if (historicalHp < lowestHp)
                            {
                                lowestHp = historicalHp;
                            }
                        }

                        bossEntity.SetHpNoUpdate(lowestHp);
                    }
                }

                if ((reason == EncounterStartReason.NewObjective) && hasStatsBeenRecorded)
                {
                    // We're likely entering a new phase (either raid boss phase or dungeon phase going into boss)
                    priorBossName = Current.BossName;

                    if (!string.IsNullOrEmpty(Current.SceneSubName))
                    {
                        // Break the current sub name into parts to try and figure out what our current phase number is to increment for upcoming encounter
                        var subNameParts = Current.SceneSubName.Split(' ', StringSplitOptions.TrimEntries);
                        if (subNameParts.Length > 1)
                        {
                            if (int.TryParse(subNameParts.Last(), out var phaseNumber))
                            {
                                // For now we trust this is a string ending with out Phase number
                                priorEncounterPhase = phaseNumber;
                            }
                        }
                    }
                    else
                    {
                        // This is our first split
                        Current.SceneSubName = "Phase 1";
                        priorEncounterPhase = 1;
                    }
                }

                // This is safe to call to ensure we're sending a proper End Final before a new Encounter is made no matter what
                BattleStateMachine.SetDeferredEncounterEndFinalData(DateTime.Now.Subtract(new TimeSpan(0, 0, 1)), new EncounterEndFinalData() { EncounterId = Current.EncounterId, BattleId = Current.BattleId, Reason = reason });
                BattleStateMachine.CheckDeferredCalls();
            }
            //Encounters.Add(new Encounter(CurrentBattleId));

            //CurrentEncounter = Encounters.Count - 1;

            if (Current != null)
            {
                DB.InsertEncounter(Current);
            }

            Current = new Encounter(CurrentBattleId);
            Current.EncounterId = DB.GetNextEncounterId();
            if ((reason == EncounterStartReason.NewObjective))
            {
                if (!string.IsNullOrEmpty(priorBossName))
                {
                    Current.BossName = priorBossName;
                }
                if (priorEncounterPhase > 0)
                {
                    Current.SceneSubName = $"Phase {priorEncounterPhase + 1}";
                }
            }

            // Reuse last sceneId as our current one (it may not always be right but hopefully is right enough)
            if (LevelMapId > 0)
            {
                SetSceneId(LevelMapId);
            }

            UpdateTruePerValuesCTS = new();
            if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
            {
                // Only allow calculating the True Per Second if it was enabled since there is a performance cost to doing so
                Task.Run(() =>
                {
                    UpdateTruePerValues(UpdateTruePerValuesCTS);
                });
            }

            OnEncounterStart(new EventArgs());
        }

        public static void StopEncounter(bool isKnownFinal = false)
        {
            if (Current != null && Current.EndTime == DateTime.MinValue)
            {
                Current.SetEndTime(DateTime.Now);
            }

            UpdateTruePerValuesCTS.Cancel();

            OnEncounterEnd(new EventArgs());

            if (isKnownFinal)
            {
                // We don't actually want to end instantly because some packets are going to be delayed and come in _after_ this and they are typically the most important ones to not miss
                BattleStateMachine.SetDeferredEncounterEndFinalData(DateTime.Now.AddSeconds(2), new EncounterEndFinalData() { EncounterId = Current.EncounterId, BattleId = Current.BattleId });
            }
            else
            {
                BattleStateMachine.SetDeferredEncounterEndFinalData(DateTime.Now.AddSeconds(5), new EncounterEndFinalData() { EncounterId = Current.EncounterId, BattleId = Current.BattleId });
            }

            EntityCache.Instance.Save();
        }

        public static void SignalEncounterEndFinal(EncounterEndFinalData data)
        {
            OnEncounterEndFinal(data);
            if (Current != null)
            {
                Current.RemoveEventHandlers();
            }
        }

        public static void UpdateEncounterState()
        {
            // Check if it has been too long since the last time the encounter was updated, meaning we are likely in a new encounter
            // Or if we've ended combat already, then we need to get put back into it now by starting up a new encounter

            double combatTimeout = 15.0;
            if (Current != null && DateTime.Now.Subtract(Current.LastUpdate).TotalSeconds > combatTimeout)
            {
                // It has been too long since the last encounter update, we're probably in a new encounter now
                StartEncounter();
            }    
        }

        public static void StartNewBattle()
        {
            // This increments an internal ID for encounters to use that allows them to be grouped together by "battle"
            // These are typically going to be just splitting encounters up by instance (which is changed via map traveling)

            if (CurrentBattleId != 0)
            {
                DB.UpdateBattleEnd(CurrentBattleId);
            }

            var battleId = DB.StartBattle(LevelMapId, SceneName);
            CurrentBattleId = battleId;

            OnBattleStart(new EventArgs());
        }

        // While we technically use the 'LevelMapId' and not the 'SceneId' field, it's just another type of SceneId ultimately
        public static void SetSceneId(uint levelMapId)
        {
            LevelMapId = levelMapId;
            if (levelMapId > 0)
            {
                if (HelperMethods.DataTables.Scenes.Data.TryGetValue(levelMapId.ToString(), out var scene))
                {
                    SceneName = scene.Name;
                }
                else
                {
                    SceneName = "";
                }
            }
            else
            {
                SceneName = "";
            }

            Current.SceneId = LevelMapId;
            Current.SceneName = SceneName;
            DB.UpdateBattleInfo(CurrentBattleId, LevelMapId, SceneName);
        }

        public static async void UpdateTruePerValues(CancellationTokenSource cancellationTokenSource)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (!cancellationTokenSource.IsCancellationRequested && await timer.WaitForNextTickAsync())
            {
                var duration_rep = 1 / Current.GetDuration().TotalSeconds;
                if (Current.TotalDamage > 0)
                {
                    var entities = Current.Entities.AsValueEnumerable();
                    foreach (var entity in entities)
                    {
                        var trueDamagePerSecond = entity.Value.TotalDamage * duration_rep;
                        var trueHealingPerSecond = entity.Value.TotalHealing * duration_rep;
                        var trueTakenPerSecond = entity.Value.TotalTakenDamage * duration_rep;

                        entity.Value.DamageStats.TrueValuePerSecond = trueDamagePerSecond;
                        entity.Value.HealingStats.TrueValuePerSecond = trueHealingPerSecond;
                        entity.Value.TakenStats.TrueValuePerSecond = trueTakenPerSecond;
                    }
                }
            }
        }

        static void OnBattleStart(EventArgs e)
        {
            BattleStart?.Invoke(e);
        }

        static void OnEncounterStart(EventArgs e)
        {
            EncounterStart?.Invoke(e);
        }

        static void OnEncounterEnd(EventArgs e)
        {
            EncounterEnd?.Invoke(e);
        }

        static void OnEncounterEndFinal(EncounterEndFinalData e)
        {
            EncounterEndFinal?.Invoke(e);
        }
    }

    public enum EncounterStartReason : int
    {
        None = 0, // No reason given (generic start)
        NewObjective = 1, // New Objective potentially a new Phase
        Wipe = 2, // Current Encounter was a wipe
        Force = 3, // We don't know the reason but we know it needs to force a new one (possibly a map transition)
        Restart = 4,
        TimedOut = 5,
        BenchmarkStart = 6,
        BenchmarkEnd = 7,
    }

    public class Encounter
    {
        public ulong EncounterId { get; set; }
        public int BattleId { get; set; }
        public uint SceneId { get; set; }
        public string SceneName { get; set; }
        public string SceneSubName { get; set; }
        public long BossUUID { get; set; }
        public long BossAttrId { get; set; }
        public string BossName { get; set; }
        public int BossHpPct { get; set; }
        public string Note { get; set; }

        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        private TimeSpan? Duration { get; set; }
        public DateTime LastUpdate { get; set; }
        public ConcurrentDictionary<long, Entity> Entities { get; set; } = [];

        public ulong TotalDamage { get; set; } = 0;
        public ulong TotalNpcDamage { get; set; } = 0;
        public ulong TotalShieldBreak { get; set; } = 0;
        public ulong TotalNpcShieldBreak { get; set; } = 0;
        public ulong TotalHealing { get; set; } = 0;
        public ulong TotalNpcHealing { get; set; } = 0;
        public ulong TotalOverhealing { get; set; } = 0;
        public ulong TotalNpcOverhealing { get; set; } = 0;
        public ulong TotalTakenDamage { get; set; } = 0;
        public ulong TotalNpcTakenDamage { get; set; } = 0;
        public ulong TotalDeaths { get; set; } = 0;
        public ulong TotalNpcDeaths { get; set; } = 0;
        public bool IsWipe { get; set; } = false;

        public delegate void SkillActivatedEventHandler(object sender, SkillActivatedEventArgs e);
        public event SkillActivatedEventHandler SkillActivated;
        public delegate void HpUpdatedEventHandler(object sender, HpUpdatedEventArgs e);
        public event HpUpdatedEventHandler BossHpUpdated; // Only used for Bosses
        public event HpUpdatedEventHandler EntityHpUpdated; // Used for all Entities
        public delegate void ThreatListUpdatedEventHandler(object sender, ThreatListUpdatedEventArgs e);
        public event ThreatListUpdatedEventHandler EntityThreatListUpdated; // This is not a real list, just the current target and their threat value

        public EncounterExData ExData { get; set; } = new();
        public byte[] ExDataBlob {  get; set; }

        // Fields here are not stored in the Database
        public List<long> BossUUIDs { get; set; } = new();
        public EDungeonState DungeonState { get; set; } = EDungeonState.DungeonStateNull;
        public uint ChannelLine { get; set; } = 0;

        public Encounter()
        {

        }

        public Encounter(int battleId = 0)
        {
            SetStartTime(DateTime.Now);
            Entities = new();
            BattleId = battleId;
        }

        public Encounter(DateTime startTime, int battleId = 0)
        {
            SetStartTime(startTime);
            Entities = new();
            BattleId = battleId;
        }

        public void SetStartTime(DateTime start)
        {
            StartTime = start;
        }

        public void SetEndTime(DateTime end)
        {
            EndTime = end;

            Duration = EndTime.Subtract(StartTime);
        }

        public TimeSpan GetDuration()
        {
            if (EndTime == DateTime.MinValue || Duration == null)
            {
                return DateTime.Now.Subtract(StartTime).Duration();
            }
            else
            {
                return (TimeSpan)Duration;
            }
        }

        public Entity GetOrCreateEntity(long uuid)
        {
            if (Entities.TryGetValue(uuid, out var entity))
            {
                return entity;
            }
            else
            {
                entity = new Entity(uuid);
                Entities.TryAdd(uuid, entity);
                return entity;
            }
        }

        public void SetName(long uuid, string name)
        {
            GetOrCreateEntity(uuid).SetName(name);
        }

        public void SetAbilityScore(long uuid, int power)
        {
            GetOrCreateEntity(uuid).SetAbilityScore(power);
        }

        public void SetProfessionId(long uuid,  int professionId)
        {
            GetOrCreateEntity(uuid).SetProfessionId(professionId);
        }

        public void SetEntityType(long uuid, EEntityType etype)
        {
            var entity = GetOrCreateEntity(uuid);
            entity.SetEntityType(etype);

            var attr_id = entity.GetAttrKV("AttrId");
            if (attr_id != null && etype == EEntityType.EntMonster)
            {
                // Only players tend to come with a valid UID that's already unique to them
                // The field that claims to normally be the UID for non-players is actually their non-unique ID
                // Only the Attribute named Id (AttrId) is their real type UID which can be resolved into a name
                // Also can be used to get all of their setup information from the Monsters table
                entity.UID = (int)attr_id;
                if (HelperMethods.DataTables.Monsters.Data.TryGetValue(attr_id.ToString(), out var monsterEntry))
                {
                    entity.SetName(monsterEntry.Name);
                    entity.SetMonsterType(monsterEntry.MonsterType);
                    UpdateEncounterBossData(entity, (int)attr_id);
                }
            }
        }

        public void SetAttrKV(long uuid, string key, object value)
        {
            var entity = GetOrCreateEntity(uuid);
            entity.SetAttrKV(key, value);

            // We used to care if the entity already had a name, but there were strange incorrect name issues, so now we don't
            if (key == "AttrId" && entity.EntityType == EEntityType.EntMonster)
            {
                entity.UID = (int)value;
                if (HelperMethods.DataTables.Monsters.Data.TryGetValue(value.ToString(), out var monsterEntry))
                {
                    entity.SetName(monsterEntry.Name);
                    entity.SetMonsterType(monsterEntry.MonsterType);
                    UpdateEncounterBossData(entity, (int)value);
                }
            }
            else if (key == "AttrLevel")
            {
                entity.SetLevel((int)value);
            }
            else if (key == "AttrSkillId")
            {
                OnSkillActivated(new SkillActivatedEventArgs { CasterUuid = uuid, SkillId = (int)value, ActivationDateTime = DateTime.Now });
                entity.RegisterSkillActivation((int)value);
            }
            else if (key == "AttrState")
            {
                if ((EActorState)value == EActorState.ActorStateDead)
                {
                    // The server does not send a final HP value update when the State also changes, so we'll fake one
                    // We do this before fully processing the State change
                    SetAttrKV(uuid, "AttrHp", 0L);

                    entity.IncrementDeaths();
                    if (entity.EntityType == EEntityType.EntChar)
                    {
                        IncrementDeaths();
                    }
                    else if (entity.EntityType == EEntityType.EntMonster)
                    {
                        IncrementNpcDeaths();
                    }
                }
            }
            else if (key == "AttrShieldList")
            {
                var shieldInfo = (ShieldInfo)value;
                //System.Diagnostics.Debug.WriteLine($"SetAttrKV({uuid})::AttrShieldList.ShieldInfo = {shieldInfo}");
                entity.AddBuffEventAttribute((int)shieldInfo.Uuid, "AttrShieldList", shieldInfo);
                //AddShieldGained(uuid, shieldInfo.Uuid, shieldInfo.Value, shieldInfo.InitialValue, shieldInfo.MaxValue);
            }
            else if (key == "AttrHp")
            {
                if (!entity.IsHpUpdatedHandlerSubscribed(OnEntityHpUpdated))
                {
                    entity.HpUpdated += OnEntityHpUpdated;
                }
                entity.SetHpValues((long)value, -1);
            }
            else if (key == "AttrMaxHp")
            {
                entity.SetHpValues(-1, (long)value);
            }
            else if (key == "AttrSkillRemodelLevel")
            {
                // This has a Tier Level
                if (entity.EntityType == EEntityType.EntChar || entity.EntityType == EEntityType.EntMonster)
                {
                    UpdateCasterSkillTierLevel(uuid, entity, (int)value);
                }
                else
                {
                    // Find the entity for the AttrSummonerId/AttrTopSummonerId and update their Skill matching this entity's AttrSkillId
                    UpdateCasterSkillTierLevel(0, entity, (int)value);
                }
            }
            else if (key == "AttrPos")
            {
                entity.SetPosition(((Zproto.Vec3)value).ToVector3());
            }
            else if (key == "AttrHateList")
            {
                if(!entity.IsThreatListUpdatedHandlerSubscribed(OnEntityThreatListUpdated))
                {
                    entity.ThreatListUpdated += OnEntityThreatListUpdated;
                }

                // This is called a "List" but it is not a list at all. It's a single item of just a UUID and Threat Value for the current target
                var hateInfo = (HateInfo)value;
                ThreatInfo threatInfo = new() { EntityUuid = hateInfo.Uuid, ThreatValue = hateInfo.HateVal };
                entity.SetThreatList(threatInfo);
            }
        }

        public void UpdateCasterSkillTierLevel(long casterUuid, Entity summoned, int skillTierLevel = -1)
        {
            long caster = casterUuid;

            if (caster == 0)
            {
                var summonerId = summoned.GetAttrKV("AttrSummonerId");
                if (summonerId == null)
                {
                    summonerId = summoned.GetAttrKV("AttrTopSummonerId");
                }
                if (summonerId != null)
                {
                    caster = (long)summonerId;
                }
            }

            if (caster != 0)
            {
                int level = skillTierLevel;
                if (level == -1)
                {
                    var attrSkillRemodelLevel = summoned.GetAttrKV("AttrSkillRemodelLevel");
                    if (attrSkillRemodelLevel != null)
                    {
                        level = (int)attrSkillRemodelLevel;
                    }
                }

                var skillId = summoned.GetAttrKV("AttrSkillId");
                if (skillId != null)
                {
                    if (GetOrCreateEntity((long)caster).SkillStats.TryGetValue((int)skillId, out var skill))
                    {
                        skill.SetSummonData(summoned.UUID, (int)level);
                    }
                }
            }
        }

        public void UpdateEncounterBossData(Entity entity, int attr_id)
        {
            if (entity.MonsterType == EMonsterType.Boss)
            {
                if (!BossUUIDs.Contains(entity.UUID))
                {
                    BossUUIDs.Add(entity.UUID);
                    entity.HpUpdated += OnBossHpUpdated;
                }

                if (BossUUID == 0)
                {
                    // This is the first boss we've seen
                    BossUUID = entity.UUID;
                    BossName = entity.Name;
                    BossAttrId = (long)attr_id;
                }
            }
        }

        public void SetChannelLineNumber(uint line)
        {
            ChannelLine = line;
        }

        public void IncrementDeaths()
        {
            TotalDeaths++;
        }

        public void IncrementNpcDeaths()
        {
            TotalNpcDeaths++;
        }

        public void SetWipeState(bool state)
        {
            IsWipe = state;
        }

        public object? GetAttrKV(long uuid, string key)
        {
            return GetOrCreateEntity(uuid).GetAttrKV(key);
        }

        public bool HasStatsBeenRecorded(bool includeHealingAndTaken = false)
        {
            if (!includeHealingAndTaken)
            {
                return TotalDamage > 0 || TotalNpcDamage > 0;
            }

            return TotalDamage > 0 || TotalHealing > 0 || TotalTakenDamage > 0 || TotalNpcTakenDamage > 0 || TotalNpcDamage > 0 || TotalNpcShieldBreak > 0 || TotalNpcHealing > 0;
        }

        public void RegisterSkillActivation(long uuid, int skillId)
        {
            var entity = GetOrCreateEntity(uuid);
            entity.RegisterSkillActivation(skillId);
        }

        public void AddDamage(
            long attackerUuid, long targetUuid, int skillId, int skillLevel, long damage, long hpLessen,
            EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode,
            bool isCrit, bool isLucky, bool isCauseLucky, bool isMiss, bool isDead, ExtraPacketData extraPacketData)
        {
            LastUpdate = extraPacketData.ArrivalTime;

            var attackerType = (EEntityType)Utils.UuidToEntityType(attackerUuid);
            var targetType = (EEntityType)Utils.UuidToEntityType(targetUuid);

            if (attackerType == EEntityType.EntMonster)
            {
                TotalNpcDamage += (ulong)damage;
                if (damageType == EDamageType.Absorbed)
                {
                    TotalNpcShieldBreak += (ulong)damage;
                }
            }
            else
            {
                TotalDamage += (ulong)damage;
                if (damageType == EDamageType.Absorbed)
                {
                    TotalShieldBreak += (ulong)damage;
                }
            }

            GetOrCreateEntity(attackerUuid).AddDamage(targetUuid, skillId, skillLevel, damage, hpLessen, damageElement, damageType, damageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, extraPacketData);
        }

        public void AddHealing(
            long attackerUuid, long targetUuid, int skillId, int skillLevel, long damage, long hpLessen,
            EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode,
            bool isCrit, bool isLucky, bool isCauseLucky, bool isMiss, bool isDead, ExtraPacketData extraPacketData)
        {
            LastUpdate = extraPacketData.ArrivalTime;

            var attackerType = (EEntityType)Utils.UuidToEntityType(attackerUuid);

            if (attackerType == EEntityType.EntMonster)
            {
                TotalNpcHealing += (ulong)damage;
            }
            else
            {
                TotalHealing += (ulong)damage;
            }

            var entity = GetOrCreateEntity(attackerUuid);

            long? currentHp = entity.GetAttrKV("AttrHp") as long?;
            long? maxHp = entity.GetAttrKV("AttrMaxHp") as long?;

            long overhealing = 0;
            long effectiveHealing = 0;

            if ((currentHp != null && maxHp != null) && (currentHp + damage > maxHp))
            {
                effectiveHealing = (long)(maxHp - currentHp);
                overhealing = damage - effectiveHealing;
            }

            if (attackerType == EEntityType.EntMonster)
            {
                TotalNpcOverhealing += (ulong)overhealing;
            }
            else
            {
                TotalOverhealing += (ulong)overhealing;
            }
            
            entity.AddHealing(targetUuid, skillId, skillLevel, damage, overhealing, effectiveHealing, hpLessen, damageElement, damageType, damageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, extraPacketData);
        }

        public void AddTakenDamage(
            long attackerUuid, long targetUuid, int skillId, int skillLevel, long damage, long hpLessen,
            EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode,
            bool isCrit, bool isLucky, bool isCauseLucky, bool isMiss, bool isDead, ExtraPacketData extraPacketData)
        {
            LastUpdate = extraPacketData.ArrivalTime;

            var targetType = (EEntityType)Utils.UuidToEntityType(targetUuid);
            if (targetType == EEntityType.EntMonster)
            {
                TotalNpcTakenDamage += (ulong)damage;
            }
            else
            {
                TotalTakenDamage += (ulong)damage;
            }

            GetOrCreateEntity(targetUuid).AddTakenDamage(attackerUuid, skillId, skillLevel, damage, hpLessen, damageElement, damageType, damageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, extraPacketData);
        }

        public void AddShieldGained(long entityUuid, long shieldBuffUuid, long value, long initialValue, long maxValue = 0)
        {
            // Check to make sure the shieldBuffUuid is not already in the shieldGain list, otherwise this is just an update for an existing shield and we're only tracking total gain for now
            //GetOrCreateEntity(entityUuid).AddBuffEventAttribute(shieldBuffUuid, "AttrShieldList", 0);
        }

        public void NotifyBuffEvent(long entityUuid, EBuffEventType buffEventType, int buffUuid, int baseId, int level, long fireUuid, int layer, int duration, int sourceConfigId, ExtraPacketData extraPacketData)
        {
            string entityCasterName = "";
            if (fireUuid > 0)
            {
                var caster = GetOrCreateEntity(fireUuid);
                if (!string.IsNullOrEmpty(caster.Name))
                {
                    entityCasterName = caster.Name;
                }
            }
            GetOrCreateEntity(entityUuid).NotifyBuffEvent(buffEventType, buffUuid, baseId, level, fireUuid, entityCasterName, layer, duration, sourceConfigId, DateTime.Now.Subtract(EncounterManager.Current.StartTime));
        }

        protected virtual void OnSkillActivated(SkillActivatedEventArgs e)
        {
            SkillActivated?.Invoke(this, e);
        }

        protected virtual void OnBossHpUpdated(object sender, HpUpdatedEventArgs e)
        {
            Entity entity = (Entity)sender;

            // This will result in the last updated entity to become the new boss as long as the HP is not max
            if (entity.Hp > -1 && entity.MaxHp > 0 && entity.Hp < entity.MaxHp)
            {
                // Entity has taken damage and is likely the real boss if there's multiple found
                if (BossUUID != entity.UUID)
                {
                    BossUUID = entity.UUID;
                    BossName = entity.Name;
                    var attr_id = entity.GetAttrKV("AttrId");
                    if (attr_id != null)
                    {
                        BossAttrId = (long)(int)attr_id;
                    }
                }

                BossHpPct = (int)(((double)entity.Hp / (double)entity.MaxHp) * 100000.0);
            }

            // We'll call this always even if it's not the true boss to keep events flowing down the chain
            BossHpUpdated?.Invoke(sender, e);
        }

        protected virtual void OnEntityHpUpdated(object sender, HpUpdatedEventArgs e)
        {
            EntityHpUpdated?.Invoke(sender, e);
        }

        protected virtual void OnEntityThreatListUpdated(object sender, ThreatListUpdatedEventArgs e)
        {
            EntityThreatListUpdated?.Invoke(sender, e);
        }

        public void RemoveEntityHandlers()
        {
            foreach (var entity in Entities)
            {
                entity.Value.RemoveEventHandlers();
            }
        }

        public void RemoveEventHandlers()
        {
            SkillActivated = null;
            BossHpUpdated = null;
            EntityHpUpdated = null;

            RemoveEntityHandlers();
        }
    }

    [ProtoContract]
    public class EncounterExData
    {
        [ProtoMember(1)]
        public string Test { get; set; } = "UWU";

        public EncounterExData() { }
    }

    public class Entity : System.ICloneable
    {
        public long UUID { get; set; }
        public long UID { get; set; }
        public EEntityType EntityType { get; private set; }
        public string Name { get; private set; }
        public int AbilityScore { get; private set; } = 0;
        public int ProfessionId { get; private set; } = 0;
        public string Profession { get; private set; }
        public int SubProfessionId { get; private set; } = 0;
        public string SubProfession { get; private set; }
        public int Level { get; set; } = 0;
        public Vector3 Position { get; private set; } = new();

        public CombatStats2 DamageStats { get; set; } = new();
        public CombatStats2 HealingStats { get; set; } = new();
        public CombatStats2 TakenStats { get; set; } = new();

        public ConcurrentDictionary<int, CombatStats2> SkillStats { get; set; } = new();
        public List<ActionStat> ActionStats { get; set; } = new();

        public ulong TotalDamage { get; set; } = 0;
        public ulong TotalShieldBreak { get; set; } = 0;
        public ulong TotalHealing { get; set; } = 0;
        public ulong TotalOverhealing { get; set; } = 0;
        public ulong TotalTakenDamage { get; set; } = 0;
        public ulong TotalShield { get; set; } = 0;
        public ulong TotalCasts { get; set; } = 0;
        public ulong TotalDeaths { get; set; } = 0;

        // The key is the BuffUuid, however it is specifically a ulong here to enable key-based and index-based lookups
        // An OrderedDictionary automatically uses an int32 as the index lookup and using an int32 as the key overrides that
        // Whenever we want to perform a key-based (BuffUuid) lookup on this, cast to a ulong to make it work
        public ThreadSafeOrderedDictionary<ulong, BuffEvent> BuffEvents { get; set; } = new();

        // Monster specific variables
        // When -1, this is unset (non-Monsters will be at -1), when 1 this is Elite, when 2 it is a boss
        public EMonsterType MonsterType { get; set; } = EMonsterType.Unknown;

        public long Hp { get; private set; } = 0;
        public long MaxHp { get; private set; } = 0;
        public ConcurrentQueue<long> RecentHpHistory { get; private set; } = new();

        public ThreatInfo ThreatInfo { get; private set; } = new();
        public ConcurrentQueue<ThreatInfo> RecentThreatInfoHistory { get; private set; } = new();

        public Dictionary<string, object> Attributes { get; set; } = new();

        public delegate void SkillActivatedEventHandler(object sender, SkillActivatedEventArgs e);
        public event SkillActivatedEventHandler SkillActivated;
        public delegate void HpUpdatedEventHandler(object sender, HpUpdatedEventArgs e);
        public event HpUpdatedEventHandler HpUpdated;
        public delegate void ThreatListUpdatedEventHandler(object sender, ThreatListUpdatedEventArgs e);
        public event ThreatListUpdatedEventHandler ThreatListUpdated;

        public object Clone()
        {
            var cloned = this.MemberwiseClone();
            ((Entity)cloned).DamageStats = (CombatStats2)this.DamageStats.Clone();
            ((Entity)cloned).HealingStats = (CombatStats2)this.HealingStats.Clone();
            ((Entity)cloned).TakenStats = (CombatStats2)this.TakenStats.Clone();
            //((Entity)cloned).SkillStats.Clear();
            // This cursed loop ensures we dereference all the items and don't break the current encounter tracker
            foreach (var skillStat in this.SkillStats)
            {
                ((Entity)cloned).SkillStats.AddOrUpdate(skillStat.Key, (CombatStats2)skillStat.Value.Clone(), (key, value) => (CombatStats2)skillStat.Value.Clone());
            }
            return cloned;
        }

        public void RemoveEventHandlers()
        {
            SkillActivated = null;
            HpUpdated = null;
            ThreatListUpdated = null;
        }

        [JsonConstructor]
        public Entity(long uuid, string name = null)
        {
            UUID = uuid;
            UID = Utils.UuidToEntityId(uuid);
            Name = name;

            SetEntityType((EEntityType)Utils.UuidToEntityType(uuid));

            var cached = EntityCache.Instance.GetOrCreate(uuid);
            if (cached != null)
            {
                if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(cached.Name))
                {
                    SetName(cached.Name);
                }

                if (AbilityScore == 0 && cached.AbilityScore != 0)
                {
                    SetAbilityScore(cached.AbilityScore);
                }

                if (Level == 0 && cached.Level != 0)
                {
                    SetLevel(cached.Level);
                }

                if (ProfessionId == 0 && cached.ProfessionId != 0)
                {
                    SetProfessionId(cached.ProfessionId);
                }

                if (SubProfessionId == 0 && cached.SubProfessionId != 0)
                {
                    SetSubProfessionId(cached.SubProfessionId);
                }
            }

            // Ensure cache is updated with latest used name from here
            //SetName(Name);
        }

        public void SetName(string name)
        {
            Name = name;

            var cached = EntityCache.Instance.GetOrCreate(UUID);
            if (cached != null && !string.IsNullOrEmpty(name))
            {
                cached.Name = name;
            }
        }

        public void SetEntityType(EEntityType type)
        {
            EntityType = type;

            if (type != EEntityType.EntChar)
            {
                // We only want a name brought in from the cache for players
                // Monsters (and other types) should be set from fresh data every time
                // If we don't do that, we run into many UID collisions unfortunately

                // TODO: Disable this for now

                //SetName("");
            }

            if (type == EEntityType.EntMonster)
            {
                // Always reset the name and monster type data if we have an AttrId at this point
                var attr_id = GetAttrKV("AttrId");
                if (attr_id != null)
                {
                    UID = (int)attr_id;
                    if (HelperMethods.DataTables.Monsters.Data.TryGetValue(attr_id.ToString(), out var monsterEntry))
                    {
                        SetName(monsterEntry.Name);
                        SetMonsterType(monsterEntry.MonsterType);
                    }
                }
            }
        }

        public void SetAbilityScore(int abilityScore)
        {
            AbilityScore = abilityScore;

            var cached = EntityCache.Instance.GetOrCreate(UUID);
            if (cached != null && abilityScore != 0)
            {
                cached.AbilityScore = abilityScore;
            }
        }

        public void SetProfessionId(int id)
        {
            ProfessionId = id;
            Profession = Professions.GetProfessionNameFromId(id);

            var cached = EntityCache.Instance.GetOrCreate(UUID);
            if (cached != null && id != 0)
            {
                cached.ProfessionId = id;
            }
        }

        public void SetSubProfessionId(int id)
        {
            SubProfessionId = id;
            SubProfession = Professions.GetSubProfessionNameFromId(id);

            var cached = EntityCache.Instance.GetOrCreate(UUID);
            if (cached != null && id != 0)
            {
                cached.SubProfessionId = id;
            }
        }

        public void SetLevel(int level)
        {
            Level = level;

            var cached = EntityCache.Instance.GetOrCreate(UUID);
            if (cached != null && level != 0)
            {
                cached.Level = level;
            }
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        /// <summary>
        /// Set the current entity HP without triggering any update events. This is mainly to be used for restoring the proper HP after an Encounter Wipe
        /// </summary>
        /// <param name="hp"></param>
        public void SetHpNoUpdate(long hp)
        {
            Hp = hp;
        }

        public void SetHpValues(long hp = -1, long maxHp = -1)
        {
            if (hp != -1)
            {
                Hp = hp;

                // We store only the last few updates so we can walk it back on wipes to see last real hp value
                if (RecentHpHistory.Count > 5)
                {
                    RecentHpHistory.TryDequeue(out _);
                }
                RecentHpHistory.Enqueue(hp);
            }

            if (maxHp != -1)
            {
                MaxHp = maxHp;
            }

            if ((Hp > -1) && (MaxHp > -1))
            {
                OnHpUpdated(new HpUpdatedEventArgs() { EntityUuid = UUID, Hp = Hp, MaxHp = MaxHp, UpdateDateTime = DateTime.Now });
            }
        }

        protected virtual void OnHpUpdated(HpUpdatedEventArgs e)
        {
            HpUpdated?.Invoke(this, e);
        }

        public void SetThreatList(ThreatInfo threatInfo)
        {
            if (RecentThreatInfoHistory.Count > 5)
            {
                RecentThreatInfoHistory.TryDequeue(out _);
            }
            RecentThreatInfoHistory.Enqueue(threatInfo);

            ThreatInfo = threatInfo;

            OnThreatListUpdated(new ThreatListUpdatedEventArgs() { EntityUuid = UUID, ThreatInfo = ThreatInfo });
        }

        protected virtual void OnThreatListUpdated(ThreatListUpdatedEventArgs e)
        {
            ThreatListUpdated?.Invoke(this, e);
        }

        public void IncrementDeaths()
        {
            TotalDeaths++;
        }

        public void SetDeaths(ulong deaths)
        {
            TotalDeaths = deaths;
        }

        public void SetMonsterType(int type)
        {
            MonsterType = (EMonsterType)type;
        }

        public void RegisterSkillActivation(int skillId)
        {
            if (!SkillStats.TryGetValue(skillId, out var stats))
            {
                var combatStats = new CombatStats2();

                if (HelperMethods.DataTables.Skills.Data.TryGetValue(skillId.ToString(), out var skill))
                {
                    combatStats.SetName(skill.Name);
                }

                combatStats.RegisterActivation();
                SkillStats.TryAdd(skillId, combatStats);
            }
            else
            {
                stats.RegisterActivation();
            }

            TotalCasts++;
            OnSkillActivated(new SkillActivatedEventArgs { CasterUuid = UUID, SkillId = skillId, ActivationDateTime = DateTime.Now });
        }

        protected virtual void OnSkillActivated(SkillActivatedEventArgs e)
        {
            SkillActivated?.Invoke(this, e);
        }

        public void RegisterSkillData(ESkillType skillType, int skillId, int skillLevel, long value, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky, EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode, bool isDead, ExtraPacketData extraPacketData)
        {
            if (!SkillStats.TryGetValue(skillId, out var stats))
            {
                var combatStats = new CombatStats2();

                combatStats.SetSkillType(skillType);

                if (HelperMethods.DataTables.Skills.Data.TryGetValue(skillId.ToString(), out var skill))
                {
                    combatStats.SetName(skill.Name);
                }

                combatStats.AddData(value, skillLevel, isCrit, isLucky, hpLessenValue, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);
                SkillStats.TryAdd(skillId, combatStats);
            }
            else
            {
                stats.SetSkillType(skillType);
                stats.AddData(value, skillLevel, isCrit, isLucky, hpLessenValue, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);
            }
        }

        public void AddDamage(long targetUuid, int skillId, int skillLevel, long damage, long hpLessen,
            EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode,
            bool isCrit, bool isLucky, bool isCauseLucky, bool isMiss, bool isDead, ExtraPacketData extraPacketData)
        {
            TotalDamage += (ulong)damage;

            if (damageType == EDamageType.Absorbed)
            {
                TotalShieldBreak += (ulong)damage;
            }

            DamageStats.AddData(damage, skillLevel, isCrit, isLucky, hpLessen, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);

            RegisterSkillData(ESkillType.Damage, skillId, skillLevel, damage, isCrit, isLucky, hpLessen, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);

            // Always attempt to update the sub profession data as they may have changed classes or not been detected properly yet
            var subProfessionId = Professions.GetSubProfessionIdBySkillId(skillId);
            if (subProfessionId != 0)
            {
                SetSubProfessionId((int)subProfessionId);
            }
        }

        public void AddHealing(
            long targetUuid, int skillId, int skillLevel, long damage, long overhealing, long effectiveHealing, long hpLessen,
            EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode,
            bool isCrit, bool isLucky, bool isCauseLucky, bool isMiss, bool isDead, ExtraPacketData extraPacketData)
        {
            TotalHealing += (ulong)damage;
            TotalOverhealing += (ulong)overhealing;
            HealingStats.AddData(damage, skillLevel, isCrit, isLucky, hpLessen, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);

            RegisterSkillData(ESkillType.Healing, skillId, skillLevel, damage, isCrit, isLucky, overhealing, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);

            // Always attempt to update the sub profession data as they may have changed classes or not been detected properly yet
            var subProfessionId = Professions.GetSubProfessionIdBySkillId(skillId);
            if (subProfessionId != 0)
            {
                SetSubProfessionId((int)subProfessionId);
            }
        }

        public void AddTakenDamage(
            long attackerUuid, int skillId, int skillLevel, long damage, long hpLessen,
            EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode,
            bool isCrit, bool isLucky, bool isCauseLucky, bool isMiss, bool isDead, ExtraPacketData extraPacketData)
        {
            TotalTakenDamage += (ulong)damage;
            TakenStats.AddData(damage, skillLevel, isCrit, isLucky, hpLessen, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);
            RegisterSkillData(ESkillType.Taken, skillId, skillLevel, damage, isCrit, isLucky, hpLessen, isCauseLucky, damageElement, damageType, damageMode, isDead, extraPacketData);
        }

        public void NotifyBuffEvent(EBuffEventType buffEventType, int buffUuid, int baseId, int level, long fireUuid, string entityCasterName, int layer, int duration, int sourceConfigId, TimeSpan encounterTime)
        {
            if (baseId > 0)
            {
                if (!BuffEvents.TryGetValue((ulong)buffUuid, out var buffEvent))
                {
                    buffEvent = new BuffEvent(buffUuid, baseId, level, fireUuid, entityCasterName, layer, duration, sourceConfigId);
                }
                else
                {
                    if (buffEvent.BaseId <= 0)
                    {
                        buffEvent.SetEvent(buffUuid, baseId, level, fireUuid, entityCasterName, layer, duration, sourceConfigId);
                    }
                }
                buffEvent.SetAddTime(encounterTime.Duration());

                if (!Settings.Instance.UseDatabaseForEncounterHistory && Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase && BuffEvents.Count > 99)
                {
                    BuffEvents.Remove(BuffEvents.AsValueEnumerable().First().Key);
                }

                BuffEvents[(ulong)buffUuid] = buffEvent;
            }
            else
            {
                if (!BuffEvents.TryGetValue((ulong)buffUuid, out var buffEvent))
                {
                    // A remove event would only be coming with the uuid and type
                    buffEvent = new BuffEvent(buffUuid);
                }
                buffEvent.SetRemoveTime(encounterTime.Duration());

                if (!Settings.Instance.UseDatabaseForEncounterHistory && Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase && BuffEvents.Count > 99)
                {
                    BuffEvents.Remove(BuffEvents.AsValueEnumerable().First().Key);
                }

                BuffEvents[(ulong)buffUuid] = buffEvent;
            }
            //else
            {
                //System.Diagnostics.Debug.WriteLine($"NotifyBuffEvent Unhandled BuffEventType: {buffEventType}");
            }
        }

        public void AddBuffEventAttribute(int buffUuid, string attributeName, object? attributeValue)
        {
            if (!BuffEvents.TryGetValue((ulong)buffUuid, out var buffEvent))
            {
                buffEvent = new BuffEvent(buffUuid);

                if (attributeName == "AttrShieldList" && attributeValue != null)
                {
                    var shieldInfo = (ShieldInfo)attributeValue;
                    TotalShield += (ulong)shieldInfo.InitialValue;
                }
            }
            buffEvent.AddData(attributeName, attributeValue);

            if (!Settings.Instance.UseDatabaseForEncounterHistory && Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase && BuffEvents.Count > 99)
            {
                BuffEvents.Remove(BuffEvents.AsValueEnumerable().First().Key);
            }

            BuffEvents[(ulong)buffUuid] = buffEvent;
        }

        public void SetAttrKV(string key, object value)
        {
            Attributes[key] = value;
        }

        public object? GetAttrKV(string key)
        {
            var value = Attributes.TryGetValue(key, out var val) ? val : null;
            return value;
        }

        public bool IsHpUpdatedHandlerSubscribed(HpUpdatedEventHandler handler)
        {
            Delegate[] invocationList = HpUpdated?.GetInvocationList();
            return invocationList != null && invocationList.Contains(handler);
        }

        public bool IsThreatListUpdatedHandlerSubscribed(ThreatListUpdatedEventHandler handler)
        {
            Delegate[] invocationList = ThreatListUpdated?.GetInvocationList();
            return invocationList != null && invocationList.Contains(handler);
        }

        // Merges the data from another entity with this one, does not check the UUIDs match first
        public void MergeEntity(Entity newEntity)
        {
            Name = newEntity.Name;
            Level = newEntity.Level;
            if (newEntity.AbilityScore > 0)
            {
                SetAbilityScore(newEntity.AbilityScore);
            }
            if (newEntity.ProfessionId > 0)
            {
                SetProfessionId(newEntity.ProfessionId);
            }
            if (newEntity.SubProfessionId > 0)
            {
                SetSubProfessionId(newEntity.SubProfessionId);
            }
            
            TotalDamage += newEntity.TotalDamage;
            TotalShieldBreak += newEntity.TotalShieldBreak;
            TotalHealing += newEntity.TotalHealing;
            TotalOverhealing += newEntity.TotalOverhealing;
            TotalTakenDamage += newEntity.TotalTakenDamage;
            TotalShield += newEntity.TotalShield;
            TotalCasts += newEntity.TotalCasts;
            TotalDeaths += newEntity.TotalDeaths;

            DamageStats.MergeCombatStats(newEntity.DamageStats);
            HealingStats.MergeCombatStats(newEntity.HealingStats);
            TakenStats.MergeCombatStats(newEntity.TakenStats);

            // Merge Attributes, this might be a bit weird given what values Attributes can hold
            foreach (var newAttr in newEntity.Attributes)
            {
                if (Attributes.ContainsKey(newAttr.Key))
                {
                    Attributes[newAttr.Key] = newAttr.Value;
                }
                else
                {
                    Attributes.Add(newAttr.Key, newAttr.Value);
                }
            }

            // Merge SkillStats
            foreach (var newSkillStat in newEntity.SkillStats)
            {
                SkillStats.TryGetValue(newSkillStat.Key, out var foundSkill);
                if (foundSkill != null)
                {
                    foundSkill.MergeCombatStats(newSkillStat.Value);
                }
                else
                {
                    SkillStats.TryAdd(newSkillStat.Key, (CombatStats2)newSkillStat.Value.Clone());
                }
            }
        }
    }

    public enum EMonsterType : int
    {
        Unknown = -1,
        Monster = 0,
        Elite = 1,
        Boss = 2
    }

    public class SkillActivatedEventArgs : EventArgs
    {
        public long CasterUuid { get; set; }
        public int SkillId { get; set; }
        public DateTime ActivationDateTime { get; set; }
    }

    public class HpUpdatedEventArgs : EventArgs
    {
        public long EntityUuid { get; set; }
        public long Hp { get; set; }
        public long MaxHp { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }

    public class ThreatInfo
    {
        public long EntityUuid { get; set; }
        public long ThreatValue { get; set; }
    }

    public class ThreatListUpdatedEventArgs : EventArgs
    {
        public long EntityUuid { get; set; }
        public ThreatInfo ThreatInfo { get; set; } = new();
    }

    public enum ESkillType : int
    {
        Unknown = 0,
        Damage = 1,
        Healing = 2,
        Taken = 3
    }

    public class CombatStats2 : System.ICloneable
    {
        public string Name { get; private set; }
        public ESkillType SkillType { get; private set; } = ESkillType.Unknown;
        public int Level { get; private set; }
        public int TierLevel { get; private set; }
        public long SummonUUID { get; private set; }

        public EDamageProperty DamageElement { get; private set; }
        public EDamageMode DamageMode { get; private set; }

        public ulong ValueTotal { get; private set; }
        public ulong ValueNormalTotal { get; private set; }
        public ulong ValueCritTotal { get; private set; }
        public ulong ValueLuckyTotal { get; private set; }
        public ulong ValueCritLuckyTotal { get; private set; }
        public long ValueMax { get; private set; }
        public long ValueMin { get; private set; }
        public double ValueAverage { get; private set; }
        public double ValuePerSecond { get; private set; }
        public double TrueValuePerSecond { get; set; }

        public uint MissCount { get; private set; }
        public double MissRate { get; private set; }
        
        public uint CritCount { get; private set; }
        public double CritRate { get; private set; }
        
        public uint LuckyCount { get; private set; }
        public double LuckyRate { get; private set; }

        public uint CritLuckyCount { get; private set; }

        public uint NormalCount { get; private set; }
        public uint KillCount { get; private set; }
        public ulong HitsCount { get; private set; }
        public uint CastsCount { get; private set; }

        public DateTime? StartTime = null;
        public DateTime? EndTime = null;

        public List<SkillSnapshot> SkillSnapshots { get; private set; } = new();

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public void SetSkillType(ESkillType skillType)
        {
            this.SkillType = skillType;
        }

        public void RegisterActivation()
        {
            CastsCount++;
        }

        private void AddValue(long value)
        {
            ValueTotal += (ulong)value;

            if (value > 0)
            {
                if (value < ValueMin)
                {
                    ValueMin = value;
                }
                if (value > ValueMax)
                {
                    ValueMax = value;
                }
            }
        }

        private void AddNormalValue(long value)
        {
            ValueNormalTotal += (ulong)value;
        }

        private void AddCritValue(long value)
        {
            ValueCritTotal += (ulong)value;
        }

        private void AddLuckyValue(long value)
        {
            ValueLuckyTotal += (ulong)value;
        }

        public void AddData(long value, int level, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky, EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode, bool isDead, ExtraPacketData extraPacketData)
        {
            DateTime now = extraPacketData.ArrivalTime;
            StartTime ??= now;
            EndTime = now;

            AddValue(value);

            Level = level;

            DamageElement = damageElement;
            DamageMode = damageMode;

            if (isCrit)
            {
                CritCount++;
                AddCritValue(value);
            }

            if (isLucky)
            {
                LuckyCount++;
                AddLuckyValue(value);
            }

            if (!isCrit && !isLucky)
            {
                NormalCount++;
                AddNormalValue(value);
            }
            else
            {
                CritLuckyCount++;
            }

            if (isDead)
            {
                KillCount++;
            }

            HitsCount++;

            ValueCritLuckyTotal = ValueCritTotal + ValueLuckyTotal;

            ValueAverage = HitsCount > 0 ? Math.Round(((double)ValueTotal / (double)HitsCount), 0) : 0.0;
            CritRate = HitsCount > 0 ? Math.Round(((double)CritCount / (double)HitsCount) * 100.0, 0) : 0.0;
            LuckyRate = HitsCount > 0 ? Math.Round(((double)LuckyCount / (double)HitsCount) * 100.0, 0) : 0.0;

            if (StartTime != null && EndTime != null && StartTime < EndTime)
            {
                var seconds = (EndTime.Value - StartTime.Value).TotalSeconds;
                if (seconds >= 1.0)
                {
                    ValuePerSecond = seconds > 0 ? Math.Round((double)ValueTotal / seconds, 0) : 0;
                }
                else
                {
                    ValuePerSecond = ValueTotal;
                }
            }

            AddSnapshot(value, isCrit, isLucky, hpLessenValue, isCauseLucky, damageElement, damageType, damageMode, isDead, now);
        }

        public void SetSummonData(long uuid, int level)
        {
            SummonUUID = uuid;
            TierLevel = level;
        }

        public void AddSnapshot(long value, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky, EDamageProperty damageElement, EDamageType damageType, EDamageMode damageMode, bool isDead, DateTime timestamp)
        {
            var snapshot = new SkillSnapshot()
            {
                Value = value,
                IsCrit = isCrit,
                IsLucky = isLucky,
                IsCritLucky = isCrit && isLucky,
                IsCauseLucky = isCauseLucky,
                DamageElement = damageElement,
                DamageType = damageType,
                DamageMode = damageMode,
                IsKill = isDead,
                Timestamp = timestamp
            };

            if (damageType == EDamageType.Miss)
            {
                snapshot.IsMiss = true;
            }
            else
            {
                // TODO: May want to refine this logic more or just remove it all
                snapshot.IsHit = true;
            }

            SkillSnapshots.Add(snapshot);
        }

        // Merges the data from another Combat Stats with this one, always uses the new Name and SkillType
        public void MergeCombatStats(CombatStats2 newCombatStats)
        {
            if (!string.IsNullOrEmpty(newCombatStats.Name))
            {
                SetName(newCombatStats.Name);
            }
            SetSkillType(newCombatStats.SkillType);

            ValueTotal += newCombatStats.ValueTotal;
            ValueNormalTotal += newCombatStats.ValueNormalTotal;
            ValueCritTotal += newCombatStats.ValueCritTotal;
            ValueLuckyTotal += newCombatStats.ValueLuckyTotal;
            ValueCritLuckyTotal += newCombatStats.ValueCritLuckyTotal;
            ValueMax = newCombatStats.ValueMax > ValueMax ? newCombatStats.ValueMax : ValueMax;
            ValueMin = newCombatStats.ValueMin < ValueMin ? newCombatStats.ValueMin : ValueMin;

            MissCount += newCombatStats.MissCount;
            CritCount += newCombatStats.CritCount;
            LuckyCount += newCombatStats.LuckyCount;
            CritLuckyCount += newCombatStats.CritLuckyCount;
            NormalCount += newCombatStats.NormalCount;
            KillCount += newCombatStats.KillCount;
            HitsCount += newCombatStats.HitsCount;
            CastsCount += newCombatStats.CastsCount;

            ValueAverage = HitsCount > 0 ? Math.Round(((double)ValueTotal / (double)HitsCount), 0) : 0.0;
            CritRate = HitsCount > 0 ? Math.Round(((double)CritCount / (double)HitsCount) * 100.0, 0) : 0.0;
            LuckyRate = HitsCount > 0 ? Math.Round(((double)LuckyCount / (double)HitsCount) * 100.0, 0) : 0.0;

            if (newCombatStats.StartTime.HasValue)
            {
                if (StartTime.HasValue)
                {
                    if (newCombatStats.StartTime.Value < StartTime.Value)
                    {
                        StartTime = newCombatStats.StartTime.Value;
                    }
                }
                else
                {
                    StartTime = newCombatStats.StartTime.Value;
                }
            }

            if (newCombatStats.EndTime.HasValue)
            {
                if (EndTime.HasValue)
                {
                    if (newCombatStats.EndTime.Value > EndTime.Value)
                    {
                        EndTime = newCombatStats.EndTime.Value;
                    }
                }
                else
                {
                    EndTime = newCombatStats.EndTime.Value;
                }
            }

            if (StartTime != null && EndTime != null && StartTime < EndTime)
            {
                var seconds = (EndTime.Value - StartTime.Value).TotalSeconds;
                if (seconds >= 1.0)
                {
                    ValuePerSecond = seconds > 0 ? Math.Round((double)ValueTotal / seconds, 0) : 0;
                }
                else
                {
                    ValuePerSecond = ValueTotal;
                }
            }
        }
    }

    public class SkillSnapshot
    {
        public long Value { get; set; }

        public EDamageProperty DamageElement { get; set; }
        public EDamageType DamageType { get; set; }
        public EDamageMode DamageMode { get; set; }

        public bool IsCrit { get; set; }
        public bool IsLucky { get; set; }
        public bool IsCritLucky { get; set; }
        public bool IsCauseLucky { get; set; }
        public bool IsHit { get; set; }
        public bool IsMiss { get; set; }
        public bool IsKill { get; set; }

        public DateTime? Timestamp { get; set; } = null;
    }

    public class BuffEvent
    {
        public DataTypes.Enum.EBuffType BuffType { get; private set; }
        public DataTypes.Enum.EBuffPriority BuffPriority { get; private set; }
        public int BuffVisibility { get; private set; }
        public long Uuid { get; private set; }
        public int BaseId { get; private set; } // Buff Id in BuffTable
        public int Level { get; private set; }
        public long FireUuid { get; private set; } // UUID of entity that cast this event
        public string EntityCasterName { get; private set; }
        public int Layer { get; private set; }
        public int Duration { get; private set; }
        public int SourceConfigId { get; private set; } // Original Skill Id
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Icon { get; private set; }
        public int BuffAbilityType { get; private set; }
        public int BuffAbilitySubType { get; private set; }
        public TimeSpan EventAddTime { get; private set; }
        public TimeSpan EventRemoveTime { get; private set; }
        public string AttributeName { get; private set; }
        public object? Data { get; private set; }

        public BuffEvent(long uuid)
        {
            Uuid = uuid;
        }

        [JsonConstructor]
        public BuffEvent(int uuid, int baseId, int level, long fireUuid, string entityCasterName, int layer, int duration, int sourceConfigId)
        {
            Uuid = uuid;
            BaseId = baseId;
            Level = level;
            FireUuid = fireUuid;
            EntityCasterName = entityCasterName;
            Layer = layer;
            Duration = duration;
            SourceConfigId = sourceConfigId;

            if (BaseId > 0)
            {
                if (HelperMethods.DataTables.Buffs.Data.TryGetValue(baseId.ToString(), out var buffTableData))
                {
                    Name = buffTableData.Name;
                    Description = buffTableData.Desc;
                    Icon = buffTableData.GetIconName();
                    BuffType = buffTableData.BuffType.Value;
                    BuffPriority = buffTableData.BuffPriority.Value;
                    BuffVisibility = buffTableData.Visible;
                    BuffAbilityType = buffTableData.BuffAbilityType;
                    BuffAbilitySubType = buffTableData.BuffAbilitySubType;
                }
            }

            if (sourceConfigId > 0)
            {
                if (HelperMethods.DataTables.Skills.Data.TryGetValue(sourceConfigId.ToString(), out var skillTableData))
                {
                    //Name = $"{Name} ({skillTableData.Name})";

                    if (string.IsNullOrWhiteSpace(Icon))
                    {
                        Icon = skillTableData.GetIconName();
                    }
                }
            }
        }

        public void AddData(string attributeName, object? data)
        {
            AttributeName = attributeName;
            Data = data;
        }

        public void SetAddTime(TimeSpan time)
        {
            EventAddTime = time;
        }

        public void SetRemoveTime(TimeSpan time)
        {
            EventRemoveTime = time;
        }

        public void SetEntitySourceNameFromUuid(string name)
        {

        }

        public void SetEvent(int uuid, int baseId, int level, long fireUuid, string entityCasterName, int layer, int duration, int sourceConfigId)
        {
            Uuid = uuid;
            BaseId = baseId;
            Level = level;
            FireUuid = fireUuid;
            EntityCasterName = entityCasterName;
            Layer = layer;
            Duration = duration;
            SourceConfigId = sourceConfigId;

            if (BaseId > 0)
            {
                if (HelperMethods.DataTables.Buffs.Data.TryGetValue(baseId.ToString(), out var buffTableData))
                {
                    Name = buffTableData.Name;
                    Description = buffTableData.Desc;
                    Icon = buffTableData.Icon;

                    // These should have already existed from the constructor
                    BuffType = buffTableData.BuffType.Value;
                    BuffPriority = buffTableData.BuffPriority.Value;
                    BuffVisibility = buffTableData.Visible;
                    BuffAbilityType = buffTableData.BuffAbilityType;
                    BuffAbilitySubType = buffTableData.BuffAbilitySubType;
                }
            }
        }
    }

    public class ActionStat
    {
        public DateTime ActivationTime;
        public int ActionType;
        public int ActionId; // Typically is a SkillId
        public string ActionName;

        public ActionStat(DateTime activationTime, int actionType, int actionId)
        {
            ActivationTime = activationTime;
            ActionType = actionType;
            ActionId = actionId;

            if (HelperMethods.DataTables.Skills.Data.ContainsKey(actionId.ToString()))
            {
                ActionName = HelperMethods.DataTables.Skills.Data[actionId.ToString()].Name;
            }
        }
    }
}
