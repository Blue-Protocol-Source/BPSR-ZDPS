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
                var task = Task.Factory.StartNew(() =>
                {
                    var teamId = CreateTeamId(encounter);
                    var discordWebHookInfo = Utils.SplitAndValidateDiscordWebhook(Settings.Instance.WebHookDiscordUrl);
                    var reportData = new EncounterReport()
                    {
                        TeamID = teamId,
                        Payload = "{\r\n  \"embeds\": [\r\n    {\r\n      \"title\": \"ZDPS Report\",\r\n      \"description\": \"**Encounter:** The Fallen Tower\\n**Time:** 12:45\",\r\n      \"color\": 10412141,\r\n      \"fields\": [\r\n        {\r\n          \"name\": \"Player\",\r\n          \"value\": \"Evie\\nZoey\\nLuna\",\r\n          \"inline\": true\r\n        },\r\n        {\r\n          \"name\": \"DPS Contribution\",\r\n          \"value\": \"██████████ 100%\\n████████  92%\\n██████    69%\",\r\n          \"inline\": true\r\n        },\r\n        {\r\n          \"name\": \"DPS | HPS | Taken\",\r\n          \"value\": \" 7520 | 1820 | 24110\\n 6930 |  210 | 18335\\n 5210 | 2940 |  9880\",\r\n          \"inline\": true\r\n        }\r\n      ]\r\n    }\r\n  ]\r\n}\r\n",
                        DiscordWebhookId = discordWebHookInfo.Value.id,
                        DiscordWebhookToken = discordWebHookInfo.Value.token,
                    };

                    using var imgMs = new MemoryStream();
                    img.SaveAsPng(imgMs);
                    imgMs.Flush();
                    imgMs.Position = 0;

                    var reportJson = JsonConvert.SerializeObject(reportData);

                    using var form = new MultipartFormDataContent();
                    form.Add(new StringContent(reportJson, Encoding.UTF8, "application/json"), "report");

                    var fileContent = new StreamContent(imgMs);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    form.Add(fileContent, "img", "img.png");

                    var url = $"{Settings.Instance.WebHookServerUrl}/report/discord";
                    var response = HttpClient.PostAsync(url, form);

                    Log.Information($"SubmitReportToDiscordWebhook: Status: {response.Status}, StatusCode: {response.Result.StatusCode}");
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
    }

    public class EncounterReport
    {
        public ulong TeamID { get; set; } = 0;
        public string Payload { get; set; } = "";
        public string DiscordWebhookId { get; set; } = "";
        public string DiscordWebhookToken { get; set; } = "";
    }
}
