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
using static HexaGen.Runtime.MemoryPool;

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
                CaptureDeviceName = Settings.Instance.NetCaptureDeviceName // "\\Device\\NPF_{40699DEA-27A5-4985-ADC0-B00BADABAAEB}"
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

        public static void ProcessAttrs(long uuid, RepeatedField<Attr> attrs)
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
                        EncounterManager.Current.SetName(uuid, reader.ReadString());
                        break;
                    case EAttrType.AttrSkillId:
                        {
                            string attr_name_id = ((EAttrType)attr.Id).ToString();
                            int skillId = reader.ReadInt32();
                            // TODO: Register this skill to the given uid as a Cast
                            // Extra details like damage and such come from the AoiSyncDelta

                            // When SetAttrKV is called with AttrSkillId, it will register for us
                            EncounterManager.Current.SetAttrKV(uuid, attr_name_id, skillId);
                            break;
                        }
                    case EAttrType.AttrProfessionId:
                        EncounterManager.Current.SetProfessionId(uuid, reader.ReadInt32());
                        break;
                    case EAttrType.AttrFightPoint:
                        EncounterManager.Current.SetAbilityScore(uuid, reader.ReadInt32());
                        break;
                    case EAttrType.AttrLevel:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrLevel", reader.ReadInt32());
                        break;
                    case EAttrType.AttrRankLevel:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrRankLevel", reader.ReadInt32());
                        break;
                    case EAttrType.AttrCri:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrCri", reader.ReadInt32());
                        break;
                    case EAttrType.AttrLuck:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrLuck", reader.ReadInt32());
                        break;
                    case EAttrType.AttrHp:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrHp", reader.ReadInt64());
                        break;
                    case EAttrType.AttrMaxHp:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrMaxHp", reader.ReadInt64());
                        break;
                    case EAttrType.AttrAttack:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrAttack", reader.ReadInt64());
                        break;
                    case EAttrType.AttrDefense:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrDefense", reader.ReadInt64());
                        break;
                    case EAttrType.AttrPos:
                        var pos = Vec3.Parser.ParseFrom(reader);
                        EncounterManager.Current.SetAttrKV(uuid, "AttrPos", pos);
                        break;
                    case EAttrType.AttrTargetPos:
                        var target_pos = Vec3.Parser.ParseFrom(reader);
                        EncounterManager.Current.SetAttrKV(uuid, "AttrTargetPos", target_pos);
                        break;
                    case EAttrType.AttrState:
                        var entityState = reader.ReadInt32();
                        EActorState state = (EActorState)entityState;
                        EncounterManager.Current.SetAttrKV(uuid, "AttrState", state);
                        break;
                    case EAttrType.AttrCombatState:
                    case EAttrType.AttrInBattleShow:
                        if (uuid == 285140 && attr.Id == 104 || attr.Id == 186)
                        {
                            System.Diagnostics.Debug.WriteLine($"[YOU] had {(EAttrType)attr.Id} = {reader.ReadInt32()}");
                        }
                        break;
                    case EAttrType.AttrShieldList:
                        {
                            //System.Diagnostics.Debug.WriteLine($"AttrShieldList");
                            while (!reader.IsAtEnd)
                            {
                                uint tag = reader.ReadTag();
                                int fieldNumber = Google.Protobuf.WireFormat.GetTagFieldNumber(tag);
                                Google.Protobuf.WireFormat.WireType wireType = Google.Protobuf.WireFormat.GetTagWireType(tag);

                                if (wireType != Google.Protobuf.WireFormat.WireType.LengthDelimited)
                                {
                                    reader.SkipLastField();
                                    continue;
                                }

                                int len = reader.ReadLength();
                                //System.Diagnostics.Debug.WriteLine($"len={len}");
                                var inf = ShieldInfo.Parser.ParseFrom(reader);
                                //System.Diagnostics.Debug.WriteLine($"uuid={inf.Uuid}, type={inf.ShieldType}, value={inf.Value}, initialvalue={inf.InitialValue}, maxvalue={inf.MaxValue}");

                                EncounterManager.Current.SetAttrKV(uuid, "AttrShieldList", inf);
                            }

                            //var count = reader.ReadInt32();
                            //System.Diagnostics.Debug.WriteLine($"AttrShieldList.count={count}");

                            /*var uuid = reader.ReadInt64();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].uuid={uuid}");
                            var type = reader.ReadInt32();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].type={type}");
                            var value = reader.ReadInt64();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].value={value}");
                            var InitialValue = reader.ReadInt64();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].InitialValue={InitialValue}");
                            var MaxValue = reader.ReadInt64();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].MaxValue={MaxValue}");

                            // We got some bytes left over still for some reason
                            var unk1 = reader.ReadInt32();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].unk1={unk1}");
                            var unk2 = reader.ReadInt32();
                            System.Diagnostics.Debug.WriteLine($"shieldInfo[].unk2={unk2}");*/
                            break;
                        }
                    default:
                        string attr_name = ((EAttrType)attr.Id).ToString();
                        EncounterManager.Current.SetAttrKV(uuid, attr_name, reader.ReadInt32());
                        //System.Diagnostics.Debug.WriteLine($"{attr_name} was hit");
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

                long uid = Shr16(entity.Uuid);

                if (uid == 0)
                {
                    continue;
                }

                EncounterManager.Current.SetEntityType(entity.Uuid, entity.EntType);

                var attrCollection = entity.Attrs;
                if (attrCollection?.Attrs == null)
                {
                    continue;
                }

                var etype = Utils.RawUuidToEntityType(entity.Uuid);
                if (etype == EEntityType.EntErrType)
                {
                    System.Diagnostics.Debug.WriteLine($"!!etype == EEntityType.EntErrType!! should have been: {((ulong)entity.Uuid & 0xFFFFUL)} == {entity.EntType.ToString()}");
                }

                switch (entity.EntType)
                {
                    case EEntityType.EntMonster:
                        {
                            ProcessAttrs(entity.Uuid, attrCollection.Attrs);
                            break;
                        }
                    case EEntityType.EntChar:
                        {
                            ProcessAttrs(entity.Uuid, attrCollection.Attrs);
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

            long targetUuid = delta.Uuid;
            if (targetUuid == 0)
            {
                return;
            }

            bool isTargetPlayer = IsUuidPlayerRaw(targetUuid);
            long targetUid = Shr16(targetUuid);
            var attrCollection = delta.Attrs;

            var eType = Utils.RawUuidToEntityType(targetUuid);
            if (EncounterManager.Current.GetOrCreateEntity(targetUuid).EntityType == EEntityType.EntErrType)
            {
                EncounterManager.Current.SetEntityType(targetUuid, eType);
                if (eType == EEntityType.EntErrType)
                {
                    System.Diagnostics.Debug.WriteLine($"Entity Error Type: rawUuid={targetUuid},res={targetUid}");
                }
            }

            if (attrCollection?.Attrs != null && attrCollection.Attrs.Any())
            {
                if (isTargetPlayer)
                {
                    // Note: This was previously passing targetUuidRaw in instead of targetUuid which seemed wrong?
                    //EncounterManager.Current.SetEntityType(targetUuid, EEntityType.EntChar);
                    ProcessAttrs(targetUuid, attrCollection.Attrs);
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine($"ProcessAoiSyncDelta Uuid={targetUuidRaw},Res={targetUuid}");
                    ProcessAttrs(targetUuid, attrCollection.Attrs);
                }
            }

            if (delta.TempAttrs != null && delta.TempAttrs.Attrs.Any())
            {
                //System.Diagnostics.Debug.WriteLine($"delta.TempAttrs.Attrs.count = {delta.TempAttrs.Attrs.Count}");
            }

            if (delta.BuffEffect != null && (targetUuid == 285140 || targetUuid == 29288969))
            {
                //System.Diagnostics.Debug.WriteLine($"delta.BuffEffect={delta.BuffEffect.BuffEffects.Count}");
            }

            if (delta.BuffInfos != null && targetUuid == 285140)
            {
                //System.Diagnostics.Debug.WriteLine($"delta.BuffInfos={delta.BuffInfos.BuffInfos.Count}");
            }

            var skillEffect = delta.SkillEffects;
            if (skillEffect != null)
            {
                //System.Diagnostics.Debug.WriteLine($"skillEffect({skillEffect.Damages.Count})={skillEffect}");
            }
            
            if (skillEffect?.Damages == null || skillEffect.Damages.Count == 0)
            {
                return;
            }

            foreach (var d in skillEffect.Damages)
            {
                // OwnerId = SkillId this is coming from
                // OwnerLevel = Level of the Skill this came from
                // TopSummonerId = If exists, Entity UUID that summoned this skill damage source (ex: Battle Imagines use this)
                // Property = Element of the damage if it has any
                // DamageMode = Physical or Magical damage type

                // HpLessen = Actual Health modification from this skill
                // Value = Requested Health modification
                // IsDead = Skill killed an entity with this damage event
                // If IsDead is true, and Value > HpLessen, the difference is the Overkill amount

                int skillId = d.OwnerId;
                if (skillId == 0)
                {
                    continue;
                }

                long attackerUuid = (d.TopSummonerId != 0 ? d.TopSummonerId : d.AttackerUuid);
                if (attackerUuid == 0)
                {
                    continue;
                }
                bool isAttackerPlayer = IsUuidPlayerRaw(attackerUuid);
                long attackerUid = Shr16(attackerUuid);

                if (isAttackerPlayer && attackerUuid != 0)
                {
                    EncounterManager.Current.SetEntityType(attackerUuid, EEntityType.EntChar);
                    var professionId = Professions.GetBaseProfessionIdBySkillId(skillId);
                    if (professionId != 0 && EncounterManager.Current.GetOrCreateEntity(attackerUuid).ProfessionId <= 0)
                    {
                        EncounterManager.Current.SetProfessionId(attackerUuid, professionId);
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

                long damage = (damageSigned < 0 ? -damageSigned : damageSigned);

                bool isCrit = d.TypeFlag != null && ((d.TypeFlag & 1) == 1);
                bool isHeal = d.Type == EDamageType.Heal;
                var luckyValue = d.LuckyValue;
                bool isLucky = luckyValue != null && luckyValue != 0;
                long hpLessen = 0;
                if (d.HpLessenValue != 0)
                {
                    hpLessen = d.HpLessenValue;
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
                        EncounterManager.Current.AddHealing((isAttackerPlayer ? attackerUuid : 0), skillId, d.Property, hpLessen, isCrit, isLucky, isCauseLucky, targetUuid);
                    }
                    else
                    {
                        // AddTakenDamage
                        //System.Diagnostics.Debug.WriteLine($"AddTakenDamage({targetUuid}, {skillId}, {damage}, {damageSource}, {isMiss}, {isDead}, {isCrit}, {hpLessen})");
                        EncounterManager.Current.AddTakenDamage(targetUuid, skillId, damage, damageSource, isMiss, isDead, isCrit, isLucky, hpLessen);

                        // This is an NPC applying damage to a target, register the damage dealt now to the NPC doing it
                        EncounterManager.Current.AddDamage(attackerUuid, skillId, d.Property, damage, isCrit, isLucky, isCauseLucky, hpLessen, d.Type, d.DamageMode);
                    }
                }
                else
                {
                    if (!isHeal && isAttackerPlayer)
                    {
                        // AddDamage
                        //System.Diagnostics.Debug.WriteLine($"AddDamage({attackerUuid}, {skillId}, {damageElement}, {damage}, {isCrit}, {isLucky}, {isCauseLucky}, {hpLessen})");
                        EncounterManager.Current.AddDamage(attackerUuid, skillId, d.Property, damage, isCrit, isLucky, isCauseLucky, hpLessen, d.Type, d.DamageMode);
                    }

                    // AddNpcTakenDamage
                    //System.Diagnostics.Debug.WriteLine($"AddNpcTakenDamage({targetUuid}, {attackerUuid}, {skillId}, {damage}, {isCrit}, {isLucky}, {hpLessen}, {isMiss}, {isDead})");
                    EncounterManager.Current.AddNpcTakenDamage(targetUuid, attackerUuid, skillId, damage, isCrit, isLucky, hpLessen, isMiss, isDead);
                }
            }
        }

        public static long currentUserUuid = 0;
        public static CharSerialize? cachedSyncContainerData = null;

        public static void ProcessSyncToMeDeltaInfo(ReadOnlySpan<byte> payloadBuffer)
        {
            var syncToMeDeltaInfo = SyncToMeDeltaInfo.Parser.ParseFrom(payloadBuffer);
            var aoiSyncToMeDelta = syncToMeDeltaInfo.DeltaInfo;
            long uuid = aoiSyncToMeDelta.Uuid;
            if (uuid != 0 && currentUserUuid != uuid)
            {
                currentUserUuid = uuid;
                AppState.PlayerUUID = uuid;
                AppState.PlayerUID = Shr16(uuid);
                ProcessCachedSyncContainerData();
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
            EncounterManager.StartNewBattle();
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

            if (currentUserUuid != 0)
            {
                ProcessCachedSyncContainerData(vData);
            }
            else
            {
                // We haven't identified our own UUID yet, store this for the next event
                cachedSyncContainerData = vData;
            }

            return;

            AppState.PlayerUID = vData.CharId;
            long playerUid = vData.CharId;

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

        public static void ProcessCachedSyncContainerData(CharSerialize? syncContainerData = null)
        {
            if (currentUserUuid != 0 && (cachedSyncContainerData != null || syncContainerData != null))
            {
                long playerUid = Shr16(currentUserUuid);

                CharSerialize? vData = cachedSyncContainerData ?? syncContainerData;

                if (vData.RoleLevel?.Level != 0)
                {
                    EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrLevel", vData.RoleLevel.Level);
                }

                if (vData.Attr?.CurHp != 0)
                {
                    EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrHp", vData.Attr.CurHp);
                }

                if (vData.Attr?.MaxHp != 0)
                {
                    EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrMaxHp", vData.Attr.MaxHp);
                }

                if (vData.CharBase != null)
                {
                    if (!string.IsNullOrEmpty(vData.CharBase.Name))
                    {
                        EncounterManager.Current.SetName(currentUserUuid, vData.CharBase.Name);
                        AppState.PlayerName = vData.CharBase.Name;
                    }

                    if (vData.CharBase.FightPoint != 0)
                    {
                        EncounterManager.Current.SetAbilityScore(currentUserUuid, vData.CharBase.FightPoint);
                    }
                }

                var professionList = vData.ProfessionList;
                if (professionList != null && professionList.CurProfessionId != 0)
                {
                    var professionName = Professions.GetProfessionNameFromId(professionList.CurProfessionId);
                    EncounterManager.Current.SetProfessionId(currentUserUuid, professionList.CurProfessionId);
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
                        System.Diagnostics.Debug.WriteLine($"{currentUserUuid} :: equip::slot={equip.Value.EquipSlot},refinelvl={equip.Value.EquipSlotRefineLevel}");
                    }
                }

                // Release the memory this held (typically is ~140KB)
                cachedSyncContainerData = null;
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

                long playerUid = currentUserUuid >> 16;

                // Ensure we've already processed our own full sync first
                ProcessCachedSyncContainerData();

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
                                            EncounterManager.Current.SetName(currentUserUuid, playerName);
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
                                            EncounterManager.Current.SetAbilityScore(currentUserUuid, (int)fightPoint);
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
                                        EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrHp", curHp);
                                        break;
                                    }
                                case UserFightAttr.MaxHpFieldNumber:
                                    {
                                        long maxHp = br.ReadInt64();
                                        EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrMaxHp", maxHp);
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
                                            EncounterManager.Current.SetProfessionId(currentUserUuid, (int)curProfessionId);
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
            // Make sure our own character container was processed if possible first
            ProcessCachedSyncContainerData();

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

            System.Diagnostics.Debug.WriteLine("ProcessSyncDungeonDirtyData");

            var dataFuncs = new Dictionary<int, Action<BinaryReader>>()
            {
                {
                    DungeonSyncData.SceneUuidFieldNumber, dungeonSyncData =>
                    {
                        dungeonSyncData.ReadInt32();
                        dungeonSyncData.ReadInt32();
                    }
                },
                {
                    DungeonSyncData.FlowInfoFieldNumber, dungeonSyncData =>
                    {
                        var flowInfoDataFuncs = new Dictionary<int, Action<BinaryReader>>()
                        {
                            {
                                DungeonFlowInfo.StateFieldNumber, dungeonFlowInfo =>
                                {
                                    var state = dungeonFlowInfo.ReadInt32();
                                    _ = dungeonFlowInfo.ReadInt32();

                                    EDungeonState dungeonState = (EDungeonState)state;
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.State == {dungeonState}");

                                    if (dungeonState == EDungeonState.DungeonStateEnd)
                                    {
                                        // Encounter has ended
                                        EncounterManager.StopEncounter();
                                    }
                                    else if (dungeonState == EDungeonState.DungeonStateReady)
                                    {
                                        // Encounter is in prep phase
                                    }
                                    else if (dungeonState == EDungeonState.DungeonStatePlaying)
                                    {
                                        // Encounter has begun
                                        EncounterManager.StopEncounter();
                                        EncounterManager.StartNewBattle();
                                        EncounterManager.StartEncounter();
                                    }
                                }
                            },
                            {
                                DungeonFlowInfo.ActiveTimeFieldNumber, dungeonFlowInfo =>
                                {
                                    var active_time = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.ActiveTime == {active_time}");
                                }
                            },
                            {
                                DungeonFlowInfo.ReadyTimeFieldNumber, dungeonFlowInfo =>
                                {
                                    var ready_time = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.ReadyTime == {ready_time}");
                                }
                            },
                            {
                                DungeonFlowInfo.PlayTimeFieldNumber, dungeonFlowInfo =>
                                {
                                    var play_time = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.PlayTime == {play_time}");
                                }
                            },
                            {
                                DungeonFlowInfo.EndTimeFieldNumber, dungeonFlowInfo =>
                                {
                                    var end_time = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.EndTime == {end_time}");
                                }
                            },
                            {
                                DungeonFlowInfo.SettlementTimeFieldNumber, dungeonFlowInfo =>
                                {
                                    var settlement_time = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.SettlementTime == {settlement_time}");
                                }
                            },
                            {
                                DungeonFlowInfo.DungeonTimesFieldNumber, dungeonFlowInfo =>
                                {
                                    var dungeon_times = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.DungeonTimes == {dungeon_times}");
                                }
                            },
                            {
                                DungeonFlowInfo.ResultFieldNumber, dungeonFlowInfo =>
                                {
                                    var result = dungeonFlowInfo.ReadInt32();
                                    dungeonFlowInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.Result == {result}");
                                }
                            }
                        };

                        ReadBinaryContainer(dungeonSyncData, flowInfoDataFuncs, "DungeonFlowInfo");
                    }
                },
                {
                    DungeonSyncData.DungeonVarFieldNumber, dungeonSyncData =>
                    {
                        var dungeonVarDataFuncs = new Dictionary<int, Action<BinaryReader>>()
                        {
                            {
                                DungeonVar.DungeonVarDataFieldNumber, dungeonVarData =>
                                {
                                    int count = dungeonVarData.ReadInt32();
                                    dungeonVarData.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonVar.count={count}");
                                    if (count == -4)
                                    {
                                        return;
                                    }

                                    Dictionary<string, int> kvp = new();

                                    string lastName = "";

                                    for (int i = 0; i < count; i++)
                                    {
                                        var dungeonVarDataDataFuncs = new Dictionary<int, Action<BinaryReader>>()
                                        {
                                            {
                                                DungeonVarData.NameFieldNumber, dungeonVarDataData =>
                                                {
                                                    var length = dungeonVarDataData.ReadInt32();
                                                    dungeonVarDataData.ReadInt32();

                                                    var name = Encoding.UTF8.GetString(dungeonVarDataData.ReadBytes(length));
                                                    dungeonVarDataData.ReadInt32();

                                                    lastName = name;

                                                    //System.Diagnostics.Debug.WriteLine($"dungeonVarDataData.name={name}");
                                                }
                                            },
                                            {
                                                DungeonVarData.ValueFieldNumber, dungeonVarDataData =>
                                                {
                                                    var value = dungeonVarDataData.ReadInt32();
                                                    dungeonVarDataData.ReadInt32();

                                                    kvp[lastName] = value;

                                                    //System.Diagnostics.Debug.WriteLine($"dungeonVarDataData.value={value}");
                                                }
                                            }
                                        };

                                        ReadBinaryContainer(dungeonVarData, dungeonVarDataDataFuncs, "DungeonVarData");
                                    }

                                    //System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonVar kvp={kvp}");
                                }
                            }
                        };

                        ReadBinaryContainer(dungeonSyncData, dungeonVarDataFuncs, "DungeonVar");
                    }
                },
                {
                    DungeonSyncData.DungeonScoreFieldNumber, dungeonSyncData =>
                    {
                        var scoreDataFuncs = new Dictionary<int, Action<BinaryReader>>()
                        {
                            {
                                DungeonScore.TotalScoreFieldNumber, dungeonScore =>
                                {
                                    var totalScore = dungeonScore.ReadInt32();
                                    dungeonScore.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonScore.TotalScore={totalScore}");
                                }
                            },
                            {
                                DungeonScore.CurRatioFieldNumber, dungeonScore =>
                                {
                                    var curRatio = dungeonScore.ReadInt32();
                                    dungeonScore.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonScore.CurRatio={curRatio}");
                                }
                            }
                        };

                        ReadBinaryContainer(dungeonSyncData, scoreDataFuncs, "DungeonScore");
                    }
                },
                {
                    DungeonSyncData.ReviveInfoFieldNumber, dungeonSyncData =>
                    {
                        var reviveInfoFuncs = new Dictionary<int, Action<BinaryReader>>()
                        {
                            {
                                DungeonReviveInfo.ReviveIdsFieldNumber, dungeonReviveInfo =>
                                {
                                    List<int> revive_ids = new();

                                    var count = dungeonReviveInfo.ReadInt32();
                                    dungeonReviveInfo.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveIds.count={count}");
                                    if (count == -4)
                                    {
                                        return;
                                    }

                                    for (int i = 0; i < count; i++)
                                    {
                                        var id = dungeonReviveInfo.ReadInt32();
                                        dungeonReviveInfo.ReadInt32();
                                        revive_ids.Add(id);
                                        System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveIds[{i}].id={id}");
                                    }
                                }
                            },
                            {
                                DungeonReviveInfo.ReviveMapFieldNumber, dungeonReviveInfo =>
                                {
                                    int add = dungeonReviveInfo.ReadInt32();
                                    _ = dungeonReviveInfo.ReadInt32();
                                    int remove = 0;
                                    int update = 0;
                                    if (add == -4)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap.add={add} (Early Exit)");
                                        return;
                                    }
                                    if (add == -1)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap.add={add} (Get New Value)");
                                        add = dungeonReviveInfo.ReadInt32();
                                    }
                                    else
                                    {
                                        remove = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();
                                        update = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();
                                    }
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap.add={add}");
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap.remove={remove}");
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap.update={update}");

                                    Dictionary<int, int> reviveMap = new();

                                    for (int i = 0; i < add; i++)
                                    {
                                        int dk = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();
                                        int dv = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();

                                        reviveMap.Add(dk, dv);
			                        }
                                    for (int i = 0; i < remove; i++)
                                    {
                                        int dk = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();

                                        if (!reviveMap.Remove(dk))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap did not find key to remove ({dk})");
                                        }
                                    }
                                    for (int i = 0; i < update; i++)
                                    {
                                        int dk = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();
                                        int dv = dungeonReviveInfo.ReadInt32();
                                        _ = dungeonReviveInfo.ReadInt32();

                                        if (reviveMap.ContainsKey(dk))
                                        {
                                            reviveMap[dk] = dv;
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonReviveInfo.ReviveMap did not find key to update ({dk})");
                                            reviveMap.Add(dk, dv);
                                        }
                                    }
                                }
                            }
                        };

                        ReadBinaryContainer(dungeonSyncData, reviveInfoFuncs, "DungeonReviveInfo");
                    }
                }
            };

            ReadBinaryContainer(br, dataFuncs, "SyncDungeonDirtyData");
            return;

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
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.SettlementTime == {settlement_time}");
                                    break;
                                }
                            case DungeonFlowInfo.DungeonTimesFieldNumber:
                                {
                                    var dungeon_times = br.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.DungeonTimes == {dungeon_times}");
                                    break;
                                }
                            case DungeonFlowInfo.ResultFieldNumber:
                                {
                                    var result = br.ReadInt32();
                                    System.Diagnostics.Debug.WriteLine($"SyncDungeonDirtyData.DungeonFlowInfo.Result == {result}");
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
                        while (br.BaseStream.Position < br.BaseStream.Length)
                        {
                            int tag = br.ReadInt32();
                            if (tag != -2)
                            {
                                // Invalid begin tag: {tag}
                                // return;
                            }
                            int size = br.ReadInt32();
                            if (size == -3)
                            {
                                // return;
                            }
                            long offset = br.BaseStream.Position;
                            int index = br.ReadInt32();

                            // while 0 < index
                            // {
                            // read data
                            // else
                            // if no function to read this data, br.BaseStream.Position = offset + size;
                            // index = br.ReadInt32();
                            // }
                        }

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
                                System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonScore.TotalScore={totalScore}");
                                // This appears to be called when a dungeon is complete enough to unlock boss access - a good time to end the current encounter
                                EncounterManager.StartEncounter();
                                break;
                            case DungeonScore.CurRatioFieldNumber:
                                var curRatio = br.ReadInt32();
                                System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonScore.CurRatio={curRatio}");
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
                        // Likely need to do this check always
                        /*if (!DoesStreamHaveIdentifier(br))
                        {
                            break;
                        }
                        uint subFieldIndex = br.ReadUInt32();
                        _ = br.ReadInt32();*/

                        // DungeonReviveInfo: revivie_ids = vec<int32>; revivie_map = hashmap<int32, int32>
                        // This seems to be consistent when there's a raid wipe
                        var revive_info = DungeonReviveInfo.Parser.ParseFrom(br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position)));


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
                        var var = DungeonVarData.Parser.ParseFrom(br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position)));
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

        static void ReadBinaryContainer(BinaryReader br, Dictionary<int, Action<BinaryReader>> dataFuncs, string debugName = "")
        {
            // TODO: ensure object with dataFuncs functions is given

            if (br.BaseStream.Position + 8 >= br.BaseStream.Length)
            {
                System.Diagnostics.Debug.WriteLine($"Stream container size is too small! Pos:{br.BaseStream.Position} Len:{br.BaseStream.Length}");
                return;
            }

            int tag = br.ReadInt32();
            _ = br.ReadInt32();
            if (tag != -2)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid begin tag: {tag}");
                return;
            }
            
            int size = br.ReadInt32();
            _ = br.ReadInt32();
            if (size == -3)
            {
                return;
            }
            if (size < 0)
            {
                System.Diagnostics.Debug.WriteLine($"ReadBinaryContainer size was unexpectedly negative! size = {size}");
                return;
            }

            long offset = br.BaseStream.Position;
            int index = br.ReadInt32(); // "Field Number"
            _ = br.ReadInt32();

            while (0 < index)
            {
                //System.Diagnostics.Debug.WriteLine($"Container block size = {size}");
                if (!string.IsNullOrEmpty(debugName))
                {
                    System.Diagnostics.Debug.WriteLine($"{debugName} FieldNumber(index)={index}");
                }

                if (dataFuncs.ContainsKey(index))
                {
                    dataFuncs[index](br);
                }
                else
                {
                    br.BaseStream.Position = offset + size;
                }

                index = br.ReadInt32();
                _ = br.ReadInt32();
            }
            if (index != -3)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid end tag: {index}");
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
        static bool IsUuidPlayerRaw(long uuidRaw) => (uuidRaw & 0xFFFFL) == 640L;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long Shr16(long v) => v >> 16;
    }
}
