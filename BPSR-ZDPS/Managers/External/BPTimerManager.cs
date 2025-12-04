using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.External;
using BPSR_ZDPS.Web;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS.Managers.External
{
    public static class BPTimerManager
    {
        const int REPORT_HP_INTERVAL = 5;
        const string HOST = "https://db.bptimer.com";
        const string API_KEY = "o5he1b5mnykg5mursljw18dixak68h1ue9515dvuthoxtih79w";

        static BPTimerHpReport? LastSentRequest = null;

        static int[] SupportedEntityReportList =
            [ 10007, 10009, 10010, 10018, 10029, 10032, 10056, 10059, 10069, 10077, 10081, 10084, 10085, 10086, 10900, 10901, 10902, 10903, 10904];

        static bool IsEncounterBound = false;

        public static void InitializeBindings()
        {
            System.Diagnostics.Debug.WriteLine("BPTimer InitializeBindings()");

            EncounterManager.EncounterStart += BPTimerManager_EncounterStart;
            EncounterManager.EncounterEndFinal += BPTimerManager_EncounterEndFinal;

            if (EncounterManager.Current != null)
            {
                System.Diagnostics.Debug.WriteLine("BPTimer InitializeBindings is auto-binding EntityHpUpdated");

                IsEncounterBound = true;
                EncounterManager.Current.EntityHpUpdated += BPTimerManager_EntityHpUpdated;
            }
        }

        private static void BPTimerManager_EncounterEndFinal(EncounterEndFinalData e)
        {
            System.Diagnostics.Debug.WriteLine("BPTimerManager_EncounterEndFinal");
            if (IsEncounterBound)
            {
                System.Diagnostics.Debug.WriteLine("BPTimerManager_EncounterEndFinal Actioned");

                IsEncounterBound = false;
                EncounterManager.Current.EntityHpUpdated -= BPTimerManager_EntityHpUpdated;
            }
        }

        private static void BPTimerManager_EncounterStart(EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BPTimerManager_EncounterStart");
            if (!IsEncounterBound)
            {
                System.Diagnostics.Debug.WriteLine("BPTimerManager_EncounterStart Actioned");

                IsEncounterBound = true;
                EncounterManager.Current.EntityHpUpdated += BPTimerManager_EntityHpUpdated;
            }
        }

        private static void BPTimerManager_EntityHpUpdated(object sender, HpUpdatedEventArgs e)
        {
            // Only care about updates while in the Open World
            if (EncounterManager.Current.DungeonState != EDungeonState.DungeonStateNull)
            {
                return;
            }

            var entity = EncounterManager.Current.GetOrCreateEntity(e.EntityUuid);
            var attrId = entity.GetAttrKV("AttrId");
            if (attrId != null && SupportedEntityReportList.Contains((int)attrId))
            {
                SendHpReport(entity, EncounterManager.Current.ChannelLine);
            }
        }

        public static void SendHpReport(Entity entity, uint line)
        {
            if (!Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled)
            {
                return;
            }

            if (!Settings.Instance.External.BPTimerSettings.ExternalBPTimerFieldBossHpReportsEnabled)
            {
                return;
            }

            var hpPct = (int)Math.Round(((double)entity.Hp / (double)entity.MaxHp) * 100.0, 0);
            var canReport = hpPct % REPORT_HP_INTERVAL == 0 && (LastSentRequest?.HpPct != hpPct || LastSentRequest?.MonsterId != entity.UID || LastSentRequest?.Line != line);

            if (string.IsNullOrEmpty(API_KEY))
            {
                Log.Error("Error in BPTimerManager: API_KEY was not set!");
                return;
            }

            if (canReport)
            {
                // We'll assume (0, 0, 0) means no position has been set yet
                bool hasPositionData = entity.Position.Length() != 0.0f;

                long? uid = (Settings.Instance.External.BPTimerSettings.ExternalBPTimerIncludeCharacterId ? AppState.PlayerUID : null);

                var report = new BPTimerHpReport()
                {
                    MonsterId = entity.UID,
                    HpPct = hpPct,
                    Line = line,
                    PosX = hasPositionData ? entity.Position.X : null,
                    PosY = hasPositionData ? entity.Position.Y : null,
                    PosZ = hasPositionData ? entity.Position.Z : null,
                    AccountId = AppState.AccountId,
                    UID = uid
                };

                LastSentRequest = report;

                //System.Diagnostics.Debug.WriteLine($"SendHpReport: {report.MonsterId} {report.HpPct} {report.Line} {report.PosX} {report.PosY} {report.PosZ} {report.AccountId} {report.UID}");
                WebManager.SubmitBPTimerRequest(report, $"{HOST}/api/create-hp-report", API_KEY);
            }
        }
    }
}
