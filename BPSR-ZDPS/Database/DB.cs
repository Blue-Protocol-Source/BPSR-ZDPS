using BPSR_ZDPS.Database;
using BPSR_ZDPS.DataTypes;
using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace BPSR_ZDPS
{
    public class DB
    {
        public const string DbFileName = "ZDatabase.db";
        public static string DbFilePath = Path.Combine(Utils.DATA_DIR_NAME, DbFileName);

        private static SqliteConnection DbConn;
        private static ILogger Log;
        private static ZstdSharp.Compressor Compressor = new ZstdSharp.Compressor();
        private static ZstdSharp.Decompressor Decompressor = new ZstdSharp.Decompressor();

        public static void Init()
        {
            Log = Log ?? Serilog.Log.Logger.ForContext<DB>();
            if (DbConn != null)
            {
                DbConn.Close();
                DbConn.Dispose();
            }

            var useFileDb = Settings.Instance.UseDatabaseForEncounterHistory;
            DbConn = new SqliteConnection($"Data Source={(useFileDb ? DbFilePath : ":memory:")}");
            DbConn.Open();

            DBSchema.CreateTables(DbConn);
        }

        // Encounters
        public static ulong GetNextEncounterId()
        {
            const string sql = "SELECT COALESCE(MAX(EncounterId), 0) + 1 FROM Encounters";
            return DbConn.QuerySingle<ulong>(sql);
        }

        public static ulong GetNumEncounters()
        {
            const string sql = "SELECT COUNT(*) FROM Encounters";
            return DbConn.QuerySingle<ulong>(sql);
        }

        public static ulong InsertEncounter(Encounter encounter)
        {
            var sw = Stopwatch.StartNew();
            using var transaction = DbConn.BeginTransaction();
            var encounterId = DbConn.QuerySingle<ulong>(DBSchema.Encounter.Insert, encounter, transaction);
            encounter.EncounterId = encounterId;

            var entitiesJson = JsonConvert.SerializeObject(encounter.Entities, Formatting.None, new JsonSerializerSettings()
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
            });
            var entitiesAsBytes = ASCIIEncoding.UTF8.GetBytes(entitiesJson);
            var compressed = Compressor.Wrap(entitiesAsBytes);

            var entityBlob = new EntityBlobTable();
            entityBlob.EncounterId = encounterId;
            entityBlob.Data = compressed.ToArray();
            DbConn.Execute(DBSchema.Entities.Insert, entityBlob);

            transaction.Commit();

            sw.Stop();
            Log.Information("Saving encounter {encounterId} to DB took: {duration}", encounterId, sw.Elapsed);

            //Directory.CreateDirectory("TestJsonEncounters");
            //File.WriteAllText($"TestJsonEncounters/Encounter_{encounterId}_Write.json", entitiesJson);

            return encounterId;
        }

        public static Encounter? LoadEncounter(ulong encounterId)
        {
            var sw = Stopwatch.StartNew();
            var encounter = DbConn.QuerySingleOrDefault<Encounter>(DBSchema.Encounter.SelectById, new { EncounterId = encounterId });

            if (encounter == null)
            {
                Log.Warning("Encounter {encounterId} not found in database", encounterId);
                return null;
            }

            var entityBlob = DbConn.QuerySingleOrDefault<EntityBlobTable>(DBSchema.Entities.SelectByEncounterId, new { EncounterId = encounterId });
            if (entityBlob?.Data != null)
            {
                using (var memStream = new MemoryStream(entityBlob.Data))
                {
                    using (var decompStream = new ZstdSharp.DecompressionStream(memStream))
                    {
                        using (var streamReader = new StreamReader(decompStream))
                        {
                            using (JsonTextReader reader = new JsonTextReader(streamReader))
                            {
                                JsonSerializer serializer = new JsonSerializer()
                                {
                                    ContractResolver = new PrivateResolver(),
                                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
                                };
                                serializer.Converters.Add(new DictionaryObjectConverter());

                                encounter.Entities = serializer.Deserialize<ConcurrentDictionary<long, Entity>>(reader)!;
                            }
                        }
                    }
                }

                // Forcefully release the Generation 2 memory that the above just used
                GC.Collect(2);
            }
            else
            {
                Log.Warning("Entities data not found for encounter {encounterId}", encounterId);
                encounter.Entities = new ConcurrentDictionary<long, Entity>();
            }

            sw.Stop();
            Log.Information("Loading encounter {encounterId} from DB took: {duration}", encounterId, sw.Elapsed);

            //Directory.CreateDirectory("TestJsonEncounters");
            //File.WriteAllText($"TestJsonEncounters/Encounter_{encounterId}_Read.json", JsonConvert.SerializeObject(encounter, Formatting.Indented));

            return encounter;
        }

        public static List<Encounter> LoadEncounterSummaries()
        {
            var encounters = DbConn.Query<Encounter>(DBSchema.Encounter.SelectAll).ToList();
            return encounters;
        }

        public static DBCleanUpResults ClearOldEncounters(int olderThanDays)
        {
            var date = DateTime.Now.AddDays(olderThanDays * -1);
            var results = new DBCleanUpResults();
            results.EncountersDeleted = DbConn.Execute(DBSchema.Encounter.RemoveEncountersOlderThan, new { Date = date });
            results.EntitiesCachesDeleted = DbConn.Execute(DBSchema.Entities.DeleteEntitiesCachesWithNoEncounters);
            results.BattlesDeleted = DbConn.Execute(DBSchema.Battles.DeleteBattlesWithNoEncounters);

            DbConn.Execute("VACUUM");

            Log.Information("Cleaned up {EncountersDeleted} encounters and {BattlesDeleted} battles, with {EntitesCachesDeleted} cachedEntities",
                    results.EncountersDeleted, results.BattlesDeleted, results.EntitiesCachesDeleted);
            
            return results;
        }

        // Battles
        public static int GetNextBattleId()
        {
            const string sql = "SELECT COALESCE(MAX(BattleId), 0) + 1 FROM Battles";
            return DbConn.QuerySingle<int>(sql);
        }

        public static int StartBattle(uint sceneId, string sceneName)
        {
            var battle = new Battle()
            {
                SceneId = sceneId,
                SceneName = sceneName ?? "",
                StartTime = DateTime.Now
            };

            var battleId = DbConn.QuerySingle<int>(DBSchema.Battles.Insert, battle);

            return battleId;
        }

        public static void UpdateBattleInfo(int battleId, uint sceneId, string sceneName)
        {
            var battle = new Battle()
            {
                BattleId = battleId,
                SceneId = sceneId,
                SceneName = sceneName ?? ""
            };

            DbConn.Execute(DBSchema.Battles.Update, battle);
        }

        public static void UpdateBattleEnd(int battleId)
        {
            var battle = new Battle()
            {
                BattleId = battleId,
                EndTime = DateTime.Now
            };

            DbConn.Execute(DBSchema.Battles.UpdateEndTime, battle);
        }

        public static List<Battle> LoadBattles()
        {
            var battles = DbConn.Query<Battle>(DBSchema.Battles.SelectAll).ToList();
            return battles;
        }

        public static List<Encounter> LoadEncountersForBattleId(int battleId)
        {
            var encountersSum = DbConn.Query<Encounter>(DBSchema.Encounter.SelectByBattleId, new { BattleId = battleId });
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
            var line = DbConn.QuerySingleOrDefault<EntityCacheLine>(DBSchema.EntityCache.SelectByUUID, uuid);
            return line;
        }

        public static EntityCacheLine? GetEntityCacheLineByUID(long uid)
        {
            var line = DbConn.QuerySingleOrDefault<EntityCacheLine>(DBSchema.EntityCache.SelectByUID, uid);
            return line;
        }

        public static EntityCacheLine? GetOrCreateEntityCacheLineByUUID(long uuid)
        {
            var line = DbConn.QuerySingle<EntityCacheLine>(DBSchema.EntityCache.GetOrCreateDefaultByUUID, uuid);
            return line;
        }

        public static bool UpdateEntityCacheLine(EntityCacheLine line)
        {
            var result = DbConn.Execute(DBSchema.EntityCache.InsertOrReplace, line);
            return result > 0;
        }

        public static bool UpdateEntityCacheLines(IEnumerable<EntityCacheLine> lines)
        {
            var sw = Stopwatch.StartNew();
            using var trans = DbConn.BeginTransaction();
            var result = DbConn.Execute(DBSchema.EntityCache.InsertOrReplace, lines, trans);
            trans.Commit();
            sw.Stop();
            Log.Information("UpdateEntityCacheLines took {elapsed} to insert {numLines}", sw.Elapsed, lines.Count());

            return result > 0;
        }
    }

    public class DBCleanUpResults
    {
        public int EncountersDeleted { get; set; } = 0;
        public int BattlesDeleted { get; set; } = 0;
        public int EntitiesCachesDeleted { get; set; } = 0;
    }
}
