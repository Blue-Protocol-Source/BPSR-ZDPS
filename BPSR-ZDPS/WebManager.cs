using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Hashing;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using ZLinq;
using Zproto;
using static System.Net.Mime.MediaTypeNames;

namespace BPSR_ZDPS.Web
{
    public class WebManager
    {
        private static HttpClient HttpClient = new HttpClient();

        public static void SubmitReportToDiscordWebhook(Encounter encounter, Image<Rgba32> img, string discordWebHookUrl)
        {
            try
            {
                var teamId = CreateTeamId(encounter);
                var reportData = new EncounterReport()
                {
                    TeamID = teamId,
                    EncounterName = encounter.SceneName,
                    Duration = encounter.GetDuration()
                };

                var players = encounter.Entities.AsValueEnumerable()
                    .Where(x => x.Value.EntityType == EEntityType.EntChar);

                foreach (var player in players)
                {
                    var partyMember = new PartyMember()
                    {
                        Name = player.Value.Name,
                        CombatScore = (ulong)player.Value.AbilityScore,
                        Dps = player.Value.TotalDamage,
                        Hps = player.Value.TotalHealing,
                        DamageTaken = player.Value.TotalTakenDamage,
                        DamagePct = 0,
                    };

                    reportData.Party.Add(partyMember);
                }

                using var imgMs = new MemoryStream();
                img.SaveAsPng(imgMs);

                var reportJson = JsonConvert.SerializeObject(reportData);

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(reportJson, Encoding.UTF8, "application/json"), "report");

                var fileContent = new StreamContent(imgMs);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                form.Add(fileContent, "img", "img.png");

                var url = $"{Settings.Instance.WebHookServerUrl}/report/discord";
                var response = HttpClient.PostAsync(url, form);

                Log.Information($"SubmitReportToDiscordWebhook: Status: {response.Status}, StatusCode: {response.Result.StatusCode}");
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
        public string EncounterName { get; set; } = "";
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0);
        public string DiscordWebhookId { get; set; } = "";
        public string DiscordWebhookToken { get; set; } = "";
        public List<PartyMember> Party { get; set; } = [];
    }

    public class PartyMember
    {
        public string Name { get; set; } = "";
        public ulong CombatScore { get; set; }
        public ulong Dps { get; set; }
        public ulong Hps { get; set; }
        public ulong DamageTaken { get; set; }
        public float DamagePct { get; set; }
    }
}
