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
using System.Collections.Concurrent;
using ZLinq;

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
                CaptureDeviceName = Settings.Instance.NetCaptureDeviceName, // "\\Device\\NPF_{40699DEA-27A5-4985-ADC0-B00BADABAAEB}"
                ExeNames = Utils.GameCapturePreferenceToExeNames(Settings.Instance.GameCapturePreference)
            });

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncContainerData, ProcessSyncContainerData);
            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncContainerDirtyData, ProcessSyncContainerDirtyData);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncNearDeltaInfo, ProcessSyncNearDeltaInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncToMeDeltaInfo, ProcessSyncToMeDeltaInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncNearEntities, ProcessSyncNearEntities);

            netCap.RegisterNotifyHandler(936649811, (uint)BPSR_DeepsLib.ServiceMethods.WorldActivityNtf.SyncHitInfo, ProcessSyncHitInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncDungeonData, ProcessSyncDungeonData);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncDungeonDirtyData, ProcessSyncDungeonDirtyData);

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.NotifyAllMemberReady, ProcessNotifyAllMemberReady);
            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.NotifyCaptainReady, ProcessNotifyCaptainReady);

            netCap.RegisterMatchNotifyHandler(BPSR_DeepsLib.ServiceMethods.MatchNtf.EnterMatchResult, ProcessEnterMatchResult);
            netCap.RegisterMatchNotifyHandler(BPSR_DeepsLib.ServiceMethods.MatchNtf.CancelMatchResult, ProcessCancelMatchResult);
            netCap.RegisterMatchNotifyHandler(BPSR_DeepsLib.ServiceMethods.MatchNtf.MatchReadyStatus, ProcessMatchReadyStatus);

            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NoticeUpdateTeamInfo, ProcessNoticeUpdateTeamInfo);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NoticeUpdateTeamMemberInfo, ProcessNoticeUpdateTeamMemberInfo);
            //
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyTeamActivityState, ProcessNotifyTeamActivityState);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.TeamActivityResult, ProcessTeamActivityResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.TeamActivityListResult, ProcessTeamActivityListResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.TeamActivityVoteResult, ProcessTeamActivityVoteResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyCharMatchResult, ProcessNotifyCharMatchResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyTeamMatchResult, ProcessNotifyTeamMatchResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyCharAbortMatch, ProcessNotifyCharAbortMatch);
            //
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.UpdateTeamMemBeCall, ProcessUpdateTeamMemBeCall);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyTeamMemBeCall, ProcessNotifyTeamMemBeCall);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyTeamMemBeCallResult, ProcessNotifyTeamMemBeCallResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_DeepsLib.ServiceMethods.GrpcTeamNtf.NotifyTeamEnterErr, ProcessNotifyTeamEnterErr);

            // Uncomment to debug print unhandled events
            //netCap.RegisterUnhandledHandler(ProcessUnhandled);

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

        public static void ProcessUnhandled(NotifyId notifyId, ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessUnhandled ServiceId:{(EServiceId)notifyId.ServiceId} MethodId:{notifyId.MethodId} Payload.Length:{payloadBuffer.Length}");
            if (payloadBuffer.Length == 0)
            {
                return;
            }
        }

        public static void ProcessNotifyAllMemberReady(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNotifyAllMemberReady");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = WorldNtf.Types.NotifyAllMemberReady.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            // This is called to open the Ready Check UI (vOpenOrClose will always be true)
            NotificationAlertManager.PlayNotifyAudio(NotificationAlertManager.NotificationType.ReadyCheck);
        }

        public static void ProcessNotifyCaptainReady(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNotifyCaptainReady");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = WorldNtf.Types.NotifyCaptainReady.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            if (vData.VCharId == AppState.PlayerUID)
            {
                // Current player is "responding" to the Ready Check
                if (vData.VReadyInfo.IsReady)
                {
                    // Player responded "Ready"
                }
                else
                {
                    // Player responded "Not Ready" or did not respond in time
                }
                NotificationAlertManager.StopNotifyAudio();
            }
        }

        public static void ProcessNoticeUpdateTeamInfo(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNoticeUpdateTeamInfo");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NoticeUpdateTeamInfo.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNoticeUpdateTeamInfo(vData, extraData);
        }

        public static void ProcessNoticeUpdateTeamMemberInfo(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNoticeUpdateTeamMemberInfo");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NoticeUpdateTeamMemberInfo.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNoticeUpdateTeamMemberInfo(vData, extraData);
        }

        public static void ProcessNotifyTeamActivityState(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNotifyTeamActivityState");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyTeamActivityState.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNotifyTeamActivityState(vData, extraData);
        }

        public static void ProcessTeamActivityResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessTeamActivityResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.TeamActivityResult.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessTeamActivityResult(vData, extraData);
        }

        public static void ProcessTeamActivityListResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessTeamActivityListResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.TeamActivityListResult.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessTeamActivityListResult(vData, extraData);
        }

        public static void ProcessTeamActivityVoteResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessTeamActivityVoteResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.TeamActivityVoteResult.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessTeamActivityVoteResult(vData, extraData);
        }

        public static void ProcessNotifyCharMatchResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessNotifyCharMatchResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyCharMatchResult.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessNotifyTeamMatchResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessNotifyTeamMatchResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyTeamMatchResult.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessNotifyCharAbortMatch(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessNotifyCharAbortMatch");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyCharAbortMatch.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessUpdateTeamMemBeCall(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessUpdateTeamMemBeCall");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.UpdateTeamMemBeCall.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            //System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessNotifyTeamMemBeCall(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessNotifyTeamMemBeCall");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyTeamMemBeCall.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            //System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessNotifyTeamMemBeCallResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessNotifyTeamMemBeCallResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyTeamMemBeCallResult.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            //System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessNotifyTeamEnterErr(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            System.Diagnostics.Debug.WriteLine("ProcessNotifyTeamEnterErr");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyTeamEnterErr.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            //System.Diagnostics.Debug.WriteLine(vData);
        }

        public static void ProcessEnterMatchResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            // Fired when Matchmaking begins
            System.Diagnostics.Debug.WriteLine("ProcessEnterMatchResult");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = MatchNtf.Types.EnterMatchResultNtf.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            MatchManager.ProcessEnterMatchResult(vData, extraData);
        }

        public static void ProcessCancelMatchResult(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            // Fired when Matchmaking ends
            System.Diagnostics.Debug.WriteLine("ProcessCancelMatchResult");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = MatchNtf.Types.CancelMatchResultNtf.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            MatchManager.ProcessCancelMatchResult(vData, extraData);
        }

        public static void ProcessMatchReadyStatus(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            // Fires each time a ready status is changed
            System.Diagnostics.Debug.WriteLine("ProcessMatchReadyStatus");

            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = MatchNtf.Types.MatchReadyStatusNtf.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            MatchManager.ProcessMatchReadyStatus(vData, extraData);
        }

        public static void ProcessSyncHitInfo(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
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

                        if (uuid == currentUserUuid)
                        {
                            CheckForWipe();
                        }
                        
                        break;
                    /*case EAttrType.AttrCombatState:
                    case EAttrType.AttrInBattleShow:
                        if (uuid == 285140 && attr.Id == 104 || attr.Id == 186)
                        {
                            System.Diagnostics.Debug.WriteLine($"[YOU] had {(EAttrType)attr.Id} = {reader.ReadInt32()}");
                        }
                        break;*/
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
                    case EAttrType.AttrActionTime:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrActionTime", reader.ReadInt64());
                        break;
                    case EAttrType.AttrActionUpperTime:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrActionUpperTime", reader.ReadInt64());
                        break;
                    case EAttrType.AttrStiffTarget:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrStiffTarget", reader.ReadInt64());
                        break;
                    case EAttrType.AttrStiffStageTime:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrStiffStageTime", reader.ReadInt64());
                        break;
                    case EAttrType.AttrSkillBeginTime:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSkillBeginTime", reader.ReadInt64());
                        break;
                    case EAttrType.AttrFirstAttack:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrFirstAttack", reader.ReadInt64());
                        break;
                    case EAttrType.AttrCombatStateTime:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrCombatStateTime", reader.ReadInt64());
                        break;
                    case EAttrType.AttrTargetUuid:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrTargetUuid", reader.ReadInt64());
                        break;
                    case EAttrType.AttrTargetId:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrTargetId", reader.ReadInt64());
                        break;
                    case EAttrType.AttrSummonerId:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSummonerId", reader.ReadInt64());
                        break;
                    case EAttrType.AttrTopSummonerId:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrTopSummonerId", reader.ReadInt64());
                        break;
                    default:
                        string attr_name = ((EAttrType)attr.Id).ToString();
                        EncounterManager.Current.SetAttrKV(uuid, attr_name, reader.ReadInt32());
                        //System.Diagnostics.Debug.WriteLine($"{attr_name} was hit");
                        break;
                }
            }
        }

        public static void ProcessSyncNearEntities(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //Log.Information($"ProcessSyncNearEntities: Meesage arrival time: {extraData.ArrivalTime}. Diff to DateTime now: {DateTime.Now - extraData.ArrivalTime}");

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

                long uid = Utils.UuidToEntityId(entity.Uuid);

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

                ProcessAttrs(entity.Uuid, attrCollection.Attrs);

                /*switch (entity.EntType)
                {
                    case EEntityType.EntMonster:
                    case EEntityType.EntChar:
                    case EEntityType.EntField:
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
                }*/
            }

            // We do this at the end in case we need to capture an entity Attr before a potential Start/End call happens
            BattleStateMachine.CheckDeferredCalls();
        }

        public static void ProcessSyncNearDeltaInfo(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            var syncNearDeltaInfo = SyncNearDeltaInfo.Parser.ParseFrom(payloadBuffer);
            //Log.Information("Notify: {Hex}", BitConverter.ToString(span.ToArray()));
            if (syncNearDeltaInfo.DeltaInfos == null || syncNearDeltaInfo.DeltaInfos.Count == 0)
            {
                return;
            }

            foreach (var aoiSyncDelta in syncNearDeltaInfo.DeltaInfos)
            {
                ProcessAoiSyncDelta(aoiSyncDelta, extraData);
            }

            // We do this at the end in case we need to capture an entity Attr before a potential Start/End call happens
            BattleStateMachine.CheckDeferredCalls();
        }

        public static void ProcessAoiSyncDelta(AoiSyncDelta delta, ExtraPacketData extraData)
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

            bool isTargetPlayer = (Utils.UuidToEntityType(targetUuid) == (long)EEntityType.EntChar); //IsUuidPlayerRaw(targetUuid);
            long targetUid = Utils.UuidToEntityId(targetUuid);
            var attrCollection = delta.Attrs;

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

            if (delta.BuffEffect != null)
            {
                //System.Diagnostics.Debug.WriteLine($"delta.BuffEffect={delta.BuffEffect.BuffEffects.Count}");

                for (int buffIdx = 0; buffIdx < delta.BuffEffect.BuffEffects.Count; buffIdx++)
                {
                    // Shield buffs appear to use Type == BuffEventAddTo and BuffEventRemove
                    var buffEffect = delta.BuffEffect.BuffEffects[buffIdx];
                    //System.Diagnostics.Debug.WriteLine($"BuffEffect[{buffIdx}] = {buffEffect}");

                    if (delta.BuffInfos != null)
                    {
                        var matchInfo = delta.BuffInfos.BuffInfos.AsValueEnumerable().Where(x => x.BuffUuid == buffEffect.BuffUuid);
                        if (matchInfo.Any())
                        {
                            var buffInfo = matchInfo.First();
                            EncounterManager.Current.NotifyBuffEvent(targetUuid, buffEffect.Type, buffEffect.BuffUuid, buffInfo.BaseId, buffInfo.Level, buffInfo.FireUuid, buffInfo.Layer, buffInfo.Duration, buffInfo.FightSourceInfo.SourceConfigId, extraData);
                        }
                    }
                    else
                    {
                        // Most commonly appears to include EBuffEventType.BuffEventRemove, EBuffEventType.BuffEventAddTo, EBuffEventType.BuffEventRemoveLayer

                        EncounterManager.Current.NotifyBuffEvent(targetUuid, buffEffect.Type, buffEffect.BuffUuid, 0, 0, 0, 0, 0, 0, extraData);
                    }
                }
            }

            /*if (delta.BuffInfos != null)
            {
                System.Diagnostics.Debug.WriteLine($"delta.BuffInfos={delta.BuffInfos.BuffInfos.Count}");

                for (int buffIdx = 0; buffIdx < delta.BuffInfos.BuffInfos.Count; buffIdx++)
                {
                    var buffInfo = delta.BuffInfos.BuffInfos[buffIdx];
                    System.Diagnostics.Debug.WriteLine($"BuffInfo[{buffIdx}] = {buffInfo}");
                }
            }*/

            var skillEffect = delta.SkillEffects;
            if (skillEffect != null)
            {
                //System.Diagnostics.Debug.WriteLine($"skillEffect({skillEffect.Damages.Count})={skillEffect}");
            }
            
            if (skillEffect?.Damages == null || skillEffect.Damages.Count == 0)
            {
                return;
            }

            foreach (var syncDamageInfo in skillEffect.Damages)
            {
                // OwnerId = SkillId this is coming from
                // OwnerLevel = Level of the Skill this came from
                // TopSummonerId = If exists, Entity UUID that summoned this skill damage source (ex: Battle Imagines use this)
                // Property = Element of the damage if it has any (ex: General, Light, Dark, Fire, Frost, etc.)
                // DamageMode = Physical or Magical damage type

                // HpLessen = Actual Health modification from this skill
                // Value = Requested Health modification
                // LuckyValue = Lucky Strike version of Value (only one of these can be present at a time)
                // IsDead = Skill killed an entity with this damage event
                // If IsDead is true, and Value > HpLessen, the difference is the Overkill amount

                //System.Diagnostics.Debug.WriteLine(syncDamageInfo);

                int skillId = syncDamageInfo.OwnerId;
                if (skillId == 0)
                {
                    continue;
                }

                long attackerUuid = (syncDamageInfo.TopSummonerId != 0 ? syncDamageInfo.TopSummonerId : syncDamageInfo.AttackerUuid);
                if (attackerUuid == 0)
                {
                    continue;
                }
                bool isAttackerPlayer = IsUuidPlayerRaw(attackerUuid);

                if (syncDamageInfo.TopSummonerId != 0)
                {
                    if (EncounterManager.Current.Entities.TryGetValue(syncDamageInfo.AttackerUuid, out var summonedEntity))
                    {
                        EncounterManager.Current.UpdateCasterSkillTierLevel(syncDamageInfo.TopSummonerId, summonedEntity);
                    }
                }

                if (AppState.IsBenchmarkMode && isAttackerPlayer && attackerUuid != AppState.PlayerUUID)
                {
                    // Only record benchmarking player related details
                    // All Monsters and other non-player entities will be processed still
                    return;
                }
                else if (AppState.IsBenchmarkMode && isAttackerPlayer && attackerUuid == AppState.PlayerUUID && !AppState.HasBenchmarkBegun)
                {
                    AppState.HasBenchmarkBegun = true;
                    // This will automatically restart our Encounter Start Time to handle the start of the Benchmark Encounter
                    EncounterManager.StartEncounter(false, EncounterStartReason.BenchmarkStart);
                }

                if (isAttackerPlayer && attackerUuid != 0)
                {
                    EncounterManager.Current.SetEntityType(attackerUuid, EEntityType.EntChar);
                    var professionId = Professions.GetBaseProfessionIdBySkillId(skillId);
                    if (professionId != 0 && EncounterManager.Current.GetOrCreateEntity(attackerUuid).ProfessionId <= 0)
                    {
                        EncounterManager.Current.SetProfessionId(attackerUuid, professionId);
                    }
                }

                long damage = 0;
                if (syncDamageInfo.Value != 0)
                {
                    damage = syncDamageInfo.Value;
                }
                else if (syncDamageInfo.LuckyValue != 0)
                {
                    damage = syncDamageInfo.LuckyValue;
                }
                // If damage is 0, the target was likely Immune and the DamageType value will reflect that
                // We will still pass it on so it can be properly registered in the encounter/entity

                if (damage < 0)
                {
                    System.Diagnostics.Debug.WriteLine("syncDamageInfo damage value is negative!");
                    System.Diagnostics.Debug.WriteLine(syncDamageInfo);
                }

                bool isCrit = syncDamageInfo.TypeFlag != null && ((syncDamageInfo.TypeFlag & 1) == 1);
                bool isHeal = syncDamageInfo.Type == EDamageType.Heal;
                var luckyValue = syncDamageInfo.LuckyValue;
                bool isLucky = luckyValue != null && luckyValue != 0;
                long hpLessen = syncDamageInfo.HpLessenValue;

                bool isCauseLucky = syncDamageInfo.TypeFlag != null && ((syncDamageInfo.TypeFlag & 0B100) == 0B100);

                bool isMiss = syncDamageInfo.IsMiss;

                bool isDead = syncDamageInfo.IsDead;

                string damageElement = syncDamageInfo.Property.ToString();

                EDamageSource damageSource = syncDamageInfo.DamageSource;

                if (isHeal)
                {
                    EncounterManager.Current.AddHealing((isAttackerPlayer ? attackerUuid : 0), targetUuid, skillId, syncDamageInfo.OwnerLevel, damage, hpLessen, syncDamageInfo.Property, syncDamageInfo.Type, syncDamageInfo.DamageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, extraData);
                }
                else
                {
                    EncounterManager.Current.AddDamage(attackerUuid, targetUuid, skillId, syncDamageInfo.OwnerLevel, damage, hpLessen, syncDamageInfo.Property, syncDamageInfo.Type, syncDamageInfo.DamageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, extraData);

                    EncounterManager.Current.AddTakenDamage(attackerUuid, targetUuid, skillId, syncDamageInfo.OwnerLevel, damage, hpLessen, syncDamageInfo.Property, syncDamageInfo.Type, syncDamageInfo.DamageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, extraData);
                }
            }

            // We do this at the end in case we need to capture an entity Attr before a potential Start/End call happens
            BattleStateMachine.CheckDeferredCalls();
        }

        public static long currentUserUuid = 0;

        public static void ProcessSyncToMeDeltaInfo(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            var syncToMeDeltaInfo = SyncToMeDeltaInfo.Parser.ParseFrom(payloadBuffer);
            var aoiSyncToMeDelta = syncToMeDeltaInfo.DeltaInfo;
            long uuid = aoiSyncToMeDelta.Uuid;
            if (uuid != 0 && currentUserUuid != uuid)
            {
                currentUserUuid = uuid;
                AppState.PlayerUUID = uuid;
                AppState.PlayerUID = Utils.UuidToEntityId(uuid);
            }
            var aoiSyncDelta = aoiSyncToMeDelta.BaseDelta;
            if (aoiSyncDelta == null)
            {
                return;
            }
            ProcessAoiSyncDelta(aoiSyncDelta, extraData);

            // We do this at the end in case we need to capture an entity Attr before a potential Start/End call happens
            BattleStateMachine.CheckDeferredCalls();
        }

        public static void ProcessSyncContainerData(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            BattleStateMachine.CheckDeferredCalls();
            // This might only occur on map change and comes from the current player, no one else
            // Teleports do not trigger this
            // As this occurs the moment a load actually begins, many states are likely not going to be set yet
            // This mainly is how the current local player will get their own data though


            // We'll spin up a new encounter before processing any of this data so it's nice and fresh in the new encounter
            if (false)
            {
                EncounterManager.StartNewBattle();
                EncounterManager.StartEncounter(true);
            }
            BattleStateMachine.StartNewBattle();

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

            long playerUuid = Utils.EntityIdToUuid(vData.CharId, (long)EEntityType.EntChar, false, false);

            System.Diagnostics.Debug.WriteLine($"ProcessSyncContainerData converted UID:{vData.CharId} into UUID:{playerUuid}");

            AppState.PlayerUID = vData.CharId;
            long playerUid = vData.CharId;

            if (vData.RoleLevel?.Level != 0)
            {
                EncounterManager.Current.SetAttrKV(playerUuid, "AttrLevel", vData.RoleLevel.Level);
            }

            if (vData.Attr?.CurHp != 0)
            {
                EncounterManager.Current.SetAttrKV(playerUuid, "AttrHp", vData.Attr.CurHp);
            }

            if (vData.Attr?.MaxHp != 0)
            {
                EncounterManager.Current.SetAttrKV(playerUuid, "AttrMaxHp", vData.Attr.MaxHp);
            }

            if (vData.CharBase != null)
            {
                if (!string.IsNullOrEmpty(vData.CharBase.Name))
                {
                    EncounterManager.Current.SetName(playerUuid, vData.CharBase.Name);
                    AppState.PlayerName = vData.CharBase.Name;
                }

                if (vData.CharBase.FightPoint != 0)
                {
                    EncounterManager.Current.SetAbilityScore(playerUuid, vData.CharBase.FightPoint);
                }
            }

            var professionList = vData.ProfessionList;
            if (professionList != null && professionList.CurProfessionId != 0)
            {
                var professionName = Professions.GetProfessionNameFromId(professionList.CurProfessionId);
                EncounterManager.Current.SetProfessionId(playerUuid, professionList.CurProfessionId);
                AppState.ProfessionId = professionList.CurProfessionId;
                AppState.ProfessionName = professionName;
            }

            var sceneData = vData.SceneData;
            if (sceneData != null)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessSyncContainerData.SceneData:\n{sceneData}");

                EncounterManager.SetSceneId(sceneData.LevelMapId);
                EncounterManager.Current.SetChannelLineNumber(sceneData.LineId);
            }

            if (vData.Equip != null)
            {
                foreach (var equip in vData.Equip.EquipList_)
                {
                    System.Diagnostics.Debug.WriteLine($"{playerUid} :: equip::slot={equip.Value.EquipSlot},refinelvl={equip.Value.EquipSlotRefineLevel}");
                }
            }
        }

        public static void ProcessSyncContainerDirtyData(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            BattleStateMachine.CheckDeferredCalls();

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
                var ser = new BPSR_DeepsLib.Blobs.CharSerialize(new BlobReader(buf));

                if (ser.CharBaseInfo != null)
                {
                    if (!string.IsNullOrEmpty(ser.CharBaseInfo.Name))
                    {
                        EncounterManager.Current.SetName(currentUserUuid, ser.CharBaseInfo.Name);
                        AppState.PlayerName = ser.CharBaseInfo.Name;
                    }
                    if (ser.CharBaseInfo.FightPoint != null)
                    {
                        EncounterManager.Current.SetAbilityScore(currentUserUuid, (int)ser.CharBaseInfo.FightPoint);
                    }

                    if (ser.CharBaseInfo.TeamInfo != null)
                    {
                        System.Diagnostics.Debug.WriteLine("ser.CharBaseInfo.TeamInfo != null)");
                    }
                }

                if (ser.Attr != null)
                {
                    if (ser.Attr.CurHp != null)
                    {
                        EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrHp", ser.Attr.CurHp);
                    }
                    if (ser.Attr.MaxHp != null)
                    {
                        EncounterManager.Current.SetAttrKV(currentUserUuid, "AttrMaxHp", ser.Attr.MaxHp);
                    }
                }

                if (ser.ProfessionList != null)
                {
                    if (ser.ProfessionList.CurProfessionId != null)
                    {
                        EncounterManager.Current.SetProfessionId(currentUserUuid, (int)ser.ProfessionList.CurProfessionId);
                    }
                }

                if (ser.SceneData != null)
                {
                    if (ser.SceneData.LevelMapId != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ser.SceneData.LevelMapId = {ser.SceneData.LevelMapId})");
                    }
                }

                return;

                using var ms = new MemoryStream(buf, writable: false);
                using var br = new BinaryReader(ms);

                if (!DoesStreamHaveIdentifier(br))
                {
                    return;
                }

                uint fieldIndex = br.ReadUInt32();
                _ = br.ReadInt32();

                long playerUid = currentUserUuid >> 16;

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
                                case CharBaseInfo.AreaIdFieldNumber:
                                    {
                                        int areaId = br.ReadInt32();
                                        _ = br.ReadInt32();

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

        public static void ProcessSyncDungeonData(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            BattleStateMachine.CheckDeferredCalls();

            // This might only occur on map change and comes from the current player, no one else
            // Teleports do not trigger this
            // Generally the dungeon has not begun at this point, it's likely not even in the Ready state
            var syncDungeonData = SyncDungeonData.Parser.ParseFrom(payloadBuffer);
            if (syncDungeonData?.VData == null)
            {
                return;
            }

            var vData = syncDungeonData.VData;

            if (vData.DungeonPlayerList != null && vData.DungeonPlayerList.PlayerInfos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("vData.DungeonPlayerList != null");
                System.Diagnostics.Debug.WriteLine(vData.DungeonPlayerList.PlayerInfos);
            }

            for(int listIdx = 0; listIdx < vData.Title.TitleList.Count; listIdx++)
            {
                var title_list = vData.Title.TitleList[listIdx];
                for (int infoIdx = 0; infoIdx < title_list.TitleInfo.Count; infoIdx++)
                {
                    var title_info = title_list.TitleInfo[infoIdx];
                    System.Diagnostics.Debug.WriteLine($"TitleList[{listIdx}].TitleInfo[{infoIdx}]: Uuid={title_info.Uuid},TitleId{title_info.TitleId}");
                }
            }

            EncounterManager.Current.DungeonState = vData.FlowInfo.State;
            BattleStateMachine.DungeonStateHistoryAdd(vData.FlowInfo.State);

            int dungeonVarDataIdx = 0;
            foreach (var dungeonVarData in vData.DungeonVar.DungeonVarData)
            {
                System.Diagnostics.Debug.WriteLine($"DungeonVar.DungeonVarData[{dungeonVarDataIdx}] = {dungeonVarData}");
                dungeonVarDataIdx++;
            }

            foreach (var targetData in vData.Target.TargetData)
            {
                BattleStateMachine.DungeonTargetDataHistoryAdd(targetData.Value);
                BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.Enqueue(targetData);
                System.Diagnostics.Debug.WriteLine($"Target.TargetData[{targetData.Key}]: TargetId={targetData.Value.TargetId},Nums={targetData.Value.Nums},Complete={targetData.Value.Complete}");
            }

            foreach (var damage in vData.Damage.Damages)
            {
                System.Diagnostics.Debug.WriteLine($"Damage.Damages[{damage.Key}]: {damage.Value}");
            }

            System.Diagnostics.Debug.WriteLine($"syncDungeonData.vData State={vData.FlowInfo.State},TotalScore={vData.DungeonScore.TotalScore},CurRatio={vData.DungeonScore.CurRatio}");
        }

        public static ConcurrentQueue<EActorState> PlayerStateHistory = new();
        public static void CheckForWipe()
        {
            // An encounter is considered a wipe when all the following are true:
            // Player was in the Dead state and is now going into the TelePort state either from Dead or Resurrection
            // All known bosses have 100% HP
            // In order to track this, we must hold onto the last couple player states

            if (!Settings.Instance.UseAutomaticWipeDetection)
            {
                return;
            }

            if (currentUserUuid != 0)
            {
                var playerEntity = EncounterManager.Current.GetOrCreateEntity(currentUserUuid);
                var attrState = playerEntity.GetAttrKV("AttrState");
                if (attrState != null)
                {
                    //System.Diagnostics.Debug.WriteLine($"{currentUserUuid} state = {attrState}");

                    if (PlayerStateHistory == null)
                    {
                        PlayerStateHistory = new();
                    }

                    if (PlayerStateHistory.Count >= 5)
                    {
                        PlayerStateHistory.TryDequeue(out _);
                    }

                    // NOTE: We have changed to only calling from the AttrState setter for now
                    // Since we call this function from multiple locations instead of only when an update to the state occurs
                    // We'll only be adding the latest unique state to our history tracker for now
                    // If there end up being wipe patterns that use duplicate state ordering, we can adjust it later to work with that
                    //if (PlayerStateHistory.Last() != EActorState.ActorStateDefault && PlayerStateHistory.Last() != (EActorState)attrState)
                    {
                        PlayerStateHistory.Enqueue((EActorState)attrState);
                    }
                }
                else
                {
                    return;
                }

                // Perform Player State pattern check
                // Wipe state patterns:
                // ActorStateDead > ActorStateResurrection > ActorStateTelePort
                // ActorStateDead > ActorStateTelePort
                bool useNoTeleportWipePattern = Settings.Instance.SkipTeleportStateCheckInAutomaticWipeDetection;
                bool isStateWipePattern = false;
                int stateCount = PlayerStateHistory.Count();
                if (useNoTeleportWipePattern == false && stateCount >= 3 && PlayerStateHistory.ElementAt(stateCount - 1) == EActorState.ActorStateTelePort)
                {
                    if (PlayerStateHistory.ElementAt(stateCount - 2) == EActorState.ActorStateResurrection)
                    {
                        if (PlayerStateHistory.ElementAt(stateCount - 3) == EActorState.ActorStateDead)
                        {
                            isStateWipePattern = true;
                        }
                    }
                    else if (PlayerStateHistory.ElementAt(stateCount - 2) == EActorState.ActorStateDead)
                    {
                        isStateWipePattern = true;
                    }
                }
                else if (useNoTeleportWipePattern && stateCount >= 2 && PlayerStateHistory.ElementAt(stateCount - 1) == EActorState.ActorStateResurrection)
                {
                    if (PlayerStateHistory.ElementAt(stateCount - 2) == EActorState.ActorStateDead)
                    {
                        isStateWipePattern = true;
                    }
                }
                //System.Diagnostics.Debug.WriteLine($"useNoTeleportWipePattern == {useNoTeleportWipePattern} && isStateWipePattern == {isStateWipePattern}");

                if (isStateWipePattern)
                {
                    // The player state is in a wipe pattern
                    // Check if there is a Boss type monster
                    var bosses = EncounterManager.Current.Entities.AsValueEnumerable().Where(x => x.Value.MonsterType == 2);
                    //System.Diagnostics.Debug.WriteLine($"bosses.Count = {bosses.Count()}");

                    if (bosses.Count() > 0)
                    {
                        int bossesAtMaxHp = 0;
                        foreach (var boss in bosses)
                        {
                            // If all bosses are full HP, then let's call it a wipe
                            long? hp = boss.Value.GetAttrKV("AttrHp") as long?;
                            long? maxHp = boss.Value.GetAttrKV("AttrMaxHp") as long?;
                            // Might need to use MaxHpTotal?
                            if (hp != null && maxHp != null && hp > 0 && maxHp > 0 && hp >= maxHp)
                            {
                                EncounterManager.Current.SetWipeState(true);
                                //System.Diagnostics.Debug.WriteLine($"We've hit a wipe (bossesAtMaxHp = {bossesAtMaxHp})! Start up a new encounter");
                                EncounterManager.StartEncounter(false, EncounterStartReason.Wipe);
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine($"We didn't hit a wipe yet {boss.UUID} - {boss.Name} {hp} / {maxHp}");
                            }
                        }
                    }
                }
            }
        }

        public static void ProcessSyncDungeonDirtyData(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            BattleStateMachine.CheckDeferredCalls();

            var dirty = SyncDungeonDirtyData.Parser.ParseFrom(payloadBuffer);
            if (dirty?.VData?.Buffer == null || dirty.VData.Buffer.Length == 0)
            {
                return;
            }

            var buf = dirty.VData.Buffer.ToByteArray();

            var dun = new BPSR_DeepsLib.Blobs.DungeonDirtyData(new BlobReader(buf));

            if (dun?.PlayerList != null && dun?.PlayerList.PlayerInfos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("dun?.PlayerList != null");
                System.Diagnostics.Debug.WriteLine(dun?.PlayerList.PlayerInfos);
            }

            if (dun?.FlowInfo != null)
            {
                if (dun.FlowInfo?.State != null)
                {
                    EDungeonState dungeonState = (EDungeonState)dun.FlowInfo.State;
                    EncounterManager.Current.DungeonState = dungeonState;
                    BattleStateMachine.DungeonStateHistoryAdd(dungeonState);
                    if (false)
                    {
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
                            //EncounterManager.StopEncounter();
                            if (EncounterManager.Current.HasStatsBeenRecorded())
                            {
                                // Start up a new BattleId if we've already recording actions happening before the dungeon's proper start
                                // This prevents us from throwing away any data of potential interest to the user
                                EncounterManager.StartNewBattle();
                                EncounterManager.StartEncounter(true);
                            }
                            else
                            {
                                EncounterManager.StartEncounter();
                            }

                        }
                    }
                }
            }

            if (dun?.Damage != null)
            {
                foreach (var item in dun.Damage.Damages)
                {
                    System.Diagnostics.Debug.WriteLine($"dun.Damage.Damages = {item.Key}, {item.Value}");
                }
            }

            if (dun?.DungeonPioneer != null)
            {
                foreach (var item in dun?.DungeonPioneer?.CompletedTargetThisTime)
                {
                    var CompletedTargetListIdx = 0;
                    foreach (var completedTargetList in item.Value.CompletedTargetList)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{item.Key}]CompletedTargetList[{CompletedTargetListIdx}] = {completedTargetList.Key}, {completedTargetList.Value}");
                        CompletedTargetListIdx++;
                    }
                }
            }

            if (dun?.DungeonVar?.Data != null)
            {
                if (dun?.DungeonVar?.Data.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine("DungeonVar.Data.Count > 1!!");
                }

                int dungeonVarDataIdx = 0;
                foreach (var dungeonVarData in dun.DungeonVar.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"dun.DungeonVar.Data.dungeonVarData[{dungeonVarDataIdx}] = {dungeonVarData.Name}, {dungeonVarData.Value}");

                    dungeonVarDataIdx++;
                }
            }

            if (dun?.DungeonEvent?.DungeonEventData != null)
            {
                foreach (var dungeonEventData in dun.DungeonEvent.DungeonEventData)
                {
                    System.Diagnostics.Debug.WriteLine($"[{dungeonEventData.Key}]DungeonEventData = {dungeonEventData.Value.EventId}, {dungeonEventData.Value.StartTime}, {dungeonEventData.Value.State}, {dungeonEventData.Value.Result}");
                    if (dungeonEventData.Value.DungeonTarget != null)
                    {
                        foreach (var dungeonTarget in dungeonEventData.Value.DungeonTarget)
                        {
                            System.Diagnostics.Debug.WriteLine($"- {dungeonTarget.Key}: {dungeonTarget.Value.TargetId}, {dungeonTarget.Value.Complete}, {dungeonTarget.Value.Nums}");
                        }
                    }
                }
            }

            if (dun?.TimerInfo != null)
            {
                System.Diagnostics.Debug.WriteLine($"dun.TimerInfo = {dun.TimerInfo.TimerType}, {dun.TimerInfo.StartTime}, {dun.TimerInfo.DungeonTimes}, {dun.TimerInfo.Direction}, {dun.TimerInfo.Index}, {dun.TimerInfo.ChangeTime}, {dun.TimerInfo.EffectType}, {dun.TimerInfo.PauseTime}, {dun.TimerInfo.PauseTotalTime}, {dun.TimerInfo.OutLookType}");
            }

            if (dun?.DungeonVarAll?.DungeonVarAllMap?.Count > 0)
            {
                if (dun?.DungeonVar?.Data.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine("DungeonVarAll.DungeonVarAllMap.Count > 1!!");
                }

                int dungeonVarAllMapIdx = 0;
                foreach (var dungeonVarAllMap in dun.DungeonVarAll.DungeonVarAllMap)
                {
                    int dungeonVarDataIdx = 0;
                    foreach (var dungeonVarData in dungeonVarAllMap.Value.Data)
                    {
                        System.Diagnostics.Debug.WriteLine($"dun.DungeonVarAll.DungeonVarAllMap.[{dungeonVarAllMapIdx}][{dungeonVarAllMap.Key}][{dungeonVarDataIdx}] = {dungeonVarData.Name}, {dungeonVarData.Value}");
                        dungeonVarDataIdx++;
                    }

                    dungeonVarAllMapIdx++;
                }
            }

            if (dun?.Target?.TargetData != null)
            {
                if (dun.Target.TargetData.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine("Target.TargetData.Count > 1!!");
                }

                // We typically only have a single entry
                // Must ForEach as the keys here are TargetId's
                foreach (var target in dun.Target.TargetData)
                {
                    System.Diagnostics.Debug.WriteLine($"dun.Target.TargetData.Target = {target.Key}, [TargetId:{target.Value.TargetId}, Complete:{target.Value.Complete}, Nums:{target.Value.Nums}]");
                    // Potentially use a TargetId blacklist to stop known messy targets from causing weird resets
                    // 3010101/3010102 in Stimen Vault lead to bad tracking as a floor is cleared (sent just before last enemy is fully dead)
                    // We can provide a list of predetermined id's for users to opt out of tracking here and rely on other targets or states
                    if (false)
                    {
                        if (Settings.Instance.SplitEncountersOnNewPhases)
                        {
                            // Not all encounters are created equal, how they use these is unique per encounter
                            // For example, Tina won't clear or update Target on wipe while Ice Dragon Raid does
                            if (target.Value.Complete == 1 && target.Value.Nums > 0)
                            {
                                // Current objective is complete when Complete == 1
                                // Overall objective is complere when Nums is also > 0
                                EncounterManager.StopEncounter();
                            }
                            else if (target.Value.Complete == 0 && target.Value.Nums == 0)
                            {
                                // We got a new objective, either advanced the phase or reset... or advanced and the devs are trolling with too many states
                                EncounterManager.StopEncounter();
                                EncounterManager.StartEncounter(true);
                            }
                        }
                    }

                    // Since people may never open this window, let's ensure the list doesn't just grow forever
                    if (BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.Count() > 100)
                    {
                        BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.TryDequeue(out _);
                    }

                    BattleStateMachine.DungeonTargetDataHistoryAdd(target.Value);
                    BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.Enqueue(new KeyValuePair<int, DungeonTargetData>(target.Key, target.Value));
                }
            }

            return;

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
                    DungeonSyncData.TargetFieldNumber, dungeonSyncData =>
                    {
                        var targetDataFuncs = new Dictionary<int, Action<BinaryReader>>
                        {
                            {
                                DungeonTarget.TargetDataFieldNumber, dungeonTarget =>
                                {
                                    int add = dungeonTarget.ReadInt32();
                                    _ = dungeonTarget.ReadInt32();
                                    int remove = 0;
                                    int update = 0;
                                    if (add == -4)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData.add={add} (Early Exit)");
                                        return;
                                    }
                                    if (add == -1)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData.add={add} (Get New Value)");
                                        add = dungeonTarget.ReadInt32();
                                    }
                                    else
                                    {
                                        remove = dungeonTarget.ReadInt32();
                                        _ = dungeonTarget.ReadInt32();
                                        update = dungeonTarget.ReadInt32();
                                        _ = dungeonTarget.ReadInt32();
                                    }
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData.add={add}");
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData.remove={remove}");
                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData.update={update}");

                                    Dictionary<int, DungeonTargetData> targetData = new();

                                    for (int i = 0; i < add; i++)
                                    {
                                        int dk = dungeonTarget.ReadInt32();
                                        _ = dungeonTarget.ReadInt32();

                                        DungeonTargetData dv = new();

                                        var targetDataDataFuncs = new Dictionary<int, Action<BinaryReader>>
                                        {
                                            {
                                                DungeonTargetData.TargetIdFieldNumber, dungeonTargetDataData =>
                                                {
                                                    var targetId = dungeonTargetDataData.ReadInt32();
                                                    dungeonTargetDataData.ReadInt32();

                                                    dv.TargetId = targetId;
                                                }
                                            },
                                            {
                                                DungeonTargetData.NumsFieldNumber, dungeonTargetDataData =>
                                                {
                                                    var nums = dungeonTargetDataData.ReadInt32();
                                                    dungeonTargetDataData.ReadInt32();

                                                    dv.Nums = nums;
                                                }
                                            },
                                            {
                                                DungeonTargetData.CompleteFieldNumber, dungeonTargetDataData =>
                                                {
                                                    var complete = dungeonTargetDataData.ReadInt32();
                                                    dungeonTargetDataData.ReadInt32();

                                                    dv.Complete = complete;
                                                }
                                            }
                                        };

                                        ReadBinaryContainer(dungeonTarget, targetDataDataFuncs, "DungeonTargetDataData");

                                        targetData.Add(dk, dv);
                                    }
                                    for (int i = 0; i < remove; i++)
                                    {
                                        int dk = dungeonTarget.ReadInt32();
                                        _ = dungeonTarget.ReadInt32();

                                        if (!targetData.Remove(dk))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData did not find key to remove ({dk})");
                                        }
                                    }
                                    for (int i = 0; i < update; i++)
                                    {
                                        int dk = dungeonTarget.ReadInt32();
                                        _ = dungeonTarget.ReadInt32();

                                        DungeonTargetData dv = new();

                                        var targetDataDataFuncs = new Dictionary<int, Action<BinaryReader>>
                                        {
                                            {
                                                DungeonTargetData.TargetIdFieldNumber, dungeonTargetDataData =>
                                                {
                                                    var targetId = dungeonTargetDataData.ReadInt32();
                                                    dungeonTargetDataData.ReadInt32();

                                                    dv.TargetId = targetId;
                                                }
                                            },
                                            {
                                                DungeonTargetData.NumsFieldNumber, dungeonTargetDataData =>
                                                {
                                                    var nums = dungeonTargetDataData.ReadInt32();
                                                    dungeonTargetDataData.ReadInt32();

                                                    dv.Nums = nums;
                                                }
                                            },
                                            {
                                                DungeonTargetData.CompleteFieldNumber, dungeonTargetDataData =>
                                                {
                                                    var complete = dungeonTargetDataData.ReadInt32();
                                                    dungeonTargetDataData.ReadInt32();

                                                    dv.Complete = complete;
                                                }
                                            }
                                        };

                                        ReadBinaryContainer(dungeonTarget, targetDataDataFuncs, "DungeonTargetDataData");

                                        if (targetData.ContainsKey(dk))
                                        {
                                            targetData[dk] = dv;
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData did not find key to update ({dk})");
                                            targetData.Add(dk, dv);
                                        }
                                    }

                                    foreach (var dataItem in targetData)
                                    {
                                        //BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.Enqueue(dataItem);
                                    }

                                    System.Diagnostics.Debug.WriteLine($"syncDungeonDirtyData.DungeonTarget.TargetData finished");
                                }
                            }
                        };

                        ReadBinaryContainer(dungeonSyncData, targetDataFuncs, "DungeonTarget");
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
    }
}
