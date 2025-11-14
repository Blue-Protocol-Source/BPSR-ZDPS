namespace BPSR_ZDPS
{
    public class BattleManager
    {
        private static Battle Battle { get; set; } = new Battle();
        public static List<Encounter> Encounters { get; set; } = [];
        public static Encounter CurrentEncounter = null;

        public static void SetScene(int sceneId, string sceneName)
        {
            Battle.SceneId = (uint)sceneId;
            Battle.SceneName = sceneName;
        }

        public static void StartNewEncounter()
        {
            Battle.StartTime = DateTime.Now;
        }

        public static void StopEncounter()
        {
            
        }

        // Save this battle to the DB
        // Really just updated the battle info
        public static void SaveToDB()
        {

        }
    }
}
