using Dapper;
using System.Data;

namespace BPSR_ZDPS
{
    public class DBSchema
    {
        public static void CreateTables(IDbConnection conn)
        {
            conn.Execute(EncounterSql.CreateTable);
            conn.Execute(EntitiesSql.CreateTable);
            conn.Execute(BattlesSql.CreateTable);
            conn.Execute(DbDataSql.CreateTable);
        }

        public static class EncounterSql
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
            public const string SelectByBattleId = @"SELECT * FROM Encounters WHERE BattleId = @BattleId ORDER BY StartTime";

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

        public static class EntitiesSql
        {
            public const string Insert = @"
                INSERT INTO Entities (
                    EncounterId, Data
                ) VALUES (
                    @EncounterId, @Data
                );
                SELECT last_insert_rowid();";

            public const string SelectByEncounterId = @"SELECT * FROM Entities WHERE EncounterId = @EncounterId";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS Entities (
                    EncounterId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Data BLOB NOT NULL
                );";
        }

        public static class BattlesSql
        {
            public const string Insert = @"
                INSERT INTO Battles (
                    SceneId, SceneName, StartTime
                ) VALUES (
                    @SceneId, @SceneName, @StartTime
                );
                SELECT last_insert_rowid();";

            public const string Update = @"
                UPDATE Battles SET SceneId = @SceneId, SceneName = @SceneName, EndTime = @EndTime WHERE BattleId = @BattleId
            ";

            public const string SelectByBattleId = @"SELECT * FROM Battles WHERE BattleId = @BattleId";

            public const string SelectAll = @"SELECT * FROM Battles";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS Battles (
                    BattleId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SceneId INT NOT NULL,
                    SceneName TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT
                );";
        }

        public static class DbDataSql
        {
            public const string Select = @"SELECT * FROM DbData";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS DbData (
                    Version REAL
                );

                INSERT INTO DbData (Version) VALUES (1.0)";
        }
    }
}
