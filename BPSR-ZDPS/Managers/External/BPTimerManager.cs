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

        public static ESpawnDataLoadStatus SpawnDataLoaded = ESpawnDataLoadStatus.NotLoaded;
        public static ESpawnDataLoadStatus SpawnDataRealtimeConnection = ESpawnDataLoadStatus.NotLoaded;
        public static List<MobsDescriptor> MobsDescriptors = new();
        public static List<StatusDescriptor> StatusDescriptors = new();

        public enum ESpawnDataLoadStatus : int
        {
            NotLoaded = 0,
            InProgress = 1,
            Complete = 2,
            Error = 3,
            Cancelled = 4
        }

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
                IsEncounterBound = false;
                EncounterManager.Current.EntityHpUpdated -= BPTimerManager_EntityHpUpdated;
            }
        }

        private static void BPTimerManager_EncounterStart(EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BPTimerManager_EncounterStart");
            if (!IsEncounterBound)
            {
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
            if (EncounterManager.Current.ChannelLine > 0 && attrId != null && SupportedEntityReportList.Contains((int)attrId))
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

        public static void FetchAllMobs()
        {
            var task = Task.Factory.StartNew(async () =>
            {
                SpawnDataLoaded = ESpawnDataLoadStatus.InProgress;

                // First fetch all mobs from /api/collections/mobs/records
                var mobs = await WebManager.BPTimerFetchAllMobs($"{HOST}/api/collections/mobs/records?page=1&perPage=100&sort=uid&expand=map&skipTotal=1");

                if (mobs == null)
                {
                    Log.Error("BPTimerManager FetchAllMobs Error getting data for mobs");
                    SpawnDataLoaded = ESpawnDataLoadStatus.Error;
                    return;
                }

                // Next fetch all mob channel status from /api/colllections/mob_channel_status/records
                Newtonsoft.Json.Linq.JObject mob_channel_status = new();
                int requestedItemsPerPage = 200;
                int lastItemsCount = 0;
                int pageNum = 1;
                do
                {
                    // Filter example
                    // https://db.bptimer.com/api/collections/mob_channel_status/records?page=1&perPage=150&skipTotal=1&filter=mob='717bt1uqt4jqu31f' || mob='tmai6ri61xgjgfm'

                    var status_response = await WebManager.BPTimerFetchMobChannelStatus($"{HOST}/api/collections/mob_channel_status/records?page={pageNum}&perPage={requestedItemsPerPage}&skipTotal=1");
                    if (status_response != null)
                    {
                        lastItemsCount = ((Newtonsoft.Json.Linq.JObject)status_response)["items"].Count();
                        mob_channel_status.Merge(status_response);
                        pageNum++;
                    }
                    else
                    {
                        lastItemsCount = 0;
                    }
                } while (lastItemsCount == requestedItemsPerPage);

                if (mob_channel_status.Count == 0)
                {
                    Log.Error("BPTimerManager FetchAllMobs Error getting data for mob channel status");
                    SpawnDataLoaded = ESpawnDataLoadStatus.Error;
                    return;
                }

                // TODO: Map the id and name fields from mobs together for lookups

                if (mobs != null && mob_channel_status.Count > 0)
                {
                    Newtonsoft.Json.Linq.JToken mobs_items = ((Newtonsoft.Json.Linq.JObject)mobs)["items"];
                    Newtonsoft.Json.Linq.JToken channel_status_items = ((Newtonsoft.Json.Linq.JObject)mob_channel_status)["items"];
                    System.Diagnostics.Debug.WriteLine(channel_status_items);

                    foreach (var mob in mobs_items)
                    {
                        long monsterId = long.Parse(mob["monster_id"].ToString());
                        string gameMonsterName = "";

                        if (HelperMethods.DataTables.Monsters.Data.TryGetValue(monsterId.ToString(), out var monster))
                        {
                            if (!string.IsNullOrEmpty(monster.Name))
                            {
                                gameMonsterName = monster.Name;
                            }
                        }

                        MobsDescriptors.Add(new MobsDescriptor()
                        {
                            MobId = mob["id"].ToString(),
                            MobName = mob["name"].ToString(),
                            MobType = mob["type"].ToString(),
                            MobRespawnTime = int.Parse(mob["respawn_time"].ToString()),
                            MobUID = int.Parse(mob["uid"].ToString()),
                            MobMapId = mob["expand"]["map"]["id"].ToString(),
                            MobMapName = mob["expand"]["map"]["name"].ToString(),
                            MobMapTotalChannels = int.Parse(mob["expand"]["map"]["total_channels"].ToString()),
                            MobMapUID = int.Parse(mob["expand"]["map"]["uid"].ToString()),
                            MonsterId = long.Parse(mob["monster_id"].ToString()),
                            GameMobName = gameMonsterName
                        });
                    }

                    foreach (var status in channel_status_items)
                    {
                        var mobId = status["mob"];
                        var channelNumber = status["channel_number"] ?? 0;
                        var lastUpdate = status["last_update"] ?? status["update"] ?? "";
                        var lastHp = status["last_hp"] ?? 0;
                        var location = status["location_image"] ?? "";

                        long monsterId = 0;
                        var match = MobsDescriptors.Where(x => x.MobId == mobId.ToString());
                        if (match.Any())
                        {
                            monsterId = match.First().MonsterId;
                        }

                        StatusDescriptors.Add(new StatusDescriptor()
                        {
                            MobId = mobId.ToString(),
                            ChannelNumber = int.Parse(channelNumber.ToString()),
                            UpdateTime = lastUpdate.ToString(),
                            LastHp = int.Parse(lastHp.ToString()),
                            UpdateTimestamp = (!string.IsNullOrEmpty(lastUpdate.ToString()) ? DateTime.ParseExact(lastUpdate.ToString(), "yyyy-MM-dd HH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture): null),
                            Location = location.ToString(),
                            MonsterId = monsterId
                        });
                    }

                    SpawnDataLoaded = ESpawnDataLoadStatus.Complete;
                }
                else
                {
                    Log.Error("BPTimerManager FetchAllMobs Error parsing data");
                    SpawnDataLoaded = ESpawnDataLoadStatus.Error;
                }
            });
        }

        public static CancellationTokenSource StartRealtime()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            SpawnDataRealtimeConnection = ESpawnDataLoadStatus.InProgress;
            var task = Task.Factory.StartNew(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await WebManager.BPTimerOpenRealtimeStream($"{HOST}/api/realtime", API_KEY, cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"BPTimerOpenRealtimeStream SSE Error: {ex.Message}");
                        await Task.Delay(500);
                    }
                }
            });
            return cancellationTokenSource;
        }

        public static void HandleMobHpUpdateEvent(List<BPTimerMobHpUpdate> updates)
        {
            foreach (var update in updates)
            {
                bool foundEntryToUpdate = false;
                int idx = 0;
                foreach (var item in StatusDescriptors)
                {
                    if (item.MobId == update.MobId && item.ChannelNumber == update.Channel)
                    {
                        item.LastHp = update.Hp;
                        DateTime timestamp = DateTime.Now;
                        item.UpdateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffZ");
                        item.UpdateTimestamp = timestamp;
                        item.Location = update.Location ?? "";

                        foundEntryToUpdate = true;
                        // Each update entry can only for be a single item
                        break;
                    }
                    idx++;
                }
                if (!foundEntryToUpdate)
                {
                    // The entry did not exist for some reason, we'll need to add it in now
                    DateTime timestamp = DateTime.Now;
                    long monsterId = 0;
                    var match = MobsDescriptors.Where(x => x.MobId == update.MobId);
                    if (match.Any())
                    {
                        monsterId = match.First().MonsterId;
                    }

                    StatusDescriptors.Add(new StatusDescriptor()
                    {
                        MobId = update.MobId,
                        ChannelNumber = update.Channel,
                        LastHp = update.Hp,
                        UpdateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffZ"),
                        UpdateTimestamp = timestamp,
                        Location = update.Location ?? "",
                        MonsterId = monsterId
                    });
                }
            }
        }

        public static void HandleMobResetEvent(List<string> resets)
        {
            foreach (var mobId in resets)
            {
                foreach (var status in StatusDescriptors)
                {
                    if (status.MobId == mobId)
                    {
                        DateTime timestamp = DateTime.Now;

                        status.LastHp = 0;
                        status.UpdateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffZ");
                        status.UpdateTimestamp = timestamp;
                    }
                }
            }
        }
    }

    public class MobsDescriptor
    {
        public string MobId { get; set; }
        public string MobName { get; set; }
        public string MobType { get; set; }
        public int MobRespawnTime { get; set; }
        public int MobUID { get; set; }
        public string MobMapId { get; set; }
        public string MobMapName { get; set; }
        public int MobMapTotalChannels { get; set; }
        public int MobMapUID { get; set; }
        public long MonsterId { get; set; }
        public string GameMobName { get; set; }
    }

    public class StatusDescriptor
    {
        public string MobId { get; set; }
        public int ChannelNumber { get; set; }
        public string UpdateTime { get; set; }
        public DateTime? UpdateTimestamp { get; set; }
        public int LastHp { get; set; }
        public string Location { get; set; }
        public long MonsterId { get; set; }
    }
}
