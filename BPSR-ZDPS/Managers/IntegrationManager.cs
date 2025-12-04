using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Managers.External;
using BPSR_ZDPS.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS
{
    public static class IntegrationManager
    {
        public static void InitBindings()
        {
            EncounterManager.EncounterEndFinal += EncounterManager_EncounterEndFinal;

            BPTimerManager.InitializeBindings();

            System.Diagnostics.Debug.WriteLine("IntegrationManager InitBindings");
        }

        private static void EncounterManager_EncounterEndFinal(EncounterEndFinalData e)
        {
            // Only care about encounters with actual data in them
            if (!EncounterManager.Current.HasStatsBeenRecorded())
            {
                return;
            }

            // We do not currently care about creating reports for benchmarks
            if (e.Reason == EncounterStartReason.BenchmarkEnd)
            {
                return;
            }

            // Don't create reports for Null (Open World) states as we don't handle their encounters nicely yet
            if (EncounterManager.Current.DungeonState == EDungeonState.DungeonStateNull)
            {
                return;
            }

            // We perform a check to make sure the setting is above 0 before iterating through the entity list to improve performance for most users who do not set a min count
            if (Settings.Instance.MinimumPlayerCountToCreateReport > 0)
            {
                if (EncounterManager.Current.Entities.Count(x => x.Value.EntityType == EEntityType.EntChar) < Settings.Instance.MinimumPlayerCountToCreateReport)
                {
                    return;
                }
            }

            // TODO: Add setting to only allow creating reports if more than one player exists (or some min count)
            // TODO: (Do this after we correctly track boss/entity hp/state in here) If Reason is not NewObjective, and it's not a wipe, if the boss has HP remaining consider it a "ran out of time" event

            // Only create reports when there is a boss in the encounter and it is dead or the encounter is a wipe
            EncounterManager.Current.Entities.TryGetValue(EncounterManager.Current.BossUUID, out var bossEntity);
            var bossState = bossEntity?.GetAttrKV("AttrState");
            var bossHp = bossEntity?.GetAttrKV("AttrHp");
            var bossMaxHp = bossEntity?.GetAttrKV("AttrMaxHp");
            if (bossEntity != null)
            {
                System.Diagnostics.Debug.WriteLine($"BossAttrHp={bossHp}, BossAttrMaxHp={bossMaxHp}, HpPct={EncounterManager.Current.BossHpPct}");
            }
            if (
                e.Reason == EncounterStartReason.NewObjective ||
                (
                EncounterManager.Current.BossUUID > 0 && (bossState != null && (bossEntity?.Hp == 0 || (EActorState)bossState == EActorState.ActorStateDead) || EncounterManager.Current.IsWipe)
                )
                )
            //if (e.Reason == EncounterStartReason.NewObjective || (EncounterManager.Current.BossUUID > 0 && (EncounterManager.Current.BossHpPct == 0 || EncounterManager.Current.IsWipe)))
            {
                System.Diagnostics.Debug.WriteLine("IntegrationManager is creating an Encounter Report.");
                HelperMethods.DeferredImGuiRenderAction = () =>
                {
                    // Hold onto a reference for the Encounter as once we enter the task it will no longer be the current one and may already be moved into the database
                    var encounter = EncounterManager.Current;

                    var img = ReportImgGen.CreateReportImg(encounter);
                    Task.Factory.StartNew(() =>
                    {
                        if (Settings.Instance.WebhookReportsEnabled)
                        {
                            switch (Settings.Instance.WebhookReportsMode)
                            {
                                case EWebhookReportsMode.DiscordDeduplication:
                                case EWebhookReportsMode.Discord:
                                    if (!string.IsNullOrEmpty(Settings.Instance.WebhookReportsDiscordUrl))
                                    {
                                        WebManager.SubmitReportToWebhook(encounter, img, Settings.Instance.WebhookReportsDiscordUrl);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("IntegrationManager could not send report to Discord, URL was not set.");
                                    }
                                    break;
                                case EWebhookReportsMode.Custom:
                                    if (!string.IsNullOrEmpty(Settings.Instance.WebhookReportsCustomUrl))
                                    {
                                        WebManager.SubmitReportToWebhook(encounter, img, Settings.Instance.WebhookReportsCustomUrl);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("IntegrationManager could not send report to Custom URL, URL was not set.");
                                    }
                                    break;
                            }
                        }
                    });
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"IntegrationManager EncounterEndFinal did not detect a dead boss or wipe in Battle:{e.BattleId} Encounter: {e.EncounterId}.");
                System.Diagnostics.Debug.WriteLine($"BossUUID:{EncounterManager.Current.BossUUID}, BossHpPct:{EncounterManager.Current.BossHpPct}, IsWipe:{EncounterManager.Current.IsWipe}");
                if (bossState != null)
                {
                    System.Diagnostics.Debug.WriteLine($"BossState {(EActorState)bossState}");
                }
            }
        }
    }
}
