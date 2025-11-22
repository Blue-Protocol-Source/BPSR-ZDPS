using BPSR_DeepsLib;
using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Zproto;
using static Zproto.WorldNtfCsharp.Types;

namespace BPSR_Deeps;

class Program
{
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        //Utils.GetTCPConnectionsForExe(["BPSR", "BPSR_STEAM"]);

        for (int i = 0; i < 200; i++)
        {
            var sw = Stopwatch.StartNew();
            //Process.GetProcessesByName("BPSR");
            sw.Stop();
            //Log.Information("GetProcessesByName took: {time}ms", sw.ElapsedMilliseconds);
        }

        for (int i = 0; i < 200; i++)
        {
            var sw3 = Stopwatch.StartNew();
            //var process2 = Process.GetProcessById(106200);
            sw3.Stop();
            //Log.Information("GetProcessesByID took: {time}ms", sw3.ElapsedMilliseconds);
        }

        try
        {
            Log.Information("Application starting up");

            var netCap = new NetCap();
            netCap.Init(new NetCapConfig()
            {
                CaptureDeviceName = "\\Device\\NPF_{40699DEA-27A5-4985-ADC0-B00BADABAAEB}"
            });

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncNearDeltaInfo, (span, extraData) =>
            {
                ProcessSyncNearDeltaInfo(span);
            });

            netCap.RegisterWorldNotifyHandler(BPSR_DeepsLib.ServiceMethods.WorldNtf.SyncToMeDeltaInfo, (span, extraData) =>
            {
                ProcessSyncToMeDeltaInfo(span);
            });
            
            netCap.Start();

            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "oh no");
            throw;
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
        ulong targetUuid = Shr16(targetUuidRaw);
        var attrCollection = delta.Attrs;
        if (attrCollection?.Attrs != null)
        {
            if (isTargetPlayer)
            {
                // ProcessPlayerAttrs(targetUuidRaw, attrCollection.Attrs);
            }
            else
            {
                // ProcessEnemyAttrs(targetUUidRaw, attrCollection.Attrs);
            }
        }

        var skillEffect = delta.SkillEffects;
        if (skillEffect?.Damages == null || skillEffect.Damages.Count == 0)
        {
            return;
        }

        foreach (var d in skillEffect.Damages)
        {
            long skillId = d.OwnerId;
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
            ulong attackerUuid = Shr16(attackerRaw);

            if (isAttackerPlayer && attackerUuid != 0)
            {
                // var info = GetPlayerBasicInfo(attackerUuid);
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

            if (isTargetPlayer)
            {
                if (isHeal)
                {
                    // AddHealing
                    Log.Logger.Information($"AddHealing({(isAttackerPlayer ? attackerUuid : 0)}, {skillId}, {damageElement}, {hpLessen}, {isLucky}, {isCauseLucky}, {targetUuid})");
                }
                else
                {
                    // AddTakenDamage
                    Log.Logger.Information($"AddTakenDamage({targetUuid}, {skillId}, {damage}, {damageSource}, {isMiss}, {isDead}, {isCrit}, {hpLessen})");
                }
            }
            else
            {
                if (!isHeal && isAttackerPlayer)
                {
                    // AddDamage
                    Log.Logger.Information($"AddDamage({attackerUuid}, {skillId}, {damageElement}, {damage}, {isCrit}, {isLucky}, {isCauseLucky}, {hpLessen})");
                }

                // AddNpcTakenDamage
                Log.Logger.Information($"AddNpcTakenDamage({targetUuid}, {attackerUuid}, {skillId}, {damage}, {isCrit}, {isLucky}, {hpLessen}, {isMiss}, {isDead})");
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
        }
        var aoiSyncDelta = aoiSyncToMeDelta.BaseDelta;
        if (aoiSyncDelta == null)
        {
            return;
        }
        ProcessAoiSyncDelta(aoiSyncDelta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsUuidPlayerRaw(ulong uuidRaw) => (uuidRaw & 0xFFFFUL) == 640UL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong Shr16(ulong v) => v >> 16;
}