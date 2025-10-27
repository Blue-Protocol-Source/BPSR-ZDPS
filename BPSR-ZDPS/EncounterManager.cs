using BPSR_ZDPS.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS
{
    public static class EncounterManager
    {
        public static List<Encounter> Encounters { get; private set; } = new();
        public static int SelectedEncounter = -1;

        public static Encounter? Current => Encounters.Count > 0 ? Encounters[CurrentEncounter] : null;

        public static int CurrentEncounter = 0;

        static EncounterManager()
        {
            // Give a default encounter for now
            StartEncounter();
        }

        public static void StartEncounter()
        {
            if (Current != null)
            {
                if (Current.EndTime == DateTime.MinValue)
                {
                    // We called StartEncounter without first stopping the current one
                    StopEncounter();
                }
            }

            Encounters.Add(new Encounter());

            CurrentEncounter = Encounters.Count - 1;
        }

        public static void StopEncounter()
        {
            if (Current != null)
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
    }

    public class Encounter
    {
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        private TimeSpan? Duration { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<Entity> Entities { get; set; }

        public ulong TotalDamage { get; set; } = 0;
        public ulong TotalHealing { get; set; } = 0;
        public ulong TotalTakenDamage { get; set; } = 0;
        public ulong TotalNpcTakenDamage { get; set; } = 0;

        public Encounter()
        {
            SetStartTime(DateTime.Now);
            Entities = new();
        }

        public Encounter(DateTime startTime)
        {
            SetStartTime(startTime);
            Entities = new();
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
            if (EndTime == DateTime.MinValue)
            {
                return DateTime.Now.Subtract(StartTime).Duration();
            }
            else
            {
                return (TimeSpan)Duration;
            }
        }

        public Entity GetOrCreateEntity(ulong uid)
        {
            var entity = Entities.FirstOrDefault(x => x.UID == uid);
            if (entity == null)
            {
                entity = new Entity(uid);
                Entities.Add(entity);
            }

            return entity;
        }

        public void SetName(ulong uid, string name)
        {
            GetOrCreateEntity(uid).SetName(name);
        }

        public void SetAbilityScore(ulong uid, int power)
        {
            GetOrCreateEntity(uid).SetAbilityScore(power);
        }

        public void SetProfessionId(ulong uid,  int professionId)
        {
            GetOrCreateEntity(uid).SetProfessionId(professionId);
        }

        public void SetEntityType(ulong uid, EEntityType etype)
        {
            var entity = GetOrCreateEntity(uid);
            entity.EntityType = etype;

            var attr_id = entity.GetAttrKV("AttrId");
            if (attr_id != null && etype == EEntityType.EntMonster)
            {
                if (HelperMethods.DataTables.Monsters.Data.ContainsKey(attr_id.ToString()))
                {
                    entity.SetName(HelperMethods.DataTables.Monsters.Data[attr_id.ToString()].Name);
                }
            }
        }

        public void SetAttrKV(ulong uid, string key, object value)
        {
            var entity = GetOrCreateEntity(uid);
            entity.SetAttrKV(key, value);

            if (key == "AttrId" && entity.EntityType == EEntityType.EntMonster)
            {
                if (HelperMethods.DataTables.Monsters.Data.ContainsKey(value.ToString()))
                {
                    entity.SetName(HelperMethods.DataTables.Monsters.Data[value.ToString()].Name);
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
        }

        public object? GetAttrKV(ulong uid, string key)
        {
            return GetOrCreateEntity(uid).GetAttrKV(key);
        }

        public void RegisterSkillActivation(ulong uid, int skillId)
        {
            var entity = GetOrCreateEntity(uid);
            entity.RegisterSkillActivation(skillId);
        }

        public void AddDamage(ulong uid, int skillId, EDamageProperty damageElement, ulong damage, bool isCrit, bool isLucky, bool isCauseLucky, ulong hpLessen = 0)
        {
            LastUpdate = DateTime.Now;
            TotalDamage += damage;
            GetOrCreateEntity(uid).AddDamage(skillId, damage, isCrit, isLucky, hpLessen, damageElement, isCauseLucky);
        }

        public void AddHealing(ulong uid, int skillId, EDamageProperty damageElement, ulong healing, bool isCrit, bool isLucky, bool isCauseLucky, ulong targetUid)
        {
            LastUpdate = DateTime.Now;
            TotalHealing += healing;
            GetOrCreateEntity(uid).AddHealing(skillId, healing, isCrit, isLucky, damageElement, isCauseLucky, targetUid);
        }

        public void AddTakenDamage(ulong uid, int skillId, ulong damage, EDamageSource damageSource, bool isMiss, bool isDead, bool isCrit, bool isLucky, ulong hpLessen = 0)
        {
            LastUpdate = DateTime.Now;
            TotalTakenDamage += damage;
            GetOrCreateEntity(uid).AddTakenDamage(skillId, damage, isCrit, isLucky, hpLessen, damageSource, isMiss, isDead);
        }

        public void AddNpcTakenDamage(ulong npcId, ulong attackerUid, int skillId, ulong damage, bool isCrit, bool isLucky, ulong hpLessen = 0, bool isMiss = false, bool isDead = false, string? npcName = null)
        {
            LastUpdate = DateTime.Now;
            TotalNpcTakenDamage += damage;
            GetOrCreateEntity(npcId).AddTakenDamage(skillId, damage, isCrit, isLucky, hpLessen, EDamageSource.Other, isMiss, isDead);
        }
    }

    public class Entity
    {
        public ulong UID { get; set; }
        public ulong UIDRaw { get; set; }
        public EEntityType EntityType { get; set; }
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

        public Dictionary<int, CombatStats2> SkillStats { get; set; } = new();
        public List<ActionStat> ActionStats { get; set; } = new();

        public ulong TotalDamage { get; set; } = 0;
        public ulong TotalHealing { get; set; } = 0;
        public ulong TotalTakenDamage { get; set; } = 0;
        public ulong TotalCasts { get; set; } = 0;

        public Dictionary<string, object> Attributes { get; set; } = new();

        public Entity(ulong uid, string name = null)
        {
            UID = uid;
            Name = name;

            var cached = EntityCache.Instance.GetOrCreate(uid);
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
        }

        public void SetName(string name)
        {
            Name = name;

            var cached = EntityCache.Instance.GetOrCreate(UID);
            if (cached != null && !string.IsNullOrEmpty(name))
            {
                cached.Name = name;
            }
        }

        public void SetAbilityScore(int abilityScore)
        {
            AbilityScore = abilityScore;

            var cached = EntityCache.Instance.GetOrCreate(UID);
            if (cached != null && abilityScore != 0)
            {
                cached.AblityScore = abilityScore;
            }
        }

        public void SetProfessionId(int id)
        {
            ProfessionId = id;
            Profession = Professions.GetProfessionNameFromId(id);

            var cached = EntityCache.Instance.GetOrCreate(UID);
            if (cached != null && id != 0)
            {
                cached.ProfessionId = id;
            }
        }

        public void SetSubProfessionId(int id)
        {
            SubProfessionId = id;
            SubProfession = Professions.GetSubProfessionNameFromId(id);

            var cached = EntityCache.Instance.GetOrCreate(UID);
            if (cached != null && id != 0)
            {
                cached.SubProfessionId = id;
            }
        }

        public void SetLevel(int level)
        {
            Level = level;

            var cached = EntityCache.Instance.GetOrCreate(UID);
            if (cached != null && level != 0)
            {
                cached.Level = level;
            }
        }

        public void RegisterSkillActivation(int skillId)
        {
            if (!SkillStats.TryGetValue(skillId, out var stats))
            {
                var combatStats = new CombatStats2();

                if (HelperMethods.DataTables.Skills.Data.ContainsKey(skillId.ToString()))
                {
                    combatStats.SetName(HelperMethods.DataTables.Skills.Data[skillId.ToString()].Name);
                }

                combatStats.RegisterActivation();
                SkillStats.Add(skillId, combatStats);
            }
            else
            {
                stats.RegisterActivation();
            }

            TotalCasts++;
        }

        public void RegisterSkillData(ESkillType skillType, int skillId, long value, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky)
        {
            if (!SkillStats.TryGetValue(skillId, out var stats))
            {
                var combatStats = new CombatStats2();

                combatStats.SetSkillType(skillType);

                if (HelperMethods.DataTables.Skills.Data.ContainsKey(skillId.ToString()))
                {
                    combatStats.SetName(HelperMethods.DataTables.Skills.Data[skillId.ToString()].Name);
                }

                combatStats.AddData(value, isCrit, isLucky, hpLessenValue, isCauseLucky);
                SkillStats.Add(skillId, combatStats);
            }
            else
            {
                stats.SetSkillType(skillType);
                stats.AddData(value, isCrit, isLucky, hpLessenValue, isCauseLucky);
            }
        }

        public void AddDamage(int skillId, ulong damage, bool isCrit, bool isLucky, ulong hpLessen = 0, EDamageProperty? damageElement = null, bool isCauseLucky = false)
        {
            TotalDamage += damage;

            DamageStats.AddData((long)damage, isCrit, isLucky, (long)hpLessen, isCauseLucky);

            RegisterSkillData(ESkillType.Damage, skillId, (long)damage, isCrit, isLucky, (long)hpLessen, isCauseLucky);

            //ActionStats.Add(new ActionStat(DateTime.Now, 0, (int)skillId));

            if (string.IsNullOrEmpty(SubProfession))
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

        public void AddHealing(int skillId, ulong healing, bool isCrit, bool isLucky, EDamageProperty? damageElement = null, bool isCauseLucky = false, ulong targetUid = 0)
        {
            TotalHealing += healing;
            HealingStats.AddData((long)healing, isCrit, isLucky, 0, isCauseLucky);

            RegisterSkillData(ESkillType.Healing, skillId, (long)healing, isCrit, isLucky, 0, isCauseLucky);
            //ActionStats.Add(new ActionStat(DateTime.Now, 1, (int)skillId));

            if (string.IsNullOrEmpty(SubProfession))
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

        public void AddTakenDamage(int skillId, ulong damage, bool isCrit, bool isLucky, ulong hpLessen = 0, EDamageSource damageSource = 0, bool isMiss = false, bool isDead = false)
        {
            TotalTakenDamage += damage;
            RegisterSkillData(ESkillType.Taken, skillId, (long)damage, isCrit, isLucky, (long)hpLessen, false);
            /*TakenStats.value += (long)damage;
            TakenStats.StartTime ??= DateTime.Now;
            TakenStats.EndTime = DateTime.Now;*/
        }

        public void SetAttrKV(string key, object value)
        {
            Attributes[key] = value;
        }

        public object? GetAttrKV(string key)
        {
            return Attributes.TryGetValue(key, out var val) ? val : null;
        }
    }

    public enum ESkillType : int
    {
        Unknown = 0,
        Damage = 1,
        Healing = 2,
        Taken = 3
    }

    public class CombatStats2
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

        public void AddData(long value, bool isCrit, bool isLucky, long hpLessenValue, bool isCauseLucky)
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
