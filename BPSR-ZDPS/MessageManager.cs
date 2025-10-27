using BPSR_DeepsLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Zproto.WorldNtfCsharp.Types;
using Zproto;
using Google.Protobuf.Collections;
using System.Numerics;
using Silk.NET.Core.Native;
using BPSR_ZDPS.DataTypes;

namespace BPSR_ZDPS
{
    public static class MessageManager
    {
        public static NetCap? netCap = null;
        public static string NetCaptureDeviceName = "";

        public static void InitializeCapturing()
        {
            if (NetCaptureDeviceName == null)
            {
                throw new InvalidOperationException();
            }

            netCap = new NetCap();
            netCap.Init(new NetCapConfig()
            {
                CaptureDeviceName = NetCaptureDeviceName // "\\Device\\NPF_{40699DEA-27A5-4985-ADC0-B00BADABAAEB}"
            });

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncContainerData, ProcessSyncContainerData);
            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncContainerDirtyData, ProcessSyncContainerDirtyData);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncNearDeltaInfo, ProcessSyncNearDeltaInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncToMeDeltaInfo, ProcessSyncToMeDeltaInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncNearEntities, ProcessSyncNearEntities);

            netCap.RegisterNotifyHandler(936649811, (uint)BPSR_DeepsLib.ServiceMethods.WorldActivityNtf.SyncHitInfo, ProcessSyncHitInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncDungeonData, ProcessSyncDungeonData);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncDungeonDirtyData, ProcessSyncDungeonDirtyData);

            netCap.Start();
            System.Diagnostics.Debug.WriteLine("MessageManager.InitializeCapturing : Capturing Started...");
        }

        public static void StopCapturing()
        {
            if (netCap != null)
            {
                netCap.Stop();
            }
        }

        public static SharpPcap.LibPcap.LibPcapLiveDevice? TryFindBestNetworkDevice()
        {
            var devices = SharpPcap.LibPcap.LibPcapLiveDeviceList.Instance;

            foreach (var device in devices)
            {
                if (device.Addresses.Count == 0)
                {
                    continue;
                }

                if (device.Interface?.GatewayAddresses.Count == 0)
                {
                    continue;
                }

                if (device.MacAddress == null)
                {
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"Best Network Device = {device.Description} -- {device.Name}");
                return device;
            }

            return null;
        }

        public static void ProcessSyncHitInfo(ReadOnlySpan<byte> payloadBuffer)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessSyncHitInfo");
        }

        public static void ProcessAttrs(ulong uid, RepeatedField<Attr> attrs)
        {
            foreach (var attr in attrs)
            {
                if (attr.Id == 0 || attr.RawData == null || attr.RawData.Length == 0)
                {
                    continue;
                }
                var reader = new Google.Protobuf.CodedInputStream(attr.RawData.ToByteArray());

                switch ((EAttrType)attr.Id)
                {
                    case EAttrType.AttrName:
                        EncounterManager.Current.SetName(uid, reader.ReadString());
                        break;
                    case EAttrType.AttrSkillId:
                        {
                            string attr_name_id = ((EAttrType)attr.Id).ToString();
                            int skillId = reader.ReadInt32();
                            // TODO: Register this skill to the given uid as a Cast
                            // Extra details like damage and such come from the AoiSyncDelta

                            // When SetAttrKV is called with AttrSkillId, it will register for us
                            EncounterManager.Current.SetAttrKV(uid, attr_name_id, skillId);
                            break;
                        }
                    case EAttrType.AttrProfessionId:
                        EncounterManager.Current.SetProfessionId(uid, reader.ReadInt32());
                        break;
                    case EAttrType.AttrFightPoint:
                        EncounterManager.Current.SetAbilityScore(uid, reader.ReadInt32());
                        break;
                    case EAttrType.AttrLevel:
                        EncounterManager.Current.SetAttrKV(uid, "AttrLevel", reader.ReadInt32());
                        break;
                    case EAttrType.AttrRankLevel:
                        EncounterManager.Current.SetAttrKV(uid, "AttrRankLevel", reader.ReadInt32());
                        break;
                    case EAttrType.AttrCri:
                        EncounterManager.Current.SetAttrKV(uid, "AttrCri", reader.ReadInt32());
                        break;
                    case EAttrType.AttrLuck:
                        EncounterManager.Current.SetAttrKV(uid, "AttrLuck", reader.ReadInt32());
                        break;
                    case EAttrType.AttrHp:
                        EncounterManager.Current.SetAttrKV(uid, "AttrHp", reader.ReadInt64());
                        break;
                    case EAttrType.AttrMaxHp:
                        EncounterManager.Current.SetAttrKV(uid, "AttrMaxHp", reader.ReadInt64());
                        break;
                    case EAttrType.AttrAttack:
                        EncounterManager.Current.SetAttrKV(uid, "AttrAttack", reader.ReadInt64());
                        break;
                    case EAttrType.AttrDefense:
                        EncounterManager.Current.SetAttrKV(uid, "AttrDefense", reader.ReadInt64());
                        break;
                    case EAttrType.AttrPos:
                        var pos = Vec3.Parser.ParseFrom(reader);
                        EncounterManager.Current.SetAttrKV(uid, "AttrPos", pos);
                        break;
                    case EAttrType.AttrTargetPos:
                        var target_pos = Vec3.Parser.ParseFrom(reader);
                        EncounterManager.Current.SetAttrKV(uid, "AttrTargetPos", target_pos);
                        break;
                    case EAttrType.AttrState:
                        var entityState = reader.ReadInt32();
                        EActorState state = (EActorState)entityState;
                        EncounterManager.Current.SetAttrKV(uid, "AttrState", state);
                        break;
                    case EAttrType.AttrCombatState:
                    case EAttrType.AttrInBattleShow:
                        if (uid == 285140 && attr.Id == 104 || attr.Id == 186)
                        {
                            System.Diagnostics.Debug.WriteLine($"[YOU] had {(EAttrType)attr.Id} = {reader.ReadInt32()}");
                        }
                        break;
                    default:
                        string attr_name = ((EAttrType)attr.Id).ToString();
                        EncounterManager.Current.SetAttrKV(uid, attr_name, reader.ReadInt32());
                        break;
                }
            }
        }

        public static void ProcessSyncNearEntities(ReadOnlySpan<byte> payloadBuffer)
        {
            var syncNearEntities = SyncNearEntities.Parser.ParseFrom(payloadBuffer);
            if (syncNearEntities.Appear == null || syncNearEntities.Appear.Count == 0)
            {
                return;
            }

            foreach (var entity in syncNearEntities.Appear)
            {
                if (entity.EntType != EEntityType.EntChar)
                {
                    // skil limiting it for now
                    //continue;
                }

                ulong uid = Shr16((ulong)entity.Uuid);

                if (uid == 0)
                {
                    continue;
                }

                EncounterManager.Current.SetEntityType(uid, entity.EntType);

                var attrCollection = entity.Attrs;
                if (attrCollection?.Attrs == null)
                {
                    continue;
                }

                var etype = Utils.RawUuidToEntityType((ulong)entity.Uuid);
                if (etype == EEntityType.EntErrType)
                {
                    System.Diagnostics.Debug.WriteLine($"!!etype == EEntityType.EntErrType!! should have been: {((ulong)entity.Uuid & 0xFFFFUL)} == {entity.EntType.ToString()}");
                }

                switch (entity.EntType)
                {
                    case EEntityType.EntMonster:
                        {
                            ProcessAttrs(uid, attrCollection.Attrs);
                            break;
                        }
                    case EEntityType.EntChar:
                        {
                            ProcessAttrs(uid, attrCollection.Attrs);
                            break;
                        }
                    case EEntityType.EntClientBullet:
                    case EEntityType.EntTrap:
                    case EEntityType.EntStaticObject:
                    case EEntityType.EntDrop:
                    case EEntityType.EntHouseItem:
                    case EEntityType.EntCommunityHouse:
                        break;
                    default:
                        break;
                }
            }
        }

        public static void ProcessSyncNearDeltaInfo(ReadOnlySpan<byte> payloadBuffer)
        {
            var syncNearDeltaInfo = SyncNearDeltaInfo.Parser.ParseFrom(payloadBuffer);
            //Log.Information("Notify: {Hex}", BitConverter.ToString(span.ToArray()));
            if (syncNearDeltaInfo.DeltaInfos == null || syncNearDeltaInfo.DeltaInfos.Count == 0)
            {
                return;
            }

            foreach (var aoiSyncDelta in syncNearDeltaInfo.DeltaInfos)
            {
                ProcessAoiSyncDelta(aoiSyncDelta);
            }
        }

        public static void ProcessAoiSyncDelta(AoiSyncDelta delta)
        {
            if (delta == null)
            {
                return;
            }

            ulong targetUuidRaw = (ulong)delta.Uuid;
            if (targetUuidRaw == 0)
            {
                return;
            }

            bool isTargetPlayer = IsUuidPlayerRaw(targetUuidRaw);
            ulong targetUid = Shr16(targetUuidRaw);
            var attrCollection = delta.Attrs;

            var eType = Utils.RawUuidToEntityType(targetUuidRaw);
            if (EncounterManager.Current.GetOrCreateEntity(targetUid).EntityType == EEntityType.EntErrType)
            {
                EncounterManager.Current.SetEntityType(targetUid, eType);
                if (eType == EEntityType.EntErrType)
                {
                    System.Diagnostics.Debug.WriteLine($"Entity Error Type: rawUuid={targetUuidRaw},res={targetUid}");
                }
            }

            if (attrCollection?.Attrs != null)
            {
                if (isTargetPlayer)
                {
                    // Note: This was previously passing targetUuidRaw in instead of targetUuid which seemed wrong?
                    //EncounterManager.Current.SetEntityType(targetUuid, EEntityType.EntChar);
                    ProcessAttrs(targetUid, attrCollection.Attrs);
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine($"ProcessAoiSyncDelta Uuid={targetUuidRaw},Res={targetUuid}");
                    ProcessAttrs(targetUid, attrCollection.Attrs);
                }
            }

            var skillEffect = delta.SkillEffects;
            if (skillEffect?.Damages == null || skillEffect.Damages.Count == 0)
            {
                return;
            }

            foreach (var d in skillEffect.Damages)
            {
                int skillId = d.OwnerId;
                if (skillId == 0)
                {
                    continue;
                }

                ulong attackerRaw = (ulong)(d.TopSummonerId != 0 ? d.TopSummonerId : d.AttackerUuid);
                if (attackerRaw == 0)
                {
                    continue;
                }
                bool isAttackerPlayer = IsUuidPlayerRaw(attackerRaw);
                ulong attackerUid = Shr16(attackerRaw);

                if (isAttackerPlayer && attackerUid != 0)
                {
                    EncounterManager.Current.SetEntityType(attackerUid, EEntityType.EntChar);
                    var professionId = Professions.GetBaseProfessionIdBySkillId(skillId);
                    if (professionId != 0 && EncounterManager.Current.GetOrCreateEntity(attackerUid).ProfessionId <= 0)
                    {
                        EncounterManager.Current.SetProfessionId(attackerUid, professionId);
                    }
                    // var info = GetPlayerBasicInfo(attackerUid);
                }

                long damageSigned = 0;
                if (d.Value != 0)
                {
                    damageSigned = d.Value;
                }
                else if (d.LuckyValue != 0)
                {
                    damageSigned = d.LuckyValue;
                }
                if (damageSigned == 0)
                {
                    continue;
                }

                ulong damage = (ulong)(damageSigned < 0 ? -damageSigned : damageSigned);

                bool isCrit = d.TypeFlag != null && ((d.TypeFlag & 1) == 1);
                bool isHeal = d.Type == EDamageType.Heal;
                var luckyValue = d.LuckyValue;
                bool isLucky = luckyValue != null && luckyValue != 0;
                ulong hpLessen = 0;
                if (d.HpLessenValue != 0)
                {
                    hpLessen = (ulong)d.HpLessenValue;
                }

                bool isCauseLucky = d.TypeFlag != null && ((d.TypeFlag & 0B100) == 0B100);

                bool isMiss = d.IsMiss;

                bool isDead = d.IsDead;

                string damageElement = d.Property.ToString();

                EDamageSource damageSource = d.DamageSource;


                // TODO: Use SkillId to map a profession to the attacker if they are a player

                if (isTargetPlayer)
                {
                    if (isHeal)
                    {
                        // AddHealing
                        //System.Diagnostics.Debug.WriteLine($"AddHealing({(isAttackerPlayer ? attackerUuid : 0)}, {skillId}, {damageElement}, {hpLessen}, {isCrit}, {isLucky}, {isCauseLucky}, {targetUuid})");
                        EncounterManager.Current.AddHealing((isAttackerPlayer ? attackerUid : 0), skillId, d.Property, hpLessen, isCrit, isLucky, isCauseLucky, targetUid);
                    }
                    else
                    {
                        // AddTakenDamage
                        //System.Diagnostics.Debug.WriteLine($"AddTakenDamage({targetUuid}, {skillId}, {damage}, {damageSource}, {isMiss}, {isDead}, {isCrit}, {hpLessen})");
                        EncounterManager.Current.AddTakenDamage(targetUid, skillId, damage, damageSource, isMiss, isDead, isCrit, isLucky, hpLessen);

                        // This is an NPC applying damage to a target, register the damage dealt now to the NPC doing it
                        EncounterManager.Current.AddDamage(attackerUid, skillId, d.Property, damage, isCrit, isLucky, isCauseLucky, hpLessen);
                    }
                }
                else
                {
                    if (!isHeal && isAttackerPlayer)
                    {
                        // AddDamage
                        //System.Diagnostics.Debug.WriteLine($"AddDamage({attackerUuid}, {skillId}, {damageElement}, {damage}, {isCrit}, {isLucky}, {isCauseLucky}, {hpLessen})");
                        EncounterManager.Current.AddDamage(attackerUid, skillId, d.Property, damage, isCrit, isLucky, isCauseLucky, hpLessen);
                    }

                    // AddNpcTakenDamage
                    //System.Diagnostics.Debug.WriteLine($"AddNpcTakenDamage({targetUuid}, {attackerUuid}, {skillId}, {damage}, {isCrit}, {isLucky}, {hpLessen}, {isMiss}, {isDead})");
                    EncounterManager.Current.AddNpcTakenDamage(targetUid, attackerUid, skillId, damage, isCrit, isLucky, hpLessen, isMiss, isDead);
                }
            }
        }

        public static long currentUserUuid = 0;

        public static void ProcessSyncToMeDeltaInfo(ReadOnlySpan<byte> payloadBuffer)
        {
            var syncToMeDeltaInfo = SyncToMeDeltaInfo.Parser.ParseFrom(payloadBuffer);
            var aoiSyncToMeDelta = syncToMeDeltaInfo.DeltaInfo;
            long uuid = aoiSyncToMeDelta.Uuid;
            if (uuid != 0 && currentUserUuid != uuid)
            {
                currentUserUuid = uuid;
                AppState.PlayerUUID = uuid;
                AppState.PlayerUID = (long)Shr16((ulong)uuid);
            }
            var aoiSyncDelta = aoiSyncToMeDelta.BaseDelta;
            if (aoiSyncDelta == null)
            {
                return;
            }
            ProcessAoiSyncDelta(aoiSyncDelta);
        }

        public static void ProcessSyncContainerData(ReadOnlySpan<byte> payloadBuffer)
        {
            // This might only occur on map change and comes from the current player, no one else
            // Teleports do not trigger this
            // As this occurs the moment a load actually begins, many states are likely not going to be set yet
            // This mainly is how the current local player will get their own data though


            // We'll spin up a new encounter before processing any of this data so it's nice and fresh in the new encounter
            EncounterManager.StartEncounter();

            var syncContainerData = SyncContainerData.Parser.ParseFrom(payloadBuffer);
            if (syncContainerData?.VData == null)
            {
                return;
            }

            var vData = syncContainerData.VData;
            if (vData.CharId == null || vData.CharId == 0)
            {
                return;
            }

            AppState.PlayerUID = vData.CharId;
            ulong playerUid = (ulong)vData.CharId;

            if (vData.RoleLevel?.Level != 0)
            {
                EncounterManager.Current.SetAttrKV(playerUid, "AttrLevel", vData.RoleLevel.Level);
            }

            if (vData.Attr?.CurHp != 0)
            {
                EncounterManager.Current.SetAttrKV(playerUid, "AttrHp", vData.Attr.CurHp);
            }

            if (vData.Attr?.MaxHp != 0)
            {
                EncounterManager.Current.SetAttrKV(playerUid, "AttrMaxHp", vData.Attr.MaxHp);
            }

            if (vData.CharBase != null)
            {
                if (!string.IsNullOrEmpty(vData.CharBase.Name))
                {
                    EncounterManager.Current.SetName(playerUid, vData.CharBase.Name);
                    AppState.PlayerName = vData.CharBase.Name;
                }

                if (vData.CharBase.FightPoint != 0)
                {
                    EncounterManager.Current.SetAbilityScore(playerUid, vData.CharBase.FightPoint);
                }
            }

            var professionList = vData.ProfessionList;
            if (professionList != null && professionList.CurProfessionId != 0)
            {
                var professionName = Professions.GetProfessionNameFromId(professionList.CurProfessionId);
                EncounterManager.Current.SetProfessionId(playerUid, professionList.CurProfessionId);
                AppState.ProfessionId = professionList.CurProfessionId;
                AppState.ProfessionName = professionName;
            }

            var sceneData = vData.SceneData;
            if (sceneData != null)
            {

            }

            if (vData.Equip != null)
            {
                foreach (var equip in vData.Equip.EquipList_)
                {
                    System.Diagnostics.Debug.WriteLine($"{playerUid} :: equip::slot={equip.Value.EquipSlot},refinelvl={equip.Value.EquipSlotRefineLevel}");
                }
            }
        }

        public static void ProcessSyncContainerDirtyData(ReadOnlySpan<byte> payloadBuffer)
        {
            try
            {
                if (currentUserUuid == 0)
                {
                    return;
                }
                var dirty = SyncContainerDirtyData.Parser.ParseFrom(payloadBuffer);
                if (dirty?.VData?.Buffer == null || dirty.VData.Buffer.Length == 0)
                {
                    return;
                }

                var buf = dirty.VData.Buffer.ToByteArray();

                using var ms = new MemoryStream(buf, writable: false);
                using var br = new BinaryReader(ms);

                if (!DoesStreamHaveIdentifier(br))
                {
                    return;
                }

                uint fieldIndex = br.ReadUInt32();
                _ = br.ReadInt32();

                ulong playerUid = (ulong)currentUserUuid >> 16;

                switch (fieldIndex)
                {
                    case CharSerialize.CharBaseFieldNumber:
                        {
                            if (!DoesStreamHaveIdentifier(br))
                            {
                                break;
                            }
                            uint subFieldIndex = br.ReadUInt32();
                            _ = br.ReadInt32();
                            switch (subFieldIndex)
                            {
                                case CharBaseInfo.NameFieldNumber:
                                    {
                                        string playerName = StreamReadString(br);
                                        if (!string.IsNullOrEmpty(playerName))
                                        {
                                            EncounterManager.Current.SetName(playerUid, playerName);
                                            AppState.PlayerName = playerName;
                                        }
                                        break;
                                    }
                                case CharBaseInfo.PersonalStateFieldNumber:
                                    {
                                        int count = br.ReadInt32();

                                        List<int> personal_state = new();

                                        for (int i = 0; i < count; i++)
                                        {
                                            var x = br.ReadInt32();
                                            personal_state.Add(x);
                                        }

                                        break;
                                    }
                                case CharBaseInfo.FightPointFieldNumber:
                                    {
                                        uint fightPoint = br.ReadUInt32();
                                        _ = br.ReadInt32();
                                        if (fightPoint != 0)
                                        {
                                            EncounterManager.Current.SetAbilityScore(playerUid, (int)fightPoint);
                                        }

                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }

                            break;
                        }
                    case CharSerialize.AttrFieldNumber:
                        {
                            if (!DoesStreamHaveIdentifier(br))
                            {
                                break;
                            }
                            uint subFieldIndex = br.ReadUInt32();
                            _ = br.ReadInt32();
                            switch (subFieldIndex)
                            {
                                case UserFightAttr.CurHpFieldNumber:
                                    {
                                        long curHp = br.ReadInt64();
                                        EncounterManager.Current.SetAttrKV(playerUid, "AttrHp", curHp);
                                        break;
                                    }
                                case UserFightAttr.MaxHpFieldNumber:
                                    {
                                        long maxHp = br.ReadInt64();
                                        EncounterManager.Current.SetAttrKV(playerUid, "AttrMaxHp", maxHp);
                                        break;
                                    }
                                case UserFightAttr.OriginEnergyFieldNumber:
                                    {
                                        float origin_energy = br.ReadSingle();
                                        break;
                                    }
                                case UserFightAttr.IsDeadFieldNumber:
                                    {
                                        int is_dead = br.ReadInt32();
                                        break;
                                    }
                                case UserFightAttr.DeadTimeFieldNumber:
                                    {
                                        long dead_time = br.ReadInt64();
                                        break;
                                    }
                                case UserFightAttr.ReviveIdFieldNumber:
                                    {
                                        int revive_id = br.ReadInt32();
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }

                            break;
                        }
                    case CharSerialize.ProfessionListFieldNumber:
                        {
                            if (!DoesStreamHaveIdentifier(br))
                            {
                                break;
                            }
                            uint subFieldIndex = br.ReadUInt32();
                            _ = br.ReadInt32();
                            switch (subFieldIndex)
                            {
                                case ProfessionList.CurProfessionIdFieldNumber:
                                    {
                                        uint curProfessionId = br.ReadUInt32();
                                        _ = br.ReadInt32();
                                        if (curProfessionId != 0)
                                        {
                                            EncounterManager.Current.SetProfessionId(playerUid, (int)curProfessionId);
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public static void ProcessSyncDungeonData(ReadOnlySpan<byte> payloadBuffer)
        {
            // This might only occur on map change and comes from the current player, no one else
            // Teleports do not trigger this
            // Generally the dungeon has not begun at this point, it's likely not even in the Ready state
            var syncDungeonData = SyncDungeonData.Parser.ParseFrom(payloadBuffer);
            if (syncDungeonData?.VData == null)
            {
                return;
            }

            var vData = syncDungeonData.VData;

            for(int listIdx = 0; listIdx < vData.Title.TitleList.Count; listIdx++)
            {
                var title_list = vData.Title.TitleList[listIdx];
                for (int infoIdx = 0; infoIdx < title_list.TitleInfo.Count; infoIdx++)
                {
                    var title_info = title_list.TitleInfo[infoIdx];
                    System.Diagnostics.Debug.WriteLine($"TitleList[{listIdx}].TitleInfo[{infoIdx}]: Uuid={title_info.Uuid},TitleId{title_info.TitleId}");
                }
            }

            foreach (var targetData in vData.Target.TargetData)
            {
                System.Diagnostics.Debug.WriteLine($"Target.TargetData[{targetData.Key}]: TargetId={targetData.Value.TargetId},Nums={targetData.Value.Nums},Complete={targetData.Value.Complete}");
            }

            foreach (var damage in vData.Damage.Damages)
            {
                System.Diagnostics.Debug.WriteLine($"Damage.Damages[{damage.Key}]: {damage.Value}");
            }

            System.Diagnostics.Debug.WriteLine($"syncDungeonData.vData State={vData.FlowInfo.State},TotalScore={vData.DungeonScore.TotalScore},CurRatio={vData.DungeonScore.CurRatio}");
        }

        public static void ProcessSyncDungeonDirtyData(ReadOnlySpan<byte> payloadBuffer)
        {
            var dirty = SyncDungeonDirtyData.Parser.ParseFrom(payloadBuffer);
            if (dirty?.VData?.Buffer == null || dirty.VData.Buffer.Length == 0)
            {
                return;
            }

            var buf = dirty.VData.Buffer.ToByteArray();

            //var reader = new Google.Protobuf.CodedInputStream(buf);
            //var dungeonSyncData = DungeonSyncData.Parser.ParseFrom(reader);
            //System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.vData TotalScore={dungeonSyncData.DungeonScore.TotalScore},CurRatio={dungeonSyncData.DungeonScore.CurRatio}");

            using var ms = new MemoryStream(buf, writable: false);
            using var br = new BinaryReader(ms);

            if (!DoesStreamHaveIdentifier(br))
            {
                return;
            }

            uint fieldIndex = br.ReadUInt32();
            _ = br.ReadInt32();

            System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData fieldIndex={fieldIndex}");

            switch (fieldIndex)
            {
                case DungeonSyncData.SceneUuidFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.FlowInfoFieldNumber:
                    {
                        if (!DoesStreamHaveIdentifier(br))
                        {
                            break;
                        }
                        uint subFieldIndex = br.ReadUInt32();
                        _ = br.ReadInt32();

                        switch (subFieldIndex)
                        {
                            case DungeonFlowInfo.StateFieldNumber:
                                {
                                    var state = br.ReadInt32();
                                    EDungeonState dungeonState = (EDungeonState)state;
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.State == {dungeonState}");

                                    if (dungeonState == EDungeonState.DungeonStateEnd)
                                    {
                                        // Encounter has ended
                                    }
                                    else if (dungeonState == EDungeonState.DungeonStateReady)
                                    {
                                        // Encounter is in prep phase
                                    }
                                    else if (dungeonState == EDungeonState.DungeonStatePlaying)
                                    {
                                        // Encounter has begun
                                        EncounterManager.StopEncounter();
                                        EncounterManager.StartEncounter();
                                    }

                                    break;
                                }
                            case DungeonFlowInfo.ActiveTimeFieldNumber:
                                {
                                    var active_time = br.ReadInt32();
                                    break;
                                }
                            case DungeonFlowInfo.ReadyTimeFieldNumber:
                                {
                                    var ready_time = br.ReadInt32();
                                    break;
                                }
                            case DungeonFlowInfo.PlayTimeFieldNumber:
                                {
                                    var play_time = br.ReadInt32();
                                    break;
                                }
                            case DungeonFlowInfo.EndTimeFieldNumber:
                                {
                                    var endTime = br.ReadInt32();
                                    break;
                                }
                            case DungeonFlowInfo.SettlementTimeFieldNumber:
                                {
                                    var settlement_time = br.ReadInt32();
                                    break;
                                }
                            case DungeonFlowInfo.DungeonTimesFieldNumber:
                                {
                                    var dungeon_times = br.ReadInt32();
                                    break;
                                }
                            case DungeonFlowInfo.ResultFieldNumber:
                                {
                                    var result = br.ReadInt32();
                                    break;
                                }
                            default:
                                break;
                        }
                        
                        break;
                    }
                case DungeonSyncData.TitleFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.TargetFieldNumber:
                    {
                        // int32: target_id, int32 nums, int32 complete
                        break;
                    }
                case DungeonSyncData.DamageFieldNumber:
                    {
                        // HashMap<int64, int64>: damages
                        break;
                    }
                case DungeonSyncData.VoteFieldNumber:
                    {
                        // HashMap<int64, int32>: vote
                        break;
                    }
                case DungeonSyncData.SettlementFieldNumber:
                    {
                        if (!DoesStreamHaveIdentifier(br))
                        {
                            break;
                        }
                        uint subFieldIndex = br.ReadUInt32();
                        _ = br.ReadInt32();

                        switch (subFieldIndex)
                        {
                            case DungeonSettlement.PassTimeFieldNumber:
                                {

                                    break;
                                }
                            case DungeonSettlement.AwardFieldNumber:
                                {

                                    break;
                                }
                            case DungeonSettlement.SettlementPosFieldNumber:
                                {

                                    break;
                                }
                            case DungeonSettlement.WorldBossSettlementFieldNumber:
                                {

                                    break;
                                }
                            case DungeonSettlement.MasterModeScoreFieldNumber:
                                {

                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                case DungeonSyncData.DungeonPioneerFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.PlanetRoomInfoFieldNumber:
                    {
                        
                        break;
                    }
                case DungeonSyncData.DungeonVarFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonRankFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonAffixDataFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonEventFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonScoreFieldNumber:
                    {
                        if (!DoesStreamHaveIdentifier(br))
                        {
                            break;
                        }
                        uint subFieldIndex = br.ReadUInt32();
                        _ = br.ReadInt32();
                        switch (subFieldIndex)
                        {
                            case DungeonScore.TotalScoreFieldNumber:
                                var totalScore = br.ReadInt32();
                                System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.vData.. TotalScore={totalScore}");
                                break;
                            case DungeonScore.CurRatioFieldNumber:
                                var curRatio = br.ReadInt32();
                                System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.vData.. CurRatio={curRatio}");
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case DungeonSyncData.TimerInfoFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.HeroKeyFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonUnionInfoFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonPlayerListFieldNumber:
                    {
                        if (!DoesStreamHaveIdentifier(br))
                        {
                            break;
                        }
                        uint subFieldIndex = br.ReadUInt32();
                        _ = br.ReadInt32();
                        switch (subFieldIndex)
                        {
                            case DungeonPlayerList.PlayerInfosFieldNumber:
                                {
                                    var count = br.ReadUInt32();
                                    for (int i = 0; i < count; i++)
                                    {

                                    }
                                    // HashMap<u32, DungeonPlayerInfo>
                                    // DungeonPlayerInfo: char_id = int64, social_data = obj
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                case DungeonSyncData.ReviveInfoFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.RandomEntityConfigIdInfoFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonSceneInfoFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonVarAllFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.DungeonRaidInfoFieldNumber:
                    {

                        break;
                    }
                case DungeonSyncData.ErrCodeFieldNumber:
                    {

                        break;
                    }
                default:
                    break;
            }
        }

        static bool DoesStreamHaveIdentifier(BinaryReader br)
        {
            var s = br.BaseStream;

            if (s.Position + 8 > s.Length)
            {
                return false;
            }

            uint id1 = br.ReadUInt32();
            int guard1 = br.ReadInt32();

            if (id1 != 0xFFFFFFFE)
            {
                return false;
            }

            if (s.Position + 8 > s.Length)
            {
                return false;
            }

            int id2 = br.ReadInt32();
            int guard2 = br.ReadInt32();

            return true;
        }

        static string StreamReadString(BinaryReader br)
        {
            uint length = br.ReadUInt32();
            _ = br.ReadInt32();

            byte[] bytes = length > 0 ? br.ReadBytes((int)length) : Array.Empty<byte>();

            _ = br.ReadInt32();

            return bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsUuidPlayerRaw(ulong uuidRaw) => (uuidRaw & 0xFFFFUL) == 640UL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong Shr16(ulong v) => v >> 16;
    }
}
