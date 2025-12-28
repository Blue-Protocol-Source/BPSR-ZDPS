using BPSR_ZDPS.Database;
using BPSR_ZDPS.DataTypes;
using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using static BPSR_ZDPS.DBSchema;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private static object DBLock = new object();

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

            CheckAndRunMigrations();
        }

        public static void CloseAndSave()
        {
            if (DbConn != null)
            {
                lock (DBLock)
                {
                    DbConn.Close();
                }
            }
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
            lock (DBLock)
            {
                using var transaction = DbConn.BeginTransaction();

                using var encMs = new MemoryStream();
                ProtoBuf.Serializer.Serialize(encMs, encounter.ExData);
                encMs.Flush();
                encounter.ExDataBlob = Compressor.Wrap(encMs.ToArray()).ToArray();

                var encounterId = DbConn.QuerySingle<ulong>(DBSchema.Encounter.Insert, encounter, transaction);
                encounter.EncounterId = encounterId;

                using (var memoryStream = new MemoryStream())
                {
                    using (var compStream = new ZstdSharp.CompressionStream(memoryStream))
                    {
                        using (var streamWriter = new StreamWriter(compStream, Encoding.UTF8, 1024, true))
                        {
                            using (var writer = new JsonTextWriter(streamWriter))
                            {
                                JsonSerializer serializer = new JsonSerializer()
                                {
                                    Formatting = Formatting.None,
                                    TypeNameHandling = TypeNameHandling.All,
                                };
                                serializer.Serialize(writer, encounter.Entities);
                                writer.Flush();
                            }
                            streamWriter.Flush();
                        }
                        compStream.Flush();
                    }

                    var entityBlob = new EntityBlobTable();
                    entityBlob.EncounterId = encounterId;
                    entityBlob.Data = memoryStream.ToArray();
                    Log.Information($"Enounter's entityBlob.Data.Length = {entityBlob.Data.Length}");
                    DbConn.Execute(DBSchema.Entities.Insert, entityBlob);

                    transaction.Commit();
                }

                // Forcefully release the Generation 2 memory that the above just used
                GC.Collect(2);

                sw.Stop();
                Log.Information("Saving encounter {encounterId} to DB took: {duration}", encounterId, sw.Elapsed);

                //Directory.CreateDirectory("TestJsonEncounters");
                //File.WriteAllText($"TestJsonEncounters/Encounter_{encounterId}_Write.json", entitiesJson);

                return encounterId;
            }
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

            var decompressedEncEx = Decompressor.Unwrap(encounter.ExDataBlob);
            ProtoBuf.Serializer.Deserialize<EncounterExData>(decompressedEncEx, encounter.ExData);
            encounter.ExDataBlob = null;

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
            lock (DBLock)
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
        }

        public static void DeleteEncounter(ulong encounterId)
        {
            lock (DBLock)
            {
                var tx = DbConn.BeginTransaction();
                DbConn.Execute(DBSchema.Encounter.RemoveEncounter, new { EncounterId = encounterId }, tx);
                DbConn.Execute(DBSchema.Entities.RemoveByEncounterId, new { EncounterId = encounterId }, tx);
                tx.Commit();

                Log.Information("Deleted Encounter: {encounterId}", encounterId);
            }
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

            lock (DBLock)
            {
                var battleId = DbConn.QuerySingle<int>(DBSchema.Battles.Insert, battle);

                return battleId;
            }
        }

        public static void UpdateBattleInfo(int battleId, uint sceneId, string sceneName)
        {
            var battle = new Battle()
            {
                BattleId = battleId,
                SceneId = sceneId,
                SceneName = sceneName ?? ""
            };

            lock (DBLock)
            {
                DbConn.Execute(DBSchema.Battles.Update, battle);
            }
        }

        public static void UpdateBattleEnd(int battleId)
        {
            var battle = new Battle()
            {
                BattleId = battleId,
                EndTime = DateTime.Now
            };

            lock (DBLock)
            {
                DbConn.Execute(DBSchema.Battles.UpdateEndTime, battle);
            }
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

        public static void CheckAndRunMigrations()
        {
            var dbData = DbConn.Query<DbData>(DBSchema.DbData.Select).First();
            if (dbData.Version <= 1.0)
            {
                RunDbMigration1();
            }
        }

        public static void RunDbMigration1()
        {
            var encounters = DB.LoadEncounterSummaries();

            Log.Information("Running DB Migration 1");
            var tx = DbConn.BeginTransaction();
            foreach (var encounter in encounters)
            {
                var entityBlob = DbConn.QuerySingleOrDefault<EntityBlobTable>(DBSchema.Entities.SelectByEncounterId, new { EncounterId = encounter.EncounterId }, tx);
                using (var memStream = new MemoryStream(entityBlob.Data))
                {
                    using (var decompStream = new ZstdSharp.DecompressionStream(memStream))
                    {
                        using (var streamReader = new StreamReader(decompStream))
                        {
                            var txt = streamReader.ReadToEnd();
                            var sb = new StringBuilder(txt);
                            sb.Replace("BPSR-DeepsLib", "BPSR-ZDPSLib");
                            sb.Replace("BPSR_ZDPS.CombatStats2, BPSR-ZDPS", "BPSR_ZDPS.CombatStats, BPSR-ZDPS");

                            using (var memoryStream = new MemoryStream())
                            {
                                using (var compStream = new ZstdSharp.CompressionStream(memoryStream))
                                {
                                    using (var streamWriter = new StreamWriter(compStream, Encoding.UTF8, 1024, true))
                                    {
                                        streamWriter.Write(sb.ToString());
                                        streamWriter.Flush();
                                    }
                                    compStream.Flush();
                                }
                                memoryStream.Flush();

                                var blob = new EntityBlobTable();
                                blob.EncounterId = encounter.EncounterId;
                                blob.Data = memoryStream.ToArray();
                                var result = DbConn.Execute("UPDATE Entities SET Data = @Data WHERE EncounterId = @EncounterId", blob, tx);
                            }
                        }
                    }
                }
            }

            DbConn.Execute(DBSchema.DbData.Delete, tx);
            DbConn.Execute("INSERT INTO DbData (Version) SELECT (1.1)", tx);
            tx.Commit();

            DbConn.Execute("VACUUM");

            Log.Information("DB Migration 1 done");
        }
    }

    public class DBCleanUpResults
    {
        public int EncountersDeleted { get; set; } = 0;
        public int BattlesDeleted { get; set; } = 0;
        public int EntitiesCachesDeleted { get; set; } = 0;
    }
}
