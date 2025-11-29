using BPSR_DeepsServ.Models;
using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace BPSR_DeepsServ
{
    public class DiscordWebHookManager
    {
        private static TimeSpan DupeWindowDuration = TimeSpan.FromSeconds(10);

        private ConcurrentDictionary<ulong, ReportTeamState> ReportDedupeData = [];
        private readonly HttpClient HttpClient = new ();

        public async Task<bool> ProcessEncounterReport(EncounterReport report, IFormFile imgFile)
        {
            if (IsDupe(report))
            {
                return false;
            }

            var sendUrl = GetWebHookUrl(report);
            var msg = CreateReportMessage(report);
            var result = await SendWebhook(sendUrl, msg, imgFile);

            return true;
        }

        private bool IsDupe(EncounterReport report)
        {
            var id = CreateTeamHookReportId(report);
            if (ReportDedupeData.TryGetValue(id, out var data))
            {
                if ((data.ReportedAt + DupeWindowDuration) <= DateTime.Now)
                {
                    ReportDedupeData[id] = new ReportTeamState(DateTime.Now);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            ReportDedupeData[id] = new ReportTeamState(DateTime.Now);
            return false;
        }

        private DiscordWebhookPayload CreateReportMessage(EncounterReport report)
        {
            var embeds = new DiscordWebhookPayload()
            {
                Embeds = [
                    new DiscordEmbed()
                    {
                        Title = "ZDPS Encounter Report",
                        Color = 10412141,
                        Description = $"**Encounter**: {report.EncounterName}\n**Duration**: {report.Duration}",
                        Fields = [
                            new EmbedField()
                            {
                                Name = "Debug",
                                Value = $"TeamID: {report.TeamID}"
                            }
                        ],
                        Image = new EmbedImage()
                        {
                            Url = "attachment://img.png"
                        }
                    }
                ]
            };

            return embeds;
        }

        private ulong CreateTeamHookReportId(EncounterReport report)
        {
            var hash = new XxHash64();
            hash.Append(MemoryMarshal.Cast<ulong, byte>([report.TeamID]));
            hash.Append(Encoding.UTF8.GetBytes(report.DiscordWebhookId));
            hash.Append(Encoding.UTF8.GetBytes(report.DiscordWebhookToken));
            var hashUlong = hash.GetCurrentHashAsUInt64();

            return hashUlong;
        }

        private string GetWebHookUrl(EncounterReport report)
        {
            var url = $"https://discord.com/api/webhooks/{report.DiscordWebhookId}/{report.DiscordWebhookToken}";
            return url;
        }

        private async Task<bool> SendWebhook(string url, DiscordWebhookPayload embed, IFormFile imgFile)
        {
            try
            {
                var json = JsonSerializer.Serialize(embed, AppJsonSerializerContext.Default.DiscordWebhookPayload);

                /*using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var result = await HttpClient.PostAsync(url, content);*/

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "payload_json");

                await using var fileStream = imgFile.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(imgFile.ContentType);
                form.Add(fileContent, "img", "img.png");

                var response = await HttpClient.PostAsync(url, form);

                return true;
            }
            catch (Exception ex)
            {

            }

            return false;
        }
    }
}
