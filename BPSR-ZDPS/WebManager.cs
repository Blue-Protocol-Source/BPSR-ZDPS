using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Hashing;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS.Web
{
    public class WebManager
    {
        private static HttpClient HttpClient = new HttpClient();

        public static void SubmitReportToWebhook(Encounter encounter, Image<Rgba32> img, string webhookUrl)
        {
            try
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    var teamId = CreateTeamId(encounter);
                    var msg = CreateDiscordMessage(encounter, teamId);
                    var msgJson = JsonConvert.SerializeObject(msg, Formatting.Indented);

                    using var imgMs = new MemoryStream();
                    img.SaveAsPng(imgMs);
                    imgMs.Flush();
                    imgMs.Position = 0;

                    using var form = new MultipartFormDataContent();
                    form.Add(new StringContent(msgJson, Encoding.UTF8, "application/json"), "payload_json");

                    var fileContent = new StreamContent(imgMs);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    form.Add(fileContent, "report", "report.png");
                    form.Headers.Add("X-ZDPS-TeamID", $"{teamId}");

                    string url = "";
                    if (Settings.Instance.WebhookReportsMode == EWebhookReportsMode.DiscordDeduplication)
                    {
                        // Construct url for going to the deduplication server
                        var discordWebHookInfo = Utils.SplitAndValidateDiscordWebhook(Settings.Instance.WebhookReportsDiscordUrl);
                        url = $"{Settings.Instance.WebhookReportsDeduplicationServerUrl}/report/discord/{discordWebHookInfo.Value.id}/{discordWebHookInfo.Value.token}";
                    }
                    else if (Settings.Instance.WebhookReportsMode == EWebhookReportsMode.Discord)
                    {
                        // Directly send to Discord
                        url = $"{webhookUrl}";
                    }
                    else if (Settings.Instance.WebhookReportsMode == EWebhookReportsMode.Custom)
                    {
                        // Directly send to Custom URL
                        url = $"{webhookUrl}";
                    }

                    var response = await HttpClient.PostAsync(url, form);

                    Log.Information($"SubmitReportToWebhook: StatusCode: {response.StatusCode}");
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SubmitReportToWebhook Error");
            }
        }

        public static ulong CreateTeamId(Encounter encounter)
        {
            var hash = new XxHash64();
            var playerIds = encounter.Entities.AsValueEnumerable()
                .Where(x => x.Value.EntityType == EEntityType.EntChar)
                .Select(x => x.Value.UUID)
                .Order();

            foreach (var id in playerIds)
            {
                hash.Append(MemoryMarshal.Cast<long, byte>([id]));
            }

            var hashUlong = hash.GetCurrentHashAsUInt64();

            return hashUlong;
        }

        private static DiscordWebhookPayload CreateDiscordMessage(Encounter encounter, ulong teamId)
        {
            var unixStartTime = new DateTimeOffset(encounter.StartTime).ToUnixTimeSeconds();

            string encounterName = $"**Encounter**:{(encounter.IsWipe ? " `Wipe`" : "")} {encounter.SceneName}{(!string.IsNullOrEmpty(encounter.SceneSubName) ? $" - {encounter.SceneSubName}" : "")}";
            string bossDetails = $"{(!string.IsNullOrEmpty(encounter.BossName) ? $"**Boss**: {encounter.BossName}{(encounter.BossHpPct > 0 ? $" ({Math.Round(encounter.BossHpPct / 1000.0f, 2)}%)" : "")}" : "")}";

            var msgContentBuilder = new StringBuilder();
            msgContentBuilder.AppendLine("**ZDPS Report**");
            msgContentBuilder.AppendLine($"**Reporter**: {AppState.PlayerName}");
            msgContentBuilder.AppendLine(encounterName);
            if (!string.IsNullOrEmpty(bossDetails))
            {
                msgContentBuilder.AppendLine(bossDetails);
            }
            msgContentBuilder.AppendLine($"**Started At**: <t:{unixStartTime}:F> <t:{unixStartTime}:R>");
            msgContentBuilder.AppendLine($"**Duration**: {(encounter.EndTime - encounter.StartTime).ToString(@"hh\:mm\:ss")}");
            msgContentBuilder.AppendLine($"**TeamID**: ``{teamId}``");

            var msg = new DiscordWebhookPayload("ZDPS", msgContentBuilder.ToString())
            {
                AvatarURL = "https://media.discordapp.net/attachments/1443057617113977015/1444260874784084008/co25l4.jpg?ex=692c1041&is=692abec1&hm=75449222af948cba198474a8e580e9e5e12a7f8bbd546935aeeec00d8ba7cb2d&=&format=webp"
            };

            return msg;
        }
    }
}
