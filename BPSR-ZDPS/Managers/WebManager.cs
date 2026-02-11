using BPSR_ZDPS.DataTypes;
using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Hashing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.InteropServices;
using System.Text;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS.Web
{
    public static class WebManager
    {
        private static HttpClient HttpClient = new HttpClient();

        static WebManager()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"ZDPS/{Utils.AppVersion}");
            HttpClient.DefaultRequestHeaders.Add("X-ZDPS-Version", Utils.AppVersion.ToString());
        }

        public static void SubmitReportToWebhook(Encounter encounter, Image<Rgba32> img, string webhookUrl)
        {
            try
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    var teamId = Utils.CreateZTeamId(encounter);
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
                    form.Headers.Add("X-ZDPS-ZTeamID", $"{teamId}");

                    string url = "";
                    if (Settings.Instance.WebhookReportsMode == EWebhookReportsMode.FallbackDiscordDeduplication)
                    {
                        // Construct url for going to the deduplication server
                        var discordWebHookInfo = Utils.SplitAndValidateDiscordWebhook(Settings.Instance.WebhookReportsDiscordUrl);
                        url = $"{Settings.Instance.WebhookReportsDeduplicationServerHost}/report/discord/{discordWebHookInfo.Value.id}/{discordWebHookInfo.Value.token}";
                    }
                    else if (Settings.Instance.WebhookReportsMode == EWebhookReportsMode.DiscordDeduplication)
                    {
                        var discordWebHookInfo = Utils.SplitAndValidateDiscordWebhook(Settings.Instance.WebhookReportsDiscordUrl);
                        var canSubmit = await CanSubmitEncounterReport(teamId, discordWebHookInfo.Value.id);
                        
                        if (canSubmit)
                        {
                            // Directly send to Discord
                            url = $"{webhookUrl}";
                        }
                        else
                        {
                            // Assume we are not allowed to submit, but if there was a problem the prior call already logged it
                            return;
                        }
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

        public static async Task<bool> CanSubmitEncounterReport(ulong teamId, string id)
        {
            try
            {
                var url = $"{Settings.Instance.WebhookReportsDeduplicationServerHost}/dedupecheck/{id}/{teamId}?uuid={AppState.PlayerUUID}";
                var result = await HttpClient.GetAsync(url);
                var dedupeResp = await result.Content.ReadFromJsonAsync<DedupeResp>();

                return dedupeResp.CanSend;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking to see if could submit report.");
                return false;
            }
        }

        public static void SubmitBPTimerRequest(object data, string endpoint, string apiKey)
        {
            try
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    string msgJson = JsonConvert.SerializeObject(data, Formatting.Indented);

                    var content = new StringContent(msgJson, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Content = content;
                    request.Headers.Add("X-API-Key", apiKey);

                    var response = await HttpClient.SendAsync(request);

                    Log.Information($"SubmitBPTimerRequest: StatusCode: {response.StatusCode}");
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        Log.Error($"{msgJson}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SubmitBPTimerRequest Error");
            }
        }

        public static async Task<object?> BPTimerFetchAllMobs(string endpoint)
        {
            var response = await HttpClient.GetAsync(endpoint);

            Log.Information($"BPTimerFetchAllMobs: StatusCode: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(content);
            }
            else
            {
                return null;
            }
        }

        public static async Task<object?> BPTimerFetchMobChannelStatus(string endpoint)
        {
            var response = await HttpClient.GetAsync(endpoint);

            Log.Information($"BPTimerFetchMobChannelStatus: StatusCode: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(content);
            }
            else
            {
                return null;
            }
        }

        public static async Task BPTimerOpenRealtimeStream(string endpoint, string apiKey, string userRegion, CancellationToken? cancellationToken = null)
        {
            Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.InProgress;
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Accept", "text/event-stream");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("X-API-Key", apiKey);

            client.DefaultRequestHeaders.Add("User-Agent", $"ZDPS/{Utils.AppVersion}");
            client.DefaultRequestHeaders.Add("X-ZDPS-Version", Utils.AppVersion.ToString());
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
            {
                Log.Debug("BPTimerOpenRealtimeStream Cancelled");
                Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.Cancelled;
                return;
            }

            if(!response.IsSuccessStatusCode)
            {
                Log.Error($"Error in BPTimerOpenRealtimeStream StatusCode: {response.StatusCode}");
                Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.Error;
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync())
            {
                if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
                {
                    Log.Debug("BPTimerOpenRealtimeStream Cancelled");
                    Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.Cancelled;
                    return;
                }

                if (item.EventType == "PB_CONNECT")
                {
                    var data = JsonConvert.DeserializeObject(item.Data);
                    string clientId = ((Newtonsoft.Json.Linq.JObject)data)["clientId"].ToString();
                    var subscribeResult = await BPTimerSubscribe(endpoint, clientId, apiKey, userRegion);
                    if (subscribeResult)
                    {
                        Log.Information($"Connected to PocketBase Realtime. Client ID: {clientId}");
                        Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.Complete;
                    }
                    else
                    {
                        Log.Error($"Failed connected to PocketBase Realtime. Client ID: {clientId}");
                        Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.Error;
                        return;
                    }
                }
                else if (item.EventType.StartsWith("mob_hp_updates", StringComparison.OrdinalIgnoreCase))
                {
                    string region = "NA";
                    try
                    {
                        if (item.EventType.StartsWith("mob_hp_updates_", StringComparison.OrdinalIgnoreCase))
                        {
                            string sentRegion = item.EventType.Substring(15).ToUpper();
                            region = sentRegion;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"BPTimerOpenRealtimeStream encountered error trying to resolve Mob HP Update regional indicator: {item.EventType}\n{ex.Message}");
                    }

                    var mobHpUpdate = JsonConvert.DeserializeObject<List<DataTypes.External.BPTimerMobHpUpdate>>(item.Data, new JsonSerializerSettings() { Converters = { new DataTypes.External.BPTimerMobHpUpdateConverter() } });
                    Managers.External.BPTimerManager.HandleMobHpUpdateEvent(mobHpUpdate, region);
                }
                else if (item.EventType.StartsWith("mob_resets", StringComparison.OrdinalIgnoreCase))
                {
                    string region = "NA";
                    try
                    {
                        if (item.EventType.StartsWith("mob_resets_", StringComparison.OrdinalIgnoreCase))
                        {
                            string sentRegion = item.EventType.Substring(11).ToUpper();
                            region = sentRegion;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"BPTimerOpenRealtimeStream encountered error trying to resolve Mob Reset regional indicator: {item.EventType}\n{ex.Message}");
                    }
                    
                    // This occurs when a monster type is schduled to respawn
                    Managers.External.BPTimerManager.HandleMobResetEvent(JsonConvert.DeserializeObject<List<string>>(item.Data), region);
                }
                else if (item.EventType.StartsWith("PB_"))
                {
                    Log.Debug($"Realtime control event = {item.EventType} in BPTimerOpenRealtimeStream");
                }
                else
                {
                    Log.Debug($"Unhandled SseItem.EventType = {item.EventType} in BPTimerOpenRealtimeStream");
                }

                if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
                {
                    Log.Debug("BPTimerOpenRealtimeStream Cancelled");
                    Managers.External.BPTimerManager.SpawnDataRealtimeConnection = Managers.External.BPTimerManager.ESpawnDataLoadStatus.Cancelled;
                    return;
                }
            }
        }

        public static async Task<bool> BPTimerSubscribe(string endpoint, string clientId, string apiKey, string userRegion)
        {
            try
            {
                List<string> topics = new() { "mob_hp_updates", "mob_resets" };
                if (!string.IsNullOrEmpty(userRegion) && userRegion != "NA")
                {
                    for (int i = 0; i < topics.Count; i++)
                    {
                        topics[i] = $"{topics[i]}_{userRegion.ToLower()}";
                    }
                }

                var msgJson = JsonConvert.SerializeObject(new DataTypes.External.BPTimerSubscribe() { ClientId = clientId, Subscriptions = topics }, Formatting.Indented);
                var content = new StringContent(msgJson, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = content;
                request.Headers.Add("X-API-Key", apiKey);
                var response = await HttpClient.SendAsync(request);

                Log.Information($"BPTimerSubscribe: StatusCode: {response.StatusCode}");
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    Log.Error($"{msgJson}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BPTimerSubscribe Error");
                return false;
            }
        }

        private static DiscordWebhookPayload CreateDiscordMessage(Encounter encounter, ulong teamId)
        {
            var unixStartTime = new DateTimeOffset(encounter.StartTime).ToUnixTimeSeconds();

            string sceneSubName = "";
            if (!string.IsNullOrEmpty(encounter.SceneSubName))
            {
                sceneSubName = $" - {encounter.SceneSubName}";
            }

            string difficulty = "";
            if (encounter.ExData.DungeonDifficulty > 0)
            {
                difficulty = $" (Master {encounter.ExData.DungeonDifficulty})";
            }

            string clearStatus = "";
            if (encounter.IsWipe)
            {
                clearStatus = $" `Wipe`";
            }
            else if (encounter.ExData.IsTimedOut)
            {
                clearStatus = $" `TimedOut`";
            }

            string gameTimeStatus = "";
            if (encounter.ExData.DungeonTimeDeathChange != 0)
            {
                gameTimeStatus = $" (Death Modified: `{(encounter.EndTime.AddSeconds(Math.Abs(encounter.ExData.DungeonTimeDeathChange)) - encounter.StartTime).ToString(@"hh\:mm\:ss")}`)";
            }

            string encounterName = $"**Encounter**:{clearStatus} {encounter.SceneName}{sceneSubName}{difficulty}";
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
            msgContentBuilder.AppendLine($"**Duration**: `{(encounter.EndTime - encounter.StartTime).ToString(@"hh\:mm\:ss")}`{gameTimeStatus}");
            msgContentBuilder.AppendLine($"**ZTeamID**: ``{teamId}``");

            var msg = new DiscordWebhookPayload("ZDPS", msgContentBuilder.ToString())
            {
                AvatarURL = "https://i.imgur.com/FsXoGt4.png"
            };

            return msg;
        }

        public static void CheckForZDPSUpdates()
        {
            try
            {
                Log.Information("Checking for ZDPS update...");
                var task = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var url = $"{Settings.Instance.LatestZDPSVersionCheckURL}";
                        var result = await HttpClient.GetAsync(url);
                        var response = await result.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(response))
                        {
                            var latestVersion = Version.Parse(response);
                            var isRunningLatest = Utils.AppVersion.CompareTo(latestVersion);
                            if (isRunningLatest >= 0)
                            {
                                // Running latest version
                                Log.Information("ZDPS is running latest version already.");
                                AppState.IsUpdateAvailable = false;
                            }
                            else
                            {
                                // Update available
                                Log.Information($"ZDPS update is available. Found v{latestVersion} online.");
                                AppState.IsUpdateAvailable = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error checking for ZDPS update.");
                        AppState.IsUpdateAvailable = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for ZDPS update task.");
                AppState.IsUpdateAvailable = false;
            }
        }
    }
}
