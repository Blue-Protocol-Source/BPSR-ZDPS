using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.External;
using BPSR_ZDPS.Web;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Managers.External
{
    public static class BPTimerManager
    {
        const int REPORT_HP_INTERVAL = 5;
        const string HOST = "https://db.bptimer.com";
        const string API_KEY = "";

        static BPTimerHpReport? LastSentRequest = null;

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
            var canReport = hpPct % REPORT_HP_INTERVAL == 0 && LastSentRequest?.HpPct != hpPct;

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

                WebManager.SubmitBPTimerRequest(report, $"{HOST}/api/create-hp-report", API_KEY);
            }
        }
    }
}
