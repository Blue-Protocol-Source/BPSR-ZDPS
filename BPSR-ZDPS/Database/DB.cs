using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using static BPSR_ZDPS.DBSchema;

namespace BPSR_ZDPS
{
    public class DB
    {
        private static SqliteConnection DbConn;
        private static ILogger Log;
        private static ZstdSharp.Compressor Compressor = new ZstdSharp.Compressor();
        private static ZstdSharp.Decompressor Decompressor = new ZstdSharp.Decompressor();

        public static void Init()
        {
            Log = Serilog.Log.Logger.ForContext<DB>();
            DbConn = new SqliteConnection("Data Source=ZDPS_Logs.db");
            DbConn.Open();

            CreateTables(DbConn);
        }

        // Encounters
        public static ulong GetNextEncounterId()
        {
            const string sql = "SELECT COALESCE(MAX(EncounterId), 0) + 1 FROM Encounters";
            return DbConn.QuerySingle<ulong>(sql);
        }

        public static ulong InsertEncounter(Encounter encounter)
        {
            var sw = Stopwatch.StartNew();
            using var transaction = DbConn.BeginTransaction();
            var encounterId = DbConn.QuerySingle<ulong>(EncounterSql.Insert, encounter, transaction);
            encounter.EncounterId = encounterId;

            var entitiesJson = JsonConvert.SerializeObject(encounter.Entities);
            var entitiesAsBytes = ASCIIEncoding.UTF8.GetBytes(entitiesJson);
            var compressed = Compressor.Wrap(entitiesAsBytes);

            var entityBlob = new EntityBlobTable();
            entityBlob.EncounterId = encounterId;
            entityBlob.Data = compressed.ToArray();
            DbConn.Execute(EntitiesSql.Insert, entityBlob);

            transaction.Commit();

            sw.Stop();
            Log.Information("Saving encounter {encounterId} to DB took: {duration}", encounterId, sw.Elapsed);

            Directory.CreateDirectory("TestJsonEncounters");
            File.WriteAllText($"TestJsonEncounters/Encounter_{encounterId}_Write.json", JsonConvert.SerializeObject(encounter, Formatting.Indented));

            return encounterId;
        }

        public static Encounter? LoadEncounter(ulong encounterId)
        {
            var sw = Stopwatch.StartNew();
            var encounter = DbConn.QuerySingleOrDefault<Encounter>(EncounterSql.SelectById, new { EncounterId = encounterId });

            if (encounter == null)
            {
                Log.Warning("Encounter {encounterId} not found in database", encounterId);
                return null;
            }

            var entityBlob = DbConn.QuerySingleOrDefault<EntityBlobTable>(EntitiesSql.SelectByEncounterId, new { EncounterId = encounterId });
            if (entityBlob?.Data != null)
            {
                var decompressed = Decompressor.Unwrap(entityBlob.Data);
                var entitiesJson = Encoding.UTF8.GetString(decompressed.ToArray());
                encounter.Entities = JsonConvert.DeserializeObject<ConcurrentDictionary<long, Entity>>(entitiesJson, new JsonSerializerSettings()
                {
                    ContractResolver = new PrivateResolver(),
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                });
            }
            else
            {
                Log.Warning("Entities data not found for encounter {encounterId}", encounterId);
                encounter.Entities = new ConcurrentDictionary<long, Entity>();
            }

            sw.Stop();
            Log.Information("Loading encounter {encounterId} from DB took: {duration}", encounterId, sw.Elapsed);

            Directory.CreateDirectory("TestJsonEncounters");
            File.WriteAllText($"TestJsonEncounters/Encounter_{encounterId}_Read.json", JsonConvert.SerializeObject(encounter, Formatting.Indented));

            return encounter;
        }

        public static List<Encounter> LoadEncounterSummaries()
        {
            var encounters = DbConn.Query<Encounter>(EncounterSql.SelectAll).ToList();
            return encounters;
        }

        // Battles
        public static int GetNextBattleId()
        {
            const string sql = "SELECT COALESCE(MAX(BattleId), 0) + 1 FROM Battles";
            return DbConn.QuerySingle<int>(sql);
        }

        public static ulong StartBattle(uint sceneId, string sceneName)
        {
            var battle = new Battle()
            {
                SceneId = sceneId,
                SceneName = sceneName ?? "",
                StartTime = DateTime.Now
            };

            var battleId = DbConn.QuerySingle<ulong>(BattlesSql.Insert, battle);

            return battleId;
        }

        public static void UpdateBattleInfo(ulong battleId, uint sceneId, string sceneName)
        {
            var battle = new Battle()
            {
                BattleId = battleId,
                SceneId = sceneId,
                SceneName = sceneName ?? "",
                EndTime = DateTime.Now
            };

            DbConn.Execute(BattlesSql.Update, battle);
        }

        public static List<Battle> LoadBattles()
        {
            var battles = DbConn.Query<Battle>(BattlesSql.SelectAll).ToList();
            return battles;
        }

        public static List<Encounter> LoadEncountersForBattleId(ulong battleId)
        {
            var encountersSum = DbConn.Query<Encounter>(EncounterSql.SelectByBattleId, battleId);
            var encounters = new List<Encounter>(encountersSum.Count());

            foreach (var encounter in encountersSum)
            {
                var encounterFull = LoadEncounter(encounter.EncounterId);
                encounters.Add(encounterFull);
            }

            return encounters;
        }

        // Entity Cache
        public static EntityCacheLine? GetEntityCacheLineByUUID(long uuid)
        {
            var line = DbConn.QuerySingleOrDefault<EntityCacheLine>(EntityCacheLineSql.SelectByUUID, uuid);
            return line;
        }

        public static EntityCacheLine? GetEntityCacheLineByUID(long uid)
        {
            var line = DbConn.QuerySingleOrDefault<EntityCacheLine>(EntityCacheLineSql.SelectByUID, uid);
            return line;
        }

        public static EntityCacheLine? GetOrCreateEntityCacheLineByUUID(long uuid)
        {
            var line = DbConn.QuerySingle<EntityCacheLine>(EntityCacheLineSql.GetOrCreateDefaultByUUID, uuid);
            return line;
        }

        public static bool UpdateEntityCacheLine(EntityCacheLine line)
        {
            var result = DbConn.Execute(EntityCacheLineSql.InsertOrReplace, line);
            return result > 0;
        }

        public static bool UpdateEntityCacheLines(IEnumerable<EntityCacheLine> lines)
        {
            var sw = Stopwatch.StartNew();
            using var trans = DbConn.BeginTransaction();
            var result = DbConn.Execute(EntityCacheLineSql.InsertOrReplace, lines, trans);
            trans.Commit();
            sw.Stop();
            Log.Information("UpdateEntityCacheLines took {elapsed} to insert {numLines}", sw.Elapsed, lines.Count());

            return result > 0;
        }
    }

    public class PrivateResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            if (!prop.Writable)
            {
                var property = member as PropertyInfo;
                var hasPrivateSetter = property?.GetSetMethod(true) != null;
                prop.Writable = hasPrivateSetter;
            }
            return prop;
        }
    }
}
