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

        public static void SubmitReportToDiscordWebhook(Encounter encounter, Image<Rgba32> img, string discordWebHookUrl)
        {
            try
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    var teamId = CreateTeamId(encounter);
                    var discordWebHookInfo = Utils.SplitAndValidateDiscordWebhook(Settings.Instance.WebHookDiscordUrl);
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
                    form.Add(fileContent, "img", "img.png");
                    form.Headers.Add("X-ZDPS-TeamID", $"{teamId}");

                    var url = $"{Settings.Instance.WebHookServerUrl}/report/discord/{discordWebHookInfo.Value.id}/{discordWebHookInfo.Value.token}";
                    var response = await HttpClient.PostAsync(url, form);

                    Log.Information($"SubmitReportToDiscordWebhook: StatusCode: {response.StatusCode}");
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SubmitReportToDiscordWebhook Error");
            }
        }

        private static ulong CreateTeamId(Encounter encounter)
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
            var msgContent =
                $"""
                **ZDPS Report**
                **Encounter**: {encounter.SceneName}
                **Started At**: <t:{unixStartTime}:F> <t:{unixStartTime}:R>
                **Duration**: {(encounter.EndTime - encounter.StartTime).ToString(@"hh\:mm\:ss")}
                **TeamID**: ``{teamId}``
                """;

            var msg = new DiscordWebhookPayload("ZDPS", msgContent)
            {
                AvatarURL = "https://media.discordapp.net/attachments/1443057617113977015/1444260874784084008/co25l4.jpg?ex=692c1041&is=692abec1&hm=75449222af948cba198474a8e580e9e5e12a7f8bbd546935aeeec00d8ba7cb2d&=&format=webp"
            };

            return msg;
        }
    }
}
