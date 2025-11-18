using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public static uint LevelMapId {  get; private set; }
        public static string SceneName { get; private set; }

        static EncounterManager()
        {
            // Give a default encounter for now
            StartNewBattle();
            StartEncounter();
        }

        public static void StartEncounter(bool force = false)
        {
            if (Current != null)
            {
                if (force || (Current.EndTime == DateTime.MinValue && Current.HasStatsBeenRecorded()))
                {
                    // We called StartEncounter without first stopping the current one
                    StopEncounter();
                }
                else
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
            }
            //Encounters.Add(new Encounter(CurrentBattleId));

            //CurrentEncounter = Encounters.Count - 1;

            if (Current != null)
            {
                DB.InsertEncounter(Current);
            }

            Current = new Encounter(CurrentBattleId);


            // Reuse last sceneId as our current one (it may not always be right but hopefully is right enough)
            if (LevelMapId > 0)
            {
                SetSceneId(LevelMapId);
            }
        }

        public static void StopEncounter()
        {
            if (Current != null && Current.EndTime == DateTime.MinValue)
            {
                Current.SetEndTime(DateTime.Now);
            }
            EntityCache.Instance.Save();
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
    }

    public class  Encounter
    {
        public ulong EncounterId { get; set; }
        public int BattleId { get; set; }
        public uint SceneId { get; set; }
        public string SceneName { get; set; }

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
                }
            }
            else if (key == "AttrLevel")
            {
                entity.SetLevel((int)value);
            }
            else if (key == "AttrSkillId")
            {
                entity.RegisterSkillActivation((int)value);
            }
            else if (key == "AttrState")
            {
                if ((EActorState)value == EActorState.ActorStateDead)
                {
                    entity.IncrementDeaths();
                }
            }
            else if (key == "AttrShieldList")
            {
                var shieldInfo = (ShieldInfo)value;
                //System.Diagnostics.Debug.WriteLine($"SetAttrKV({uuid})::AttrShieldList.ShieldInfo = {shieldInfo}");
                entity.AddBuffEventAttribute((int)shieldInfo.Uuid, "AttrShieldList", shieldInfo);
                //AddShieldGained(uuid, shieldInfo.Uuid, shieldInfo.Value, shieldInfo.InitialValue, shieldInfo.MaxValue);
            }
        }

        public object? GetAttrKV(long uuid, string key)
        {
            return GetOrCreateEntity(uuid).GetAttrKV(key);
        }

        public bool HasStatsBeenRecorded()
        {
            return TotalDamage > 0 || TotalHealing > 0 || TotalTakenDamage > 0 || TotalNpcTakenDamage > 0 || TotalNpcDamage > 0 || TotalNpcShieldBreak > 0 || TotalNpcHealing > 0;
        }

        public void RegisterSkillActivation(long uuid, int skillId)
        {
            var entity = GetOrCreateEntity(uuid);
            entity.RegisterSkillActivation(skillId);
        }

        public void AddDamage(long uuid, int skillId, EDamageProperty damageElement, long damage, bool isCrit, bool isLucky, bool isCauseLucky, long hpLessen = 0, EDamageType? damageType = null, EDamageMode? damageMode = null)
        {
            LastUpdate = DateTime.Now;

            var uuidType = (EEntityType)Utils.UuidToEntityType(uuid);

            if (uuidType == EEntityType.EntMonster)
            {
                TotalNpcDamage += (ulong)damage;
            }
            else
            {
                TotalDamage += (ulong)damage;
            }
            
            if (damageType != null && damageType == EDamageType.Absorbed)
            {
                TotalShieldBreak += (ulong)damage;
            }
            GetOrCreateEntity(uuid).AddDamage(skillId, damage, isCrit, isLucky, hpLessen, damageElement, isCauseLucky, damageType, damageMode);
        }

        public void AddHealing(long uuid, int skillId, EDamageProperty damageElement, long healing, bool isCrit, bool isLucky, bool isCauseLucky, long targetUuid)
        {
            LastUpdate = DateTime.Now;
            TotalHealing += (ulong)healing;

            var entity = GetOrCreateEntity(uuid);

            long? currentHp = entity.GetAttrKV("AttrHp") as long?;
            long? maxHp = entity.GetAttrKV("AttrMaxHp") as long?;

            long overhealing = 0;
            long effectiveHealing = 0;

            if ((currentHp != null && maxHp != null) && (currentHp + healing > maxHp))
            {
                effectiveHealing = (long)(maxHp - currentHp);
                overhealing = healing - effectiveHealing;
            }

            TotalOverhealing += (ulong)overhealing;
            
            entity.AddHealing(skillId, healing, overhealing, isCrit, isLucky, damageElement, isCauseLucky, targetUuid);
        }

        public void AddTakenDamage(long uuid, int skillId, long damage, EDamageSource damageSource, bool isMiss, bool isDead, bool isCrit, bool isLucky, long hpLessen = 0)
        {
            LastUpdate = DateTime.Now;
            TotalTakenDamage += (ulong)damage;
            GetOrCreateEntity(uuid).AddTakenDamage(skillId, damage, isCrit, isLucky, hpLessen, damageSource, isMiss, isDead);
        }

        public void AddNpcTakenDamage(long npcUuid, long attackerUid, int skillId, long damage, bool isCrit, bool isLucky, long hpLessen = 0, bool isMiss = false, bool isDead = false, string? npcName = null)
        {
            LastUpdate = DateTime.Now;
            TotalNpcTakenDamage += (ulong)damage;
            GetOrCreateEntity(npcUuid).AddTakenDamage(skillId, damage, isCrit, isLucky, hpLessen, EDamageSource.Other, isMiss, isDead);
        }

        public void AddShieldGained(long entityUuid, long shieldBuffUuid, long value, long initialValue, long maxValue = 0)
        {
            // Check to make sure the shieldBuffUuid is not already in the shieldGain list, otherwise this is just an update for an existing shield and we're only tracking total gain for now
            //GetOrCreateEntity(entityUuid).AddBuffEventAttribute(shieldBuffUuid, "AttrShieldList", 0);
        }

        public void NotifyBuffEvent(long entityUuid, EBuffEventType buffEventType, int buffUuid, int baseId, int level, long fireUuid, int layer, int duration, int sourceConfigId)
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
        public int MonsterType { get; set; } = -1;

        public Dictionary<string, object> Attributes { get; set; } = new();

        public delegate void SkillActivatedEventHandler(object sender, SkillActivatedEventArgs e);
        public event SkillActivatedEventHandler SkillActivated;

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

                if (AbilityScore == 0 && cached.AblityScore != 0)
                {
                    SetAbilityScore(cached.AblityScore);
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
                cached.AblityScore = abilityScore;
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
            MonsterType = type;
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
            OnSkillActivated(new SkillActivatedEventArgs { SkillId = skillId, ActivationDateTime = DateTime.Now });
        }

        protected virtual void OnSkillActivated(SkillActivatedEventArgs e)
        {
            SkillActivated?.Invoke(this, e);
        }

        public void RegisterSkillData(ESkillType skillType, int skillId, long value, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky, bool isDead = false)
        {
            if (!SkillStats.TryGetValue(skillId, out var stats))
            {
                var combatStats = new CombatStats2();

                combatStats.SetSkillType(skillType);

                if (HelperMethods.DataTables.Skills.Data.TryGetValue(skillId.ToString(), out var skill))
                {
                    combatStats.SetName(skill.Name);
                }

                combatStats.AddData(value, isCrit, isLucky, hpLessenValue, isCauseLucky, isDead);
                SkillStats.TryAdd(skillId, combatStats);
            }
            else
            {
                stats.SetSkillType(skillType);
                stats.AddData(value, isCrit, isLucky, hpLessenValue, isCauseLucky, isDead);
            }
        }

        public void AddDamage(int skillId, long damage, bool isCrit, bool isLucky, long hpLessen = 0, EDamageProperty? damageElement = null, bool isCauseLucky = false, EDamageType? damageType = null, EDamageMode? damageMode = null)
        {
            TotalDamage += (ulong)damage;

            if (damageType != null && damageType == EDamageType.Absorbed)
            {
                TotalShieldBreak += (ulong)damage;
            }

            DamageStats.AddData(damage, isCrit, isLucky, hpLessen, isCauseLucky);

            RegisterSkillData(ESkillType.Damage, skillId, damage, isCrit, isLucky, hpLessen, isCauseLucky);

            //ActionStats.Add(new ActionStat(DateTime.Now, 0, (int)skillId));

            //if (string.IsNullOrEmpty(SubProfession))
            {
                var subProfessionId = Professions.GetSubProfessionIdBySkillId(skillId);

                if (subProfessionId != 0)
                {
                    SetSubProfessionId((int)subProfessionId);
                }
            }

            /*DamageStats.value += (long)damage;
            DamageStats.StartTime ??= DateTime.Now;
            DamageStats.EndTime = DateTime.Now;*/
        }

        public void AddHealing(int skillId, long healing, long overhealing, bool isCrit, bool isLucky, EDamageProperty? damageElement = null, bool isCauseLucky = false, long targetUuid = 0)
        {
            TotalHealing += (ulong)healing;
            TotalOverhealing += (ulong)overhealing;
            HealingStats.AddData(healing, isCrit, isLucky, 0, isCauseLucky);

            RegisterSkillData(ESkillType.Healing, skillId, healing, isCrit, isLucky, overhealing, isCauseLucky);
            //ActionStats.Add(new ActionStat(DateTime.Now, 1, (int)skillId));

            //if (string.IsNullOrEmpty(SubProfession))
            {
                var subProfessionId = Professions.GetSubProfessionIdBySkillId(skillId);

                if (subProfessionId != 0)
                {
                    SetSubProfessionId((int)subProfessionId);
                }
            }

            /*HealingStats.value += (long)healing;
            HealingStats.StartTime ??= DateTime.Now;
            HealingStats.EndTime = DateTime.Now;*/
        }

        public void AddTakenDamage(int skillId, long damage, bool isCrit, bool isLucky, long hpLessen = 0, EDamageSource damageSource = 0, bool isMiss = false, bool isDead = false)
        {
            TotalTakenDamage += (ulong)damage;
            RegisterSkillData(ESkillType.Taken, skillId, damage, isCrit, isLucky, hpLessen, false, isDead);
            /*TakenStats.value += (long)damage;
            TakenStats.StartTime ??= DateTime.Now;
            TakenStats.EndTime = DateTime.Now;*/
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

                if (Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase && BuffEvents.Count > 99)
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

                if (Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase && BuffEvents.Count > 99)
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

            if (Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase && BuffEvents.Count > 99)
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

        public T GetAttrKV<T>(string key) where T : struct
        {
            var value = Attributes.TryGetValue(key, out var val) ? val : null;
            if (typeof(T) == typeof(long))
            {
                //return ((T)(int)value);
            }

            return (T)value;
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

    public class SkillActivatedEventArgs : EventArgs
    {
        public int SkillId { get; set; }
        public DateTime ActivationDateTime { get; set; }
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

        public ulong ValueTotal { get; private set; }
        public ulong ValueNormalTotal { get; private set; }
        public ulong ValueCritTotal { get; private set; }
        public ulong ValueLuckyTotal { get; private set; }
        public ulong ValueCritLuckyTotal { get; private set; }
        public long ValueMax { get; private set; }
        public long ValueMin { get; private set; }
        public double ValueAverage { get; private set; }
        public double ValuePerSecond { get; private set; }

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

        public void AddData(long value, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky, bool isDead = false)
        {
            StartTime ??= DateTime.Now;
            EndTime = DateTime.Now;

            AddValue(value);

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

    public class BuffEvent
    {
        public int BuffType { get; private set; } // Source type of buff 0 = Skill, 1 = Talent, 2 = Special?
        public int BuffPriority { get; private set; }
        public long Uuid { get; private set; }
        public int BaseId { get; private set; } // Buff Id in BuffTable
        public int Level { get; private set; }
        public long FireUuid { get; private set; }
        public string EntityCasterName { get; private set; }
        public int Layer { get; private set; }
        public int Duration { get; private set; }
        public int SourceConfigId { get; private set; } // Original Skill Id
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Icon { get; private set; }
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
                    BuffType = buffTableData.BuffType;
                    BuffPriority = buffTableData.BuffPriority;
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
                }
            }
        }
    }

    public class CombatStats
    {
        public EDamageSource damageSource;
        public bool isMiss;
        public bool isCrit;
        public EDamageType damageType;
        public int type_flag;
        public long value;
        public long actual_value;
        public long lucky_value;
        public long hp_lessen_value;
        public long shield_lessen_value;
        public long attacker_uuid;
        public int owner_id;
        public int owner_level;
        public int owner_stage;
        public int hit_event_id;
        public bool is_normal;
        public bool is_dead;
        public EDamageProperty property;
        public Vector3 damage_pos;
        //...
        public EDamageMode damage_mode;

        public DateTime? StartTime = null;
        public DateTime? EndTime = null;

        public double GetValuePerSecond()
        {
            if (StartTime != null && EndTime != null && StartTime != EndTime)
            {
                var seconds = (EndTime.Value - StartTime.Value).TotalSeconds;
                return seconds > 0 ? Math.Round(value / seconds, 2) : 0;
            }

            return 0;
        }

        public void AddData()
        {

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
