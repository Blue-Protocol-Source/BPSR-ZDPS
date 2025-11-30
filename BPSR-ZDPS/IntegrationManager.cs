using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

namespace BPSR_ZDPS
{
    public static class IntegrationManager
    {
        public static void InitBindings()
        {
            EncounterManager.EncounterEndFinal += EncounterManager_EncounterEndFinal;

            System.Diagnostics.Debug.WriteLine("IntegrationManager InitBindings");
        }

        private static void EncounterManager_EncounterEndFinal(EncounterEndFinalData e)
        {
            // Only create reports when there is a boss in the encounter and it is dead or the encounter is a wipe
            if (EncounterManager.Current.BossUUID > 0 && (EncounterManager.Current.BossHpPct == 0 || EncounterManager.Current.IsWipe))
            {
                System.Diagnostics.Debug.WriteLine("IntegrationManager is creating an Encounter Report.");
                HelperMethods.DeferredImGuiRenderAction = () =>
                {
                    var img = ReportImgGen.CreateReportImg(EncounterManager.Current);
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
                                        WebManager.SubmitReportToWebhook(EncounterManager.Current, img, Settings.Instance.WebhookReportsDiscordUrl);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("IntegrationManager could not send report to Discord, URL was not set.");
                                    }
                                    break;
                                case EWebhookReportsMode.Custom:
                                    if (!string.IsNullOrEmpty(Settings.Instance.WebhookReportsCustomUrl))
                                    {
                                        WebManager.SubmitReportToWebhook(EncounterManager.Current, img, Settings.Instance.WebhookReportsCustomUrl);
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
            }
        }
    }
}
