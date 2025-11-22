using BPSR_ZDPS.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS
{
    public static class BattleStateMachine
    {
        public static ConcurrentQueue<KeyValuePair<EDungeonState, DateTime>> DungeonStateHistory { get; private set; } = new();
        public static ConcurrentQueue<KeyValuePair<DungeonTargetData, DateTime>> DungeonTargetDataHistory { get; private set; } = new();
        public static DateTime? DeferredTime { get; private set; } = null;
        static KeyValuePair<DungeonTargetData, DateTime>? PreviousDungeonTargetData = null;

        // Called at the start of a Map Load/Enter event before Objective and State data is set
        public static void StartNewBattle()
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} - BattleStateMachine.StartNewBattle");
            PreviousDungeonTargetData = null;
            DeferredTime = null;
            DungeonTargetDataHistory.Clear();
            DungeonStateHistory.Clear();

            EncounterManager.StartNewBattle();
            EncounterManager.StartEncounter(true);
        }

        public static void DungeonStateHistoryAdd(EDungeonState dungeonState)
        {
            DungeonStateHistory.Enqueue(new KeyValuePair<EDungeonState, DateTime>(dungeonState, DateTime.Now));
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} - BattleStateMachine.DungeonStateHistoryAdd: {dungeonState}");

            if (dungeonState == EDungeonState.DungeonStateNull)
            {
                // Start encounter (this is the Open World)
                // We can call this safely as it will reuse the current encounter if nothing has happened (which is the most likely case)
                // Note: While we are in Open World we will want to use different logic for detecting encounters
                EncounterManager.StartEncounter();
            }
            else if (dungeonState == EDungeonState.DungeonStatePlaying)
            {
                // Start encounter (this is when a dungeon's timer typically begins ticking)
                if (EncounterManager.Current.HasStatsBeenRecorded())
                {
                    // A battle was already created for us but we are forcing a new encounter to be made to keep the generated data separate from the prior states
                    EncounterManager.StartEncounter(true);
                }
                else
                {
                    EncounterManager.StartEncounter();
                }
            }
            else if (dungeonState == EDungeonState.DungeonStateEnd)
            {
                // End encounter (this is when a dungeon's timer typically stops ticking)
                EncounterManager.StopEncounter();
                // Do not have to worry about Settlement or Vote state as they can only occur after End
            }

            // Stimen Vaults failure state flow will go End > Settlement > Active. On starting a run it will immediately perform Ready > Playing
        }

        public static void DungeonTargetDataHistoryAdd(DungeonTargetData dungeonTargetData)
        {
            // We'll ensure there's an upper limit just in case something goes very wrong though if it does we're already in trouble
            if (DungeonTargetDataHistory.Count > 300)
            {
                DungeonTargetDataHistory.TryDequeue(out _);
            }

            // Store the current last data before we add the latest to the stack so we can make sure targets are actually changing
            var newDungeonTargetData = new KeyValuePair<DungeonTargetData, DateTime>(dungeonTargetData, DateTime.Now);

            DungeonTargetDataHistory.Enqueue(newDungeonTargetData);
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} - BattleStateMachine.DungeonTargetDataHistoryAdd: {dungeonTargetData}");

            if (DungeonTargetDataHistory.Count > 2 && dungeonTargetData.Complete == 0 && dungeonTargetData.Nums == 0)
            {
                // We got a new update, we'll check if it's the same as our map entry objective
                var firstObjective = DungeonTargetDataHistory.First();
                if (firstObjective.Key != null && PreviousDungeonTargetData != null)
                {
                    if (firstObjective.Key.TargetId != 0 && PreviousDungeonTargetData.Value.Key.TargetId != 0 && PreviousDungeonTargetData.Value.Key.TargetId != firstObjective.Key.TargetId && firstObjective.Key.TargetId == dungeonTargetData.TargetId)
                    {
                        PreviousDungeonTargetData = newDungeonTargetData;
                        System.Diagnostics.Debug.WriteLine($"{DateTime.Now} - BattleStateMachine.DungeonTargetDataHistoryAdd: RestartCheckHit!");
                        // Our first map entry objective is the same as the newest one we were just given
                        // This is likely some form of reset such as killing one of the Raid bosses but others have not been beat yet
                        EncounterManager.StopEncounter();
                        return;
                    }
                }
            }

            if (PreviousDungeonTargetData != null)
            {
                if (PreviousDungeonTargetData.Value.Key.Complete == 0 && dungeonTargetData.Complete == 0 && PreviousDungeonTargetData.Value.Key.TargetId == dungeonTargetData.TargetId)
                {
                    // The last objective and this one are the same "new objective" being set, this can happen if a player is watching a cutscene while others skip it/finish first and begin playing
                    // As each player finishes a cutscene, the objective is resent from the server to all players
                    // In order to continue the current encounter and use the first player done as the starting point we can just early exit now and everything will be handled
                    PreviousDungeonTargetData = newDungeonTargetData;
                    return;
                }
            }

            // We can set this here since we don't need it again in this function, if that changes then move it down lower
            PreviousDungeonTargetData = newDungeonTargetData;

            if (Settings.Instance.SplitEncountersOnNewPhases)
            {
                if (dungeonTargetData.Complete == 0 && dungeonTargetData.Nums == 0)
                {
                    // New objective set

                    // TODO: If this was set within a very short time after Complete, delay the start creation to allow resolving effects against despawning enemies properly
                    DeferredTime = DateTime.Now.AddSeconds(1);
                    //Task.Run(() => { Thread.Sleep(1000); EncounterManager.StartEncounter(); });
                    //EncounterManager.StartEncounter();
                }
                else if (dungeonTargetData.Complete == 1 && dungeonTargetData.Nums > 0)
                {
                    // Objective complete

                    // Note: As long as a new encounter is not made, new data will still be applied to this ended encounter (this is the desired behavior)
                    EncounterManager.StopEncounter();
                }
            }
        }

        public static void CheckDeferredCalls()
        {
            if (DeferredTime.HasValue && DateTime.Now.CompareTo(DeferredTime) >= 0)
            {
                DeferredTime = null;

                EncounterManager.StartEncounter();
            }
        }
    }
}
