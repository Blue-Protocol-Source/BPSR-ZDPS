using BPSR_ZDPSLib;
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
using BPSR_ZDPS.Windows;

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
                CaptureDeviceName = Settings.Instance.NetCaptureDeviceName,
                ExeNames = Utils.GameCapturePreferenceToExeNames(Settings.Instance.GameCapturePreference)
            });

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncContainerData, ProcessSyncContainerData);
            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncContainerDirtyData, ProcessSyncContainerDirtyData);

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncNearDeltaInfo, ProcessSyncNearDeltaInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncToMeDeltaInfo, ProcessSyncToMeDeltaInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncNearEntities, ProcessSyncNearEntities);

            netCap.RegisterNotifyHandler(936649811, (uint)BPSR_ZDPSLib.ServiceMethods.WorldActivityNtf.SyncHitInfo, ProcessSyncHitInfo);

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncDungeonData, ProcessSyncDungeonData);

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.SyncDungeonDirtyData, ProcessSyncDungeonDirtyData);

            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.NotifyAllMemberReady, ProcessNotifyAllMemberReady);
            netCap.RegisterWorldNotifyHandler(BPSR_ZDPSLib.ServiceMethods.WorldNtf.NotifyCaptainReady, ProcessNotifyCaptainReady);

            netCap.RegisterMatchNotifyHandler(BPSR_ZDPSLib.ServiceMethods.MatchNtf.EnterMatchResult, ProcessEnterMatchResult);
            netCap.RegisterMatchNotifyHandler(BPSR_ZDPSLib.ServiceMethods.MatchNtf.CancelMatchResult, ProcessCancelMatchResult);
            netCap.RegisterMatchNotifyHandler(BPSR_ZDPSLib.ServiceMethods.MatchNtf.MatchReadyStatus, ProcessMatchReadyStatus);

            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NoticeUpdateTeamInfo, ProcessNoticeUpdateTeamInfo);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NoticeUpdateTeamMemberInfo, ProcessNoticeUpdateTeamMemberInfo);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyJoinTeam, ProcessNotifyJoinTeam);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyLeaveTeam, ProcessNotifyLeaveTeam);
            //
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyBeTransferLeader, ProcessNotifyBeTransferLeader);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NoticeTeamDissolve, ProcessNoticeTeamDissolve);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyTeamActivityState, ProcessNotifyTeamActivityState);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.TeamActivityResult, ProcessTeamActivityResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.TeamActivityListResult, ProcessTeamActivityListResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.TeamActivityVoteResult, ProcessTeamActivityVoteResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyCharMatchResult, ProcessNotifyCharMatchResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyTeamMatchResult, ProcessNotifyTeamMatchResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyCharAbortMatch, ProcessNotifyCharAbortMatch);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.UpdateTeamMemBeCall, ProcessUpdateTeamMemBeCall);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyTeamMemBeCall, ProcessNotifyTeamMemBeCall);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyTeamMemBeCallResult, ProcessNotifyTeamMemBeCallResult);
            netCap.RegisterNotifyHandler((ulong)EServiceId.GrpcTeamNtf, (uint)BPSR_ZDPSLib.ServiceMethods.GrpcTeamNtf.NotifyTeamEnterErr, ProcessNotifyTeamEnterErr);

            netCap.RegisterNotifyHandler((ulong)EServiceId.ChitChatNtf, (uint)BPSR_ZDPSLib.ServiceMethods.ChitChatNtf.NotifyNewestChitChatMsgs, Managers.ChatManager.ProcessChatMessage);

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

        public static void ProcessNotifyJoinTeam(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNotifyJoinTeam");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyJoinTeam.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNotifyJoinTeam(vData, extraData);
        }

        public static void ProcessNotifyLeaveTeam(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNotifyLeaveTeam");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyLeaveTeam.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNotifyLeaveTeam(vData, extraData);
        }

        public static void ProcessNotifyBeTransferLeader(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNotifyBeTransferLeader");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NotifyBeTransferLeader.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNotifyBeTransferLeader(vData, extraData);
        }

        public static void ProcessNoticeTeamDissolve(ReadOnlySpan<byte> payloadBuffer, ExtraPacketData extraData)
        {
            //System.Diagnostics.Debug.WriteLine("ProcessNoticeTeamDissolve");
            if (payloadBuffer.Length == 0)
            {
                return;
            }

            var vData = GrpcTeamNtf.Types.NoticeTeamDissolve.Parser.ParseFrom(payloadBuffer);

            if (vData == null)
            {
                return;
            }

            GrpcTeamManager.ProcessNoticeTeamDissolve(vData, extraData);
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
                        string name = reader.ReadString();
                        EncounterManager.Current.SetName(uuid, name);
                        EncounterManager.Current.SetAttrKV(uuid, "AttrName", name);
                        break;
                    case EAttrType.AttrSkillId:
                        {
                            string attr_name_id = ((EAttrType)attr.Id).ToString();
                            int skillId = reader.ReadInt32();

                            EncounterManager.Current.SetAttrKV(uuid, attr_name_id, skillId);
                            break;
                        }
                    case EAttrType.AttrProfessionId:
                        int professionId = reader.ReadInt32();
                        EncounterManager.Current.SetProfessionId(uuid, professionId);
                        EncounterManager.Current.SetAttrKV(uuid, "AttrProfessionId", professionId);
                        break;
                    case EAttrType.AttrFightPoint:
                        int fightPoint = reader.ReadInt32();
                        EncounterManager.Current.SetAbilityScore(uuid, fightPoint);
                        EncounterManager.Current.SetAttrKV(uuid, "AttrFightPoint", fightPoint);
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
                    case EAttrType.AttrShieldList:
                        {
                            List<ShieldInfo> shieldList = new();
                            while (!reader.IsAtEnd)
                            {
                                int len = reader.ReadLength();

                                ShieldInfo shield = new();

                                reader.ReadMessage(shield);
                                shieldList.Add(shield);
                            }
                            EncounterManager.Current.SetAttrKV(uuid, "AttrShieldList", shieldList);
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
                    case EAttrType.AttrHateList:
                        List<HateInfo> hateList = new();
                        while (!reader.IsAtEnd)
                        {
                            int len = reader.ReadLength();

                            HateInfo hate = new();

                            reader.ReadMessage(hate);
                            hateList.Add(hate);
                        }
                        EncounterManager.Current.SetAttrKV(uuid, "AttrHateList", hateList);
                        break;
                    case EAttrType.AttrSeasonLevel:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSeasonLevel", reader.ReadInt32());
                        break;
                    case EAttrType.AttrSeasonStrength:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSeasonStrength", reader.ReadInt32());
                        break;
                    case EAttrType.AttrSeasonStrengthAdd:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSeasonStrengthAdd", reader.ReadInt32());
                        break;
                    case EAttrType.AttrSeasonStrengthTotal:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSeasonStrengthTotal", reader.ReadInt32());
                        break;
                    case EAttrType.AttrSkillLevelIdList:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrSkillLevelIdList", reader.ReadInt32());
                        // TODO: Enable this when we want to track every skill level and tier for players when they appear
                        /*List<Zproto.SkillLevelInfo> skillLevelInfoList = new();
                        while (!reader.IsAtEnd)
                        {
                            int len = reader.ReadLength();

                            SkillLevelInfo info = new();

                            reader.ReadMessage(info);
                            skillLevelInfoList.Add(info);
                        }*/
                        
                        break;
                    case EAttrType.AttrTeamId:
                        EncounterManager.Current.SetAttrKV(uuid, "AttrTeamId", reader.ReadInt64());
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

            bool isTargetPlayer = (Utils.UuidToEntityType(targetUuid) == (long)EEntityType.EntChar);
            long targetUid = Utils.UuidToEntityId(targetUuid);
            var attrCollection = delta.Attrs;

            if (attrCollection?.Attrs != null && attrCollection.Attrs.Any())
            {
                ProcessAttrs(targetUuid, attrCollection.Attrs);
            }

            if (delta.TempAttrs != null && delta.TempAttrs.Attrs.Any())
            {
                //System.Diagnostics.Debug.WriteLine($"delta.TempAttrs.Attrs.count = {delta.TempAttrs.Attrs.Count}");
            }

            long buffBasedShieldBreakValue = 0;
            
            List<int> EventHandledBuffs = new();
            if (delta.BuffEffect != null)
            {
                //System.Diagnostics.Debug.WriteLine($"delta.BuffEffect={delta.BuffEffect.BuffEffects.Count}");
                for (int buffIdx = 0; buffIdx < delta.BuffEffect.BuffEffects.Count; buffIdx++)
                {
                    // Shield buffs appear to use Type == BuffEventAddTo and BuffEventRemove
                    var buffEffect = delta.BuffEffect.BuffEffects[buffIdx];
                    //System.Diagnostics.Debug.WriteLine($"BuffEffect[{buffIdx}] = {buffEffect}");
                    EventHandledBuffs.Add(buffEffect.BuffUuid);
                    //System.Diagnostics.Debug.WriteLine($"BuffEffect: {buffEffect}");
                    // When a buff effect event occurs, a specific buff is being modified by the BuffUuid indicator.
                    // However, the same payload can contain other buffs which are being updated at the same time, such as Layer modifications without their own event.
                    if (buffEffect.LogicEffect != null && buffEffect.LogicEffect.Count > 0)
                    {
                        for (int logicIdx = 0; logicIdx < buffEffect.LogicEffect.Count; logicIdx++)
                        {
                            var logicEffect = buffEffect.LogicEffect[logicIdx];
                            var reader = new Google.Protobuf.CodedInputStream(logicEffect.RawData.ToByteArray());
                            if (logicEffect.EffectType == EBuffEffectLogicPbType.BuffEffectAddBuff)
                            {
                                var buffInfo = BuffInfo.Parser.ParseFrom(reader);
                                //System.Diagnostics.Debug.WriteLine($"({buffEffect.Type}) buffEffect[{buffIdx}].logicEffect[{logicIdx}] (Type:{logicEffect.EffectType}) = (BuffUUID:{buffEffect.BuffUuid}){buffInfo}");
                                EncounterManager.Current.NotifyBuffEvent(targetUuid, buffEffect.Type, buffEffect.BuffUuid, buffInfo.BaseId, buffInfo.Level, buffInfo.FireUuid, buffInfo.Layer, buffInfo.Duration, buffInfo.FightSourceInfo.SourceConfigId, extraData);
                            }
                            else if (logicEffect.EffectType == EBuffEffectLogicPbType.BuffEffectBuffChange)
                            {
                                // Layer, Duration, CreateTime
                                var changeInfo = BuffChange.Parser.ParseFrom(reader);
                                //System.Diagnostics.Debug.WriteLine($"({buffEffect.Type}) buffEffect[{buffIdx}].logicEffect[{logicIdx}] (Type:{logicEffect.EffectType}) = (BuffUUID:{buffEffect.BuffUuid}){changeInfo}");
                                EncounterManager.Current.NotifyBuffEvent(targetUuid, buffEffect.Type, buffEffect.BuffUuid, 0, 0, 0, changeInfo.Layer, (int)changeInfo.Duration, 0, extraData);
                            }
                            else if (logicEffect.EffectType == null)
                            {
                                //System.Diagnostics.Debug.WriteLine($"Unhandled Logic Effect {buffEffect}");
                                EncounterManager.Current.NotifyBuffEvent(targetUuid, buffEffect.Type, buffEffect.BuffUuid, 0, 0, 0, 0, 0, 0, extraData);
                            }
                        }
                    }
                    else
                    {
                        // Most commonly appears to include EBuffEventType.BuffEventRemove, EBuffEventType.BuffEventAddTo, EBuffEventType.BuffEventRemoveLayer
                        //System.Diagnostics.Debug.WriteLine($"No Logic Effect {buffEffect}");
                        EncounterManager.Current.NotifyBuffEvent(targetUuid, buffEffect.Type, buffEffect.BuffUuid, 0, 0, 0, 0, 0, 0, extraData);
                    }

                    if (buffEffect.Type == EBuffEventType.BuffEventRemove)
                    {
                        if (EncounterManager.Current.Entities.TryGetValue(targetUuid, out var targetEntity))
                        {
                            List<ShieldInfo>? attrShieldList = targetEntity.GetAttrKV("AttrShieldList") as List<ShieldInfo>;

                            if (attrShieldList != null)
                            {
                                var matches = attrShieldList.Where(x => x.Uuid == buffEffect.BuffUuid);
                                if (matches.Any())
                                {
                                    // Shield is being removed from this Damage instance, we can consider the previously remaining Value to be how much was mitigated with (Shield Breakthrough)
                                    // If no damage events occur against the target in this event then the Shield expired instead of being removed via Breakthrough

                                    var match = matches.First();

                                    buffBasedShieldBreakValue = match.Value;

                                    attrShieldList.Remove(match);

                                    // Remove the old shields now that the buff containing it is gone
                                    targetEntity.SetAttrKV("AttrShieldList", attrShieldList);
                                }
                            }
                        }
                    }
                }
            }

            var skillEffect = delta.SkillEffects;
            
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
                bool isAttackerPlayer = (Utils.UuidToEntityType(attackerUuid) == (long)EEntityType.EntChar);

                if (syncDamageInfo.TopSummonerId != 0)
                {
                    if (EncounterManager.Current.Entities.TryGetValue(syncDamageInfo.AttackerUuid, out var summonedEntity))
                    {
                        EncounterManager.Current.UpdateCasterSkillTierLevel(syncDamageInfo.TopSummonerId, summonedEntity);
                    }
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
                // Note: There are some rare cases where an Immune event occurs but the damage is not 0, HpLessen however will be null

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

                // Handle rewriting the event to account for Shield Breakthrough
                long shieldBreak = 0;
                if (syncDamageInfo.Type == EDamageType.Absorbed)
                {
                    shieldBreak = damage;
                }
                else if (buffBasedShieldBreakValue > 0 && targetUuid != attackerUuid)
                {
                    if (hpLessen >= buffBasedShieldBreakValue)
                    {
                        // This will be the only time both HpLessen and Value exist on an Absorbed "event"
                        syncDamageInfo.Type = EDamageType.Absorbed;

                        // Lower the HP modified to being the true HP change rather than keeping the Shield Damage in it like the game normally does
                        hpLessen = hpLessen - buffBasedShieldBreakValue;
                        shieldBreak = buffBasedShieldBreakValue;
                    }
                    else
                    {
                        // This should not be possible
                        //Log.Warning($"TargetUid {targetUid} (UUID:{targetUuid}) hit by Skill Id ({skillId}) had greater Shield Breakthrough than HP modified! Damage: {damage}, HpLessen: {hpLessen}, Breakthrough: {buffBasedShieldBreakValue}");
                        shieldBreak = 0;
                    }

                    buffBasedShieldBreakValue = 0;
                }

                if (AppState.IsBenchmarkMode)
                {
                    if (isAttackerPlayer && attackerUuid != AppState.PlayerUUID)
                    {
                        // Only record benchmarking player related details
                        // All Monsters and other non-player entities will be processed still
                        continue;
                    }
                    else if (isAttackerPlayer && attackerUuid == AppState.PlayerUUID)
                    {
                        if (!AppState.HasBenchmarkBegun)
                        {
                            AppState.HasBenchmarkBegun = true;
                            // This will automatically restart our Encounter Start Time to handle the start of the Benchmark Encounter
                            EncounterManager.StartEncounter(false, EncounterStartReason.BenchmarkStart);
                        }

                        if (AppState.BenchmarkSingleTarget && Utils.UuidToEntityType(targetUuid) == (long)EEntityType.EntMonster && !isHeal)
                        {
                            if (AppState.BenchmarkSingleTargetUUID == 0)
                            {
                                AppState.BenchmarkSingleTargetUUID = targetUuid;
                            }

                            // Only care about recording details for our first hit target
                            if (targetUuid != AppState.BenchmarkSingleTargetUUID)
                            {
                                continue;
                            }
                        }
                    }
                }

                if (isHeal)
                {
                    EncounterManager.Current.AddHealing((isAttackerPlayer ? attackerUuid : 0), targetUuid, skillId, syncDamageInfo.OwnerLevel, damage, hpLessen, shieldBreak, syncDamageInfo.Property, syncDamageInfo.Type, syncDamageInfo.DamageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, syncDamageInfo.DamagePos, extraData);
                }
                else
                {
                    EncounterManager.Current.AddDamage(attackerUuid, targetUuid, skillId, syncDamageInfo.OwnerLevel, damage, hpLessen, shieldBreak, syncDamageInfo.Property, syncDamageInfo.Type, syncDamageInfo.DamageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, syncDamageInfo.DamagePos, extraData);

                    EncounterManager.Current.AddTakenDamage(attackerUuid, targetUuid, skillId, syncDamageInfo.OwnerLevel, damage, hpLessen, shieldBreak, syncDamageInfo.Property, syncDamageInfo.Type, syncDamageInfo.DamageMode, isCrit, isLucky, isCauseLucky, isMiss, isDead, syncDamageInfo.DamagePos, extraData);
                }

                buffBasedShieldBreakValue = 0;
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

            ModuleSolver.SetPlayerInv(vData);

            long playerUuid = Utils.EntityIdToUuid(vData.CharId, (long)EEntityType.EntChar, false, false);

            AppState.PlayerUID = vData.CharId;
            AppState.AccountId = vData.CharBase.AccountId;
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

                if (vData.CharBase.TeamInfo.TeamId != 0)
                {
                    System.Diagnostics.Debug.WriteLine("vData.CharBase.TeamInfo.TeamId != 0");
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

            var seasonRoleLevelData = vData.SeasonRoleLevelData;
            if (seasonRoleLevelData != null)
            {
                var lastSeason = seasonRoleLevelData.SeasonRoleLevelMap.LastOrDefault();
                if (lastSeason.Value != null)
                {
                    EncounterManager.Current.SetAttrKV(playerUuid, "AttrSeasonLevel", lastSeason.Value.Level);
                }
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
                var ser = new BPSR_ZDPSLib.Blobs.CharSerialize(new BlobReader(buf));

                if (ser.CharBaseInfo != null)
                {
                    if (!string.IsNullOrEmpty(ser.CharBaseInfo.Name))
                    {
                        EncounterManager.Current.SetName(currentUserUuid, ser.CharBaseInfo.Name);
                        AppState.PlayerName = ser.CharBaseInfo.Name;
                    }
                    if (!string.IsNullOrEmpty(ser.CharBaseInfo.AccountId) && string.IsNullOrEmpty(AppState.AccountId))
                    {
                        AppState.AccountId = ser.CharBaseInfo.AccountId;
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
                    Log.Debug($"ser.sceneData = {ser.SceneData}");
                    if (ser.SceneData.LevelMapId != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ser.SceneData.LevelMapId = {ser.SceneData.LevelMapId})");
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

            if (vData.DungeonSceneInfo != null)
            {
                EncounterManager.Current.SetDungeonDifficulty(vData.DungeonSceneInfo.Difficulty);
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

            if (vData.TimerInfo != null)
            {
                System.Diagnostics.Debug.WriteLine($"TimerInfo = {vData.TimerInfo.Type}, {vData.TimerInfo.StartTime}, {vData.TimerInfo.DungeonTimes}, {vData.TimerInfo.Direction}, {vData.TimerInfo.Index}, {vData.TimerInfo.ChangeTime}, {vData.TimerInfo.EffectType}, {vData.TimerInfo.PauseTime}, {vData.TimerInfo.PauseTotalTime}, {vData.TimerInfo.OutLookType}");
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
                if (EncounterManager.Current.IsWipe)
                {
                    // This Encounter already has been reported as a wipe and should be in the processing of ending already
                    return;
                }

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

                // TODO: Currently a local player state change can incorrectly report a wipe pattern in rare cases where it wasn't an actual wipe
                // An additional check needs to be performed to help validate the wipe, or overturn the result if other players weren't also dead/respawning
                
                // This Encounter-wide status check can help ensure real wipes are found, but does not overturn a local player state match a wipe pattern
                // The follow-up Boss HP check _should hopefully_ protect against the vast majority of incorrect player state pattern matches
                if (EncounterManager.Current.HasStatsBeenRecorded())
                {
                    var characterList = EncounterManager.Current.Entities.AsValueEnumerable().Where(x => x.Value.EntityType == EEntityType.EntChar);
                    bool areAllCharactersDead = true;
                    foreach (var character in characterList)
                    {
                        var charState = character.Value.GetAttrKV("AttrState");
                        if (charState != null)
                        {
                            if ((EActorState)charState != EActorState.ActorStateDead && character.Value.Hp > 0)
                            {
                                areAllCharactersDead = false;
                            }
                        }
                        else if (character.Value.Hp > 0 || character.Value.MaxHp == 0)
                        {
                            areAllCharactersDead = false;
                        }
                    }
                    if (areAllCharactersDead && !isStateWipePattern)
                    {
                        Log.Debug($"All characters were reported as actively dead in current Encounter. Overriding isStateWipePattern to true.");
                        isStateWipePattern = true;
                    }
                    if (Settings.Instance.AllowWipeRecalculationOverwriting && !areAllCharactersDead && isStateWipePattern)
                    {
                        Log.Debug($"Not all characters were reported as actively dead in current Encounter. Overriding isStateWipePattern to false.");
                        isStateWipePattern = false;
                    }
                }

                //System.Diagnostics.Debug.WriteLine($"useNoTeleportWipePattern == {useNoTeleportWipePattern} && isStateWipePattern == {isStateWipePattern}");

                if (isStateWipePattern)
                {
                    // The player state is in a wipe pattern
                    // Check if there is a Boss type monster
                    var bosses = EncounterManager.Current.Entities.AsValueEnumerable().Where(x => x.Value.MonsterType == EMonsterType.Boss);
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

            var dun = new BPSR_ZDPSLib.Blobs.DungeonDirtyData(new BlobReader(buf));

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
                if (dun.TimerInfo.EffectType == EDungeonTimerEffectType.Sub && dun.TimerInfo.ChangeTime != null)
                {
                    EncounterManager.Current.ExData.DungeonTimeDeathChange += (int)dun.TimerInfo.ChangeTime;
                }
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

                    // Since people may never open this window, let's ensure the list doesn't just grow forever
                    if (BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.Count() > 100)
                    {
                        BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.TryDequeue(out _);
                    }

                    BattleStateMachine.DungeonTargetDataHistoryAdd(target.Value);
                    BPSR_ZDPS.Windows.DebugDungeonTracker.DungeonTargetDataTracker.Enqueue(new KeyValuePair<int, DungeonTargetData>(target.Key, target.Value));
                }
            }
        }
    }
}
