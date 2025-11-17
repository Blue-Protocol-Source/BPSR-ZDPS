using Dapper;
using System.Data;

namespace BPSR_ZDPS
{
    public class DBSchema
    {
        public static void CreateTables(IDbConnection conn)
        {
            conn.Execute(Encounter.CreateTable);
            conn.Execute(Entities.CreateTable);
            conn.Execute(Battles.CreateTable);
            conn.Execute(DbData.CreateTable);
            conn.Execute(EntityCache.CreateTable);
        }

        public static class Encounter
        {
            public const string Insert = @"
                INSERT INTO Encounters (
                    BattleId, SceneId, SceneName, StartTime, EndTime, LastUpdate, 
                    TotalDamage, TotalNpcDamage, TotalShieldBreak, TotalNpcShieldBreak,
                    TotalHealing, TotalNpcHealing, TotalOverhealing, TotalNpcOverhealing,
                    TotalTakenDamage, TotalNpcTakenDamage, TotalDeaths, TotalNpcDeaths
                ) VALUES (
                    @BattleId, @SceneId, @SceneName, @StartTime, @EndTime, @LastUpdate,
                    @TotalDamage, @TotalNpcDamage, @TotalShieldBreak, @TotalNpcShieldBreak,
                    @TotalHealing, @TotalNpcHealing, @TotalOverhealing, @TotalNpcOverhealing,
                    @TotalTakenDamage, @TotalNpcTakenDamage, @TotalDeaths, @TotalNpcDeaths
                );
                SELECT last_insert_rowid();";

            public const string SelectAll = @"SELECT * FROM Encounters ORDER BY StartTime DESC";
            public const string SelectById = @"SELECT * FROM Encounters WHERE EncounterId = @EncounterId";
            public const string SelectByBattleId = 
                @"SELECT * FROM Encounters WHERE BattleId = @BattleId ORDER BY StartTime";
            
            public const string RemoveEncountersOlderThan = 
                @"DELETE FROM Encounters WHERE EndTime IS NOT NULL AND datetime(EndTime) < @Date;";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS Encounters (
                    EncounterId INTEGER PRIMARY KEY AUTOINCREMENT,
                    BattleId INTEGER NOT NULL,
                    SceneId INTEGER NOT NULL,
                    SceneName TEXT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    LastUpdate TEXT,
                    TotalDamage INTEGER DEFAULT 0,
                    TotalNpcDamage INTEGER DEFAULT 0,
                    TotalShieldBreak INTEGER DEFAULT 0,
                    TotalNpcShieldBreak INTEGER DEFAULT 0,
                    TotalHealing INTEGER DEFAULT 0,
                    TotalNpcHealing INTEGER DEFAULT 0,
                    TotalOverhealing INTEGER DEFAULT 0,
                    TotalNpcOverhealing INTEGER DEFAULT 0,
                    TotalTakenDamage INTEGER DEFAULT 0,
                    TotalNpcTakenDamage INTEGER DEFAULT 0,
                    TotalDeaths INTEGER DEFAULT 0,
                    TotalNpcDeaths INTEGER DEFAULT 0
                )";
        }

        public static class Entities
        {
            public const string Insert = @"
                INSERT INTO Entities (
                    EncounterId, Data
                ) VALUES (
                    @EncounterId, @Data
                );
                SELECT last_insert_rowid();";

            public const string SelectByEncounterId = @"SELECT * FROM Entities WHERE EncounterId = @EncounterId";

            public const string DeleteEntitiesCachesWithNoEncounters =
                @"DELETE FROM Entities WHERE NOT EXISTS (SELECT 1 FROM Encounters WHERE Encounters.EncounterId = Entities.EncounterId);";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS Entities (
                    EncounterId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Data BLOB NOT NULL
                );";
        }

        public static class Battles
        {
            public const string Insert = @"
                INSERT INTO Battles (
                    SceneId, SceneName, StartTime
                ) VALUES (
                    @SceneId, @SceneName, @StartTime
                );
                SELECT last_insert_rowid();";

            public const string Update = @"UPDATE Battles SET SceneId = @SceneId, SceneName = @SceneName WHERE BattleId = @BattleId";
            public const string UpdateEndTime = @"UPDATE Battles SET EndTime = @EndTime WHERE BattleId = @BattleId";
            public const string SelectByBattleId = @"SELECT * FROM Battles WHERE BattleId = @BattleId";
            public const string SelectAll = @"SELECT * FROM Battles WHERE EndTime NOT NULL";

            public const string DeleteBattlesWithNoEncounters =
                @"DELETE FROM Battles WHERE NOT EXISTS (SELECT 1 FROM Encounters WHERE Encounters.BattleId = Battles.BattleId);";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS Battles (
                    BattleId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SceneId INT NOT NULL,
                    SceneName TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT
                );";
        }

        public static class DbData
        {
            public const string Select = @"SELECT * FROM DbData";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS DbData (
                    Version REAL
                );

                INSERT INTO DbData (Version) VALUES (1.0)";
        }

        public static class EntityCache
        {
            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS EntityCache (
                    UUID INTEGER PRIMARY KEY,
                    UID INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Level INTEGER NOT NULL,
                    AblityScore INTEGER NOT NULL,
                    ProfessionId INTEGER NOT NULL,
                    SubProfessionId INTEGER NOT NULL
                );";

            public const string InsertOrReplace = @"
                INSERT OR REPLACE INTO EntityCache
                (UUID, UID, Name, Level, AblityScore, ProfessionId, SubProfessionId)
                VALUES (@UUID, @UID, @Name, @Level, @AblityScore, @ProfessionId, @SubProfessionId);";

            public const string SelectAll = @"SELECT * FROM EntityCache;";
            public const string SelectByUUID = @"SELECT * FROM EntityCache WHERE UUID = @UUID;";
            public const string SelectByUID = @"SELECT * FROM EntityCache WHERE UID = @UID;";
            public const string GetOrCreateDefaultByUUID = @"
                INSERT INTO EntityCache (UUID, UID, Name, Level, AblityScore, ProfessionId, SubProfessionId)
                SELECT @UUID, (@UID >> 16), '', 0, 0, 0, 0
                WHERE NOT EXISTS (SELECT 1 FROM EntityCache WHERE UUID = @UUID);

                SELECT * FROM EntityCache WHERE UUID = @UUID;";
        }
    }
}
