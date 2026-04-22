using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS.Windows
{
    public static class EventTrackerWindow
    {
        public const string LAYER = "EventTrackerWindowLayer";
        public static string TITLE_ID = "###EventTrackerWindow";
        public static string TITLE = "Event Tracker";

        public static string SaveDataFileName = "EventTrackerSaveData";
        public static string PresetsContainersSaveDataFileName = "EventTrackerPresetsContainersSaveData";
        public static string PresetsTrackersSaveDataFileName = "EventTrackerPresetsTrackersSaveData";
        public static int EventTrackerSaveVersion = 2;

        public static bool IsOpened = false;

        static bool ShouldTrackOpenState = false;

        static int RunOnceDelayed = 0;
        static bool HasInitBindings = false;
        static Vector2 MenuBarSize;
        static int LastPinnedOpacity = 100;
        static bool IsPinned = false;

        static bool HasBoundEvents = false;
        
        static bool DebugAllSceneEvents = false;

        static DateTime? LastGameFocusCheckTime = null;
        static bool IsGameFocused = false;

        public static bool ForceHideAllContainers = false;

        static uint PersistentTrackerCount = 0;
        static uint PersistentContainerCount = 0;
        static Dictionary<uint, TrackerContainer> EventTrackerContainers = new();

        static TrackerContainer? ActiveTrackerContainer;
        static TrackedEventEntry? ActiveTrackedEventEntry;
        static int ActiveTrackedEventEntryIdx = -1;

        static bool ShowAllBuffEventsToTrack = false;

        static ImGuiWindowClassPtr EventTrackerDisplayClass = ImGui.ImGuiWindowClass();
        static ImGuiWindowClassPtr EventTrackerDisplayInTaskBarClass = ImGui.ImGuiWindowClass();

        static string BuffFilterText = "";
        static KeyValuePair<string, Buff>[]? BuffFilterMatches;
        static bool BuffFilterIncludeDescriptions = false;

        static string SkillFilterText = "";
        static KeyValuePair<string, Skill>[]? SkillFilterMatches;
        static bool SkillFilterIncludeDescriptions = false;

        static bool ShowDebugLogWindow = false;
        static bool DebugLogAutoScroll = true;
        static ConcurrentQueue<string> DebugEventTrackerLog = new();

        static bool IsPresetManagerOpened = false;
        static bool ShouldPresetManagerFocusNext = false;
        static bool IsPresetManagerInContainerMode = false;
        static TrackedEventEntry? SelectedPresetManagerTracker = null;
        static List<TrackedEventEntry> PresetTrackersList = new();
        static string PresetManagerTrackersFilterText = "";
        static int SelectedPresetManagerContainerIdx = -1;
        static List<TrackerContainer> PresetContainersList = new();

        static string DragDropTargetName = "";

        // TODO: This really should be split up into an Event Tracker Manager and not all in the Window class

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;
            InitializeBindings();
            ImGui.PopID();
        }

        public static void InitializeBindings()
        {
            if (HasInitBindings == false)
            {
                HasInitBindings = true;
                EncounterManager.EncounterStart += EncounterManager_EncounterStart;
                EncounterManager.EncounterEndFinal += EncounterManager_EncounterEndFinal;

                EventTrackerDisplayClass.ClassId = ImGuiP.ImHashStr("EventTrackerDisplayClass");
                EventTrackerDisplayClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.TopMost | ImGuiViewportFlags.NoTaskBarIcon | ImGuiViewportFlags.OwnedByApp;// | ImGuiViewportFlags.NoInputs;

                EventTrackerDisplayInTaskBarClass.ClassId = ImGuiP.ImHashStr("EventTrackerDisplayInTaskBarClass");
                EventTrackerDisplayInTaskBarClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.TopMost | ImGuiViewportFlags.OwnedByApp;// | ImGuiViewportFlags.NoInputs;

                LoadContainersFromFile();
                LoadContainerPresetsFromFile();
                LoadTrackerPresetsFromFile();

                LoadDefaultPresets();

                BindCurrentEncounterEvents();
            }
        }

        public static bool SaveContainersToFile()
        {
            try
            {
                var data = new EventTrackerDataContainers()
                {
                    Version = EventTrackerSaveVersion,
                    Containers = EventTrackerContainers
                };

                var containers = JsonConvert.SerializeObject(data, Formatting.Indented);
                var filePath = Path.Combine(Utils.DATA_DIR_NAME, $"{SaveDataFileName}.json");
                File.WriteAllText(filePath, containers);
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error saving containers to file.");
                return false;
            }
        }

        public static bool SaveContainerPresetsToFile()
        {
            try
            {
                var data = new EventTrackerDataContainerPresets()
                {
                    Version = EventTrackerSaveVersion,
                    PresetsList = PresetContainersList
                };

                var containers = JsonConvert.SerializeObject(data, Formatting.Indented);
                var filePath = Path.Combine(Utils.DATA_DIR_NAME, $"{PresetsContainersSaveDataFileName}.json");
                File.WriteAllText(filePath, containers);
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error saving preset containers to file.");
                return false;
            }
        }

        public static bool SaveTrackerPresetsToFile()
        {
            try
            {
                var data = new EventTrackerDataTrackerPresets()
                {
                    Version = EventTrackerSaveVersion,
                    PresetsList = PresetTrackersList
                };

                var containers = JsonConvert.SerializeObject(data, Formatting.Indented);
                var filePath = Path.Combine(Utils.DATA_DIR_NAME, $"{PresetsTrackersSaveDataFileName}.json");
                File.WriteAllText(filePath, containers);
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error saving preset trackers to file.");
                return false;
            }
        }

        public static bool LoadContainersFromFile()
        {
            try
            {
                var filePath = Path.Combine(Utils.DATA_DIR_NAME, $"{SaveDataFileName}.json");
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var containersDataRaw = File.ReadAllText(filePath);
                //var deserialized = JsonConvert.DeserializeObject<Dictionary<uint, TrackerContainer>>(containersDataRaw);
                var deserialized = JsonConvert.DeserializeObject<EventTrackerDataContainers>(containersDataRaw);
                if (deserialized != null)
                {
                    if (deserialized.Version == 0)
                    {
                        Serilog.Log.Error($"LoadContainersFromFile {SaveDataFileName} had Version == 0");
                    }

                    EventTrackerContainers = deserialized.Containers;

                    foreach (var eventContainer in EventTrackerContainers)
                    {
                        eventContainer.Value.RecheckTrackerStates();
                        if (eventContainer.Value.IdTracker > PersistentContainerCount)
                        {
                            PersistentContainerCount = eventContainer.Value.IdTracker;

                            foreach (var eventTracker in eventContainer.Value.EventTrackers)
                            {
                                if (eventTracker.Value.IdTracker > PersistentTrackerCount)
                                {
                                    PersistentTrackerCount = eventTracker.Value.IdTracker;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error trying to load Containers from save data file.");
                return false;
            }
            return false;
        }

        public static bool LoadContainerPresetsFromFile()
        {
            try
            {
                var filePath = Path.Combine(Utils.DATA_DIR_NAME, $"{PresetsContainersSaveDataFileName}.json");
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var containersDataRaw = File.ReadAllText(filePath);
                //var deserialized = JsonConvert.DeserializeObject<List<TrackerContainer>>(containersDataRaw);
                var deserialized = JsonConvert.DeserializeObject<EventTrackerDataContainerPresets>(containersDataRaw);
                if (deserialized != null)
                {
                    if (deserialized.Version == 0)
                    {
                        Serilog.Log.Error($"LoadContainerPresetsFromFile {PresetsContainersSaveDataFileName} had Version == 0");
                    }

                    PresetContainersList = deserialized.PresetsList;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error trying to load Preset Containers from save data file.");
                return false;
            }
            return false;
        }

        public static bool LoadTrackerPresetsFromFile()
        {
            try
            {
                var filePath = Path.Combine(Utils.DATA_DIR_NAME, $"{PresetsTrackersSaveDataFileName}.json");
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var containersDataRaw = File.ReadAllText(filePath);
                //var deserialized = JsonConvert.DeserializeObject<List<TrackedEventEntry>>(containersDataRaw);
                var deserialized = JsonConvert.DeserializeObject<EventTrackerDataTrackerPresets>(containersDataRaw);
                if (deserialized != null)
                {
                    if (deserialized.Version == 0)
                    {
                        Serilog.Log.Error($"LoadTrackerPresetsFromFile {PresetsTrackersSaveDataFileName} had Version == 0");
                    }

                    PresetTrackersList = deserialized.PresetsList;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error trying to load Preset Trackers from save data file.");
                return false;
            }
            return false;
        }

        private static void EncounterManager_EncounterEndFinal(EncounterEndFinalData e)
        {
            HasBoundEvents = false;
            e.Encounter.BuffUpdated -= Encounter_BuffUpdated;
            e.Encounter.SkillActivated -= Encounter_SkillActivated;
            //e.Encounter.AttributeUpdated -= Encounter_AttributeUpdated;
            e.Encounter.SceneEvent -= Encounter_SceneEvent;
        }

        private static void EncounterManager_EncounterStart(EncounterStartEventArgs e)
        {
            if (e.Reason != EncounterStartReason.NewObjective)
            {
                foreach (var eventContainer in EventTrackerContainers)
                {
                    eventContainer.Value.EventWindowSizes.Clear();
                    foreach (var eventTrackers in eventContainer.Value.EventTrackers)
                    {
                        if (eventTrackers.Value.LoadEvents.KeepOnSceneChange && (e.Reason == EncounterStartReason.Force || e.Reason == EncounterStartReason.None))
                        {
                            // Tracker should persist through what is likely a scene change
                        }
                        else if (eventTrackers.Value.LoadEvents.KeepOnWipe && (e.Reason == EncounterStartReason.Wipe))
                        {
                            // Tracker should persist through a wipe
                        }
                        else if (eventTrackers.Value.LoadEvents.KeepOnRestart && (e.Reason == EncounterStartReason.Restart))
                        {
                            // Tracker should persist through a restart (generally is a Raid Boss kill)
                        }
                        else
                        {
                            eventTrackers.Value.EventData.Clear();
                        }
                    }
                }
            }

            BindCurrentEncounterEvents();
        }

        private static void BindCurrentEncounterEvents()
        {
            if (!HasBoundEvents)
            {
                HasBoundEvents = true;
            }
            EncounterManager.Current.BuffUpdated -= Encounter_BuffUpdated;
            EncounterManager.Current.BuffUpdated += Encounter_BuffUpdated;
            EncounterManager.Current.SkillActivated -= Encounter_SkillActivated;
            EncounterManager.Current.SkillActivated += Encounter_SkillActivated;
            // Disable Attribute events until a good reason to support them is found
            //EncounterManager.Current.AttributeUpdated -= Encounter_AttributeUpdated;
            //EncounterManager.Current.AttributeUpdated += Encounter_AttributeUpdated;
            EncounterManager.Current.SceneEvent -= Encounter_SceneEvent;
            EncounterManager.Current.SceneEvent += Encounter_SceneEvent;
        }

        public static void AddDebugLog(string log)
        {
            if (DebugEventTrackerLog.Count > 50)
            {
                DebugEventTrackerLog.TryDequeue(out _);
            }
            DebugEventTrackerLog.Enqueue(log);
        }

        private static bool CheckIfShouldHandleEvent(TrackedEventEntry eventTracker, long entityUuid)
        {
            bool shouldHandle = false;

            if (eventTracker.TrackedEntityType == ETrackedEntityType.Self)
            {
                shouldHandle = entityUuid == AppState.PlayerUUID;
            }
            else if (eventTracker.TrackedEntityType == ETrackedEntityType.UserTarget)
            {
                if (AppState.PlayerUUID != 0 && EncounterManager.Current != null)
                {
                    if (EncounterManager.Current.Entities.TryGetValue(AppState.PlayerUUID, out var player))
                    {
                        var attrTargetId = player.GetAttrKV("AttrTargetId") as long?;
                        if (attrTargetId != null)
                        {
                            shouldHandle = entityUuid == attrTargetId;

                            if (shouldHandle)
                            {
                                if (eventTracker.LastUserTargetEntityUuid != entityUuid)
                                {
                                    var oldEntityTrackers = eventTracker.EventData.Where(x => x.Key != eventTracker.LastUserTargetEntityUuid);
                                    foreach (var item in oldEntityTrackers)
                                    {
                                        item.Value.Cooldown.EndCooldown();
                                    }
                                    eventTracker.EventData.Remove(eventTracker.LastUserTargetEntityUuid, out _);
                                }
                                eventTracker.LastUserTargetEntityUuid = entityUuid;
                            }
                        }
                    }
                }
            }
            else if (eventTracker.TrackedEntityType == ETrackedEntityType.DefinedTarget)
            {
                if (eventTracker.DefinedEntityTargetUuid != 0)
                {
                    shouldHandle = entityUuid == eventTracker.DefinedEntityTargetUuid;
                }
            }
            else if (eventTracker.TrackedEntityType == ETrackedEntityType.Party)
            {
                if (AppState.PlayerUUID != entityUuid && AppState.PlayerUUID != 0 && AppState.PartyTeamId != 0 && EncounterManager.Current != null)
                {
                    if (Utils.UuidToEntityType(entityUuid) == (long)EEntityType.EntChar)
                    {
                        if (EncounterManager.Current.Entities.TryGetValue(entityUuid, out var otherEntity))
                        {
                            var teamId = otherEntity.GetAttrKV("AttrTeamId") as long?;
                            if (teamId != null && teamId == AppState.PartyTeamId)
                            {
                                shouldHandle = true;
                            }
                        }
                    }
                }
            }
            /*else if (eventTracker.TrackedEntityType == ETrackedEntityType.Raid)
            {
                // TODO: Handle Raid detection
                shouldHandle = false;
            }*/
            else if (eventTracker.TrackedEntityType == ETrackedEntityType.Everyone)
            {
                if (eventTracker.ExcludeSelfFromEveryoneType)
                {
                    shouldHandle = entityUuid != AppState.PlayerUUID;
                }
                else
                {
                    shouldHandle = true;
                }
            }
            else if (eventTracker.TrackedEntityType == ETrackedEntityType.Summons)
            {
                if (AppState.PlayerUUID != 0 && EncounterManager.Current != null)
                {
                    if (EncounterManager.Current.Entities.TryGetValue(entityUuid, out var otherEntity))
                    {
                        var summonFlag = otherEntity.GetAttrKV("AttrSummonFlag") as int?;
                        if (summonFlag != null && summonFlag > 0)
                        {
                            if (eventTracker.OnlyTrackSummonsFromSelf)
                            {
                                var topSummonerId = otherEntity.GetAttrKV("AttrTopSummonerId") as long?;
                                shouldHandle = (topSummonerId != null && topSummonerId == AppState.PlayerUUID);
                            }
                            else
                            {
                                shouldHandle = true;
                            }
                        }
                    }
                }
            }

            return shouldHandle;
        }

        private static void Encounter_BuffUpdated(object sender, BuffUpdatedEventArgs e)
        {
            foreach (var eventContainer in EventTrackerContainers)
            {
                if (!eventContainer.Value.IsContainerEnabled)
                {
                    continue;
                }

                if (eventContainer.Value.TrackAllBuffs && e.BaseId != 0 && e.Duration >= 0 && eventContainer.Value.EventTrackers.Count > 0)
                {
                    var trackerTemplate = eventContainer.Value.EventTrackers.First();
                    var hasTracked = eventContainer.Value.EventTrackers.Skip(1).Where(x => x.Value.TrackedBuffId == e.BaseId).Any();
                    if (!hasTracked)
                    {
                        bool shouldHandle = CheckIfShouldHandleEvent(trackerTemplate.Value, e.EntityUuid);
                        if (shouldHandle)
                        {
                            var newTracker = (TrackedEventEntry)trackerTemplate.Value.Clone(++PersistentTrackerCount);
                            newTracker.BuffEvents = new();
                            newTracker.BuffEvents.AddRange(trackerTemplate.Value.BuffEvents);
                            newTracker.EventData = new();
                            newTracker.TrackedBuffId = e.BaseId;

                            if (HelperMethods.DataTables.Buffs.Data.TryGetValue(e.BaseId.ToString(), out var matched))
                            {
                                newTracker.TrackerName = $"{matched.Name} AutoTracker";
                                newTracker.Name = matched.Name;
                                newTracker.Desc = matched.Desc;
                                string matchedIconName = matched.GetIconName();
                                string resolvedIconName = Path.Combine("Buffs", matchedIconName);
                                if (newTracker.IconPath != resolvedIconName)
                                {
                                    newTracker.UpdateIconData(matchedIconName, true);
                                }
                            }
                            else
                            {
                                newTracker.TrackerName = $"{e.BaseId} AutoTracker";
                            }

                            eventContainer.Value.EventTrackers.Add(newTracker.IdTracker, newTracker);
                            eventContainer.Value.RecheckTrackerStates();
                        }
                    }
                }

                foreach (var eventTrackerKVP in eventContainer.Value.EventTrackers)
                {
                    var eventTracker = eventTrackerKVP.Value;

                    if (eventTracker.TrackerType != ETrackerType.Buffs)
                    {
                        continue;
                    }

                    bool shouldHandle = CheckIfShouldHandleEvent(eventTracker, e.EntityUuid);

                    if (shouldHandle)
                    {
                        if (!eventTracker.EventData.TryGetValue(e.EntityUuid, out var eventData))
                        {
                            //eventData = new();
                            //eventTracker.EventData.Add(e.EntityUuid, eventData);
                        }

                        if ((eventTracker.TrackedBuffId == e.BaseId && (eventTracker.EventSourceMustBeSelf ? AppState.PlayerUUID == e.FireUuid : true))
                            || (eventData != null && e.BuffUuid != 0 && eventData.Uuid == e.BuffUuid))
                        {
                            if (eventTracker.BuffEvents.Contains(e.BuffEventType))
                            {
                                if (eventTracker.LimitToOneTrackerInstance)
                                {
                                    if (eventTracker.EventData.Count > 0)
                                    {
                                        bool shouldSkip = false;
                                        foreach (var item in eventTracker.EventData)
                                        {
                                            if (item.Value.Cooldown != null)
                                            {
                                                shouldSkip = true;
                                                break;
                                            }
                                        }
                                        if (shouldSkip)
                                        {
                                            continue;
                                        }
                                    }
                                }

                                if (eventData == null)
                                {
                                    eventData = new();
                                    eventTracker.EventData.TryAdd(e.EntityUuid, eventData);
                                }

                                eventTracker.IsHidden = false;

                                eventData.Uuid = e.BuffUuid;
                                if (e.BaseId != 0)
                                {
                                    eventData.SourceEntityUuid = e.FireUuid;
                                    eventData.SourceEntityName = e.EntityCasterName;
                                }
                                eventData.OwnerEntityUuid = e.EntityUuid;

                                bool didRaidWarning = false;

                                if (HelperMethods.DataTables.Buffs.Data.TryGetValue(e.BaseId.ToString(), out var matched))
                                {
                                    if (string.IsNullOrEmpty(eventTracker.Name) || eventTracker.Name != matched.Name)
                                    {
                                        eventTracker.Name = matched.Name;
                                    }

                                    string matchedIconName = matched.GetIconName();
                                    string resolvedIconName = Path.Combine("Buffs", matchedIconName);
                                    if (eventTracker.IconPath != resolvedIconName)
                                    {
                                        eventTracker.UpdateIconData(matchedIconName, true);
                                    }

                                    if (!string.IsNullOrEmpty(matched.Desc) && eventTracker.Desc != matched.Desc)
                                    {
                                        eventTracker.Desc = matched.Desc;
                                    }

                                    eventData.BuffType = matched.BuffType;
                                }

                                if (eventTracker.OverrideDuration)
                                {
                                    eventData.Cooldown = new(eventTracker.DurationOverrideValue, 0, 0);
                                }
                                else
                                {
                                    float newDuration = MathF.Round((float)e.Duration / 1000.0f, 4);
                                    if (eventData.Cooldown != null && eventData.Cooldown.IsCooldownStarted && newDuration != eventData.Cooldown.BaseDuration)
                                    {
                                        eventData.Cooldown.IncreaseBaseDuration(newDuration, true);
                                        if (e.BuffEventType == EBuffEventType.BuffEventRemoveLayer)
                                        {
                                            eventData.Cooldown.StartOrUpdate(e.UpdateDateTime, null, true);
                                        }
                                    }
                                    else
                                    {
                                        eventData.Cooldown = new(newDuration, 0, 0);
                                    }
                                }

                                int previousLayers = eventData.Layers;
                                eventData.Layers = e.Layer;

                                if (e.BuffEventType == EBuffEventType.BuffEventAddTo)
                                {
                                    eventData.Cooldown.StartOrUpdate(e.UpdateDateTime, null, true);
                                }


                                if (eventTracker.DebugLogTracker)
                                {
                                    AddDebugLog($"{DateTime.Now} Buff Event [{e.BuffUuid}]({e.BaseId}) {e.BuffEventType} FromUUID={e.EntityUuid} Name={eventTracker.Name} Layers={e.Layer} Duration={e.Duration} AppliedDur={eventData.Cooldown?.BaseDuration}");
                                }

                                eventData.Cooldown.StartOrUpdate(e.UpdateDateTime);

                                if (e.BuffEventType == EBuffEventType.BuffEventAddTo)
                                {
                                    if (!didRaidWarning)
                                    {
                                        var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnGain);

                                        if (rw != null)
                                        {
                                            if (rw.UseConditionValueCheck)
                                            {
                                                if (PerformRaidWarningConditionCheck<int>(eventData.Layers, (int)rw.CheckConditionValue, rw.CheckConditionType))
                                                {
                                                    didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, eventData.SourceEntityUuid);
                                                }
                                            }
                                            else
                                            {
                                                didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, eventData.SourceEntityUuid);
                                            }
                                        }
                                    }
                                }

                                if (e.BuffEventType == EBuffEventType.BuffEventRemoveLayer)
                                {
                                    if (!didRaidWarning)
                                    {
                                        var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnLayerCount);

                                        if (rw != null)
                                        {
                                            if (rw.UseConditionValueCheck)
                                            {
                                                if (PerformRaidWarningConditionCheck<int>(eventData.Layers, (int)rw.CheckConditionValue, rw.CheckConditionType))
                                                {
                                                    didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, eventData.SourceEntityUuid);
                                                }
                                            }
                                            else
                                            {
                                                didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, eventData.SourceEntityUuid);
                                            }
                                        }
                                    }
                                }

                                if (eventTracker.HideTrackerCondition == EHideTrackerCondition.HideOnSpecificEvent && e.BuffEventType == eventTracker.HideOnSpecificBuffEventValue)
                                {
                                    eventTracker.IsHidden = true;
                                }
                                else if (eventTracker.HideTrackerCondition == EHideTrackerCondition.HideOnRemoveEvent && e.BuffEventType == EBuffEventType.BuffEventRemove)
                                {
                                    eventTracker.IsHidden = true;
                                }

                                if (eventTracker.IgnoreCooldownDuration && e.BuffEventType == EBuffEventType.BuffEventRemove)
                                {
                                    // There won't be a Remove event coming from regular Cooldown Finished events so we'll simulate one here
                                    var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnRemove);

                                    if (rw != null)
                                    {
                                        didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, null);
                                    }
                                }
                                //System.Diagnostics.Debug.WriteLine($"{e.BuffEventType} ({eventData.Uuid}) {eventTracker.Name} - Dur={e.Duration}, Upd={e.UpdateDateTime}, Add={eventData.Added}, Rmv={eventData.Removed}");
                            }
                        }
                    }
                }
            }
        }

        private static void Encounter_SkillActivated(object sender, SkillActivatedEventArgs e)
        {
            foreach (var eventContainer in EventTrackerContainers)
            {
                if (!eventContainer.Value.IsContainerEnabled)
                {
                    continue;
                }

                foreach (var eventTrackerKVP in eventContainer.Value.EventTrackers)
                {
                    var eventTracker = eventTrackerKVP.Value;

                    if (eventTracker.TrackerType != ETrackerType.Skills)
                    {
                        continue;
                    }

                    if (!eventTracker.SkillEvents.Contains(ESkillEventTrackingType.SkillCast))
                    {
                        continue;
                    }

                    bool shouldHandle = CheckIfShouldHandleEvent(eventTracker, e.CasterUuid);

                    if (shouldHandle)
                    {
                        if (eventTracker.TrackedSkillId == e.SkillId)
                        {
                            eventTracker.EventData.TryGetValue(e.CasterUuid, out var eventData);

                            if (eventData == null)
                            {
                                eventData = new SkillEventData();
                                eventTracker.EventData.TryAdd(e.CasterUuid, eventData);
                            }

                            SkillEventData skillEventData = eventData as SkillEventData;

                            eventTracker.IsHidden = false;

                            eventData.OwnerEntityUuid = e.CasterUuid;

                            EncounterManager.Current.Entities.TryGetValue(e.CasterUuid, out var entity);

                            bool didRaidWarning = false;

                            if (HelperMethods.DataTables.Skills.Data.TryGetValue(e.SkillId.ToString(), out var matched))
                            {
                                if (string.IsNullOrEmpty(eventTracker.Name) || eventTracker.Name != matched.Name)
                                {
                                    eventTracker.Name = matched.Name;
                                }

                                string matchedIconName = matched.GetIconName();
                                string baseDir = "Skills";
                                if (matched.IsRoleSlot())
                                {
                                    baseDir = "Skills_Imagines";
                                }
                                string resolvedIconName = Path.Combine(baseDir, matchedIconName);
                                if (eventTracker.IconPath != resolvedIconName)
                                {
                                    eventTracker.UpdateIconData(matchedIconName, true);
                                }

                                if (!string.IsNullOrEmpty(matched.Desc) && eventTracker.Desc != matched.Desc)
                                {
                                    eventTracker.Desc = matched.Desc;
                                }

                                float tempCooldownReductionPct = 0.0f;
                                float tempCooldownReductionFlat = 0.0f;
                                float tempCooldownReductionAccel = 0.0f;
                                if (entity != null)
                                {
                                    foreach (var tempAttr in entity.TempAttributes)
                                    {
                                        if (tempAttr.Value.TempAttr.GetProtoAttrType() == ETempAttrEffectType.TempAttrSkillCd)
                                        {
                                            if (tempAttr.Value.TempAttr.AttrParams.Contains(e.SkillId))
                                            {
                                                // Percentage Cooldown reduction in encoded percent (ex: 3000 = 30% Reduction)
                                                tempCooldownReductionPct += tempAttr.Value.Value * 0.0001f;
                                            }
                                        }
                                        else if (tempAttr.Value.TempAttr.GetProtoAttrType() == ETempAttrEffectType.TempAttrSkillCdfixed)
                                        {
                                            if (tempAttr.Value.TempAttr.AttrParams.Contains(e.SkillId))
                                            {
                                                // Flat Cooldown reduction in ms
                                                tempCooldownReductionFlat += tempAttr.Value.Value;
                                            }
                                        }
                                        else if (tempAttr.Value.TempAttr.GetProtoAttrType() == ETempAttrEffectType.TempAttrSkillAccelerate)
                                        {
                                            // TODO: Implement handling of Accelerate reduction
                                            if (tempAttr.Value.TempAttr.AttrParams.Contains(e.SkillId))
                                            {
                                                // This should only be applied in "real-time" while active, we can't adjust the total Duration or Removed ahead of time

                                                // Percentage Cooldown reduction in encoded percent (ex: 3000 = 30% Reduction)
                                                tempCooldownReductionAccel += tempAttr.Value.Value * 0.0001f;
                                            }
                                        }
                                    }
                                }

                                // TODO: MaxEnergyChargeNum can be Negative to indicate the skill starts with no extra charges, but can gain them
                                if (matched.MaxEnergyChargeNum > 0)
                                {
                                    // TODO: When Charges are used, need to store the activation of each one
                                    // This will allow us to calculate per-charge remaining time
                                    // Most skills are relative charge cooldown instead of all charges restored at once

                                    // TODO: Need user options for if cooldowns:
                                    // - Reset all at same time (use latest cast to determine completion)
                                    // - Each begins cooling down the moment the charge is cast
                                    // - Each begins cooling down after the previous charge cooldown completed

                                    skillEventData.MaxCharges = matched.MaxEnergyChargeNum;
                                    var newChargeData = new ChargeData()
                                    {
                                        Cooldown = new((float)matched.EnergyChargeTime / 1000.0f, tempCooldownReductionPct, tempCooldownReductionFlat)
                                    };
                                    skillEventData.ChargeTimes.Add(newChargeData);

                                    float relativeDuration = 0;

                                    if (eventTracker.ChargeCooldownType == EChargeCooldownType.ChargeCooldownStartsAfterPrevious)
                                    {
                                        foreach (var chargeData in skillEventData.ChargeTimes)
                                        {
                                            relativeDuration += chargeData.Cooldown?.BaseDuration ?? 0;
                                        }
                                    }
                                    else if (eventTracker.ChargeCooldownType == EChargeCooldownType.ChargeCooldownStartsPerCast)
                                    {
                                        foreach (var chargeData in skillEventData.ChargeTimes)
                                        {
                                            chargeData.Cooldown.StartOrUpdate(e.ActivationDateTime.ToUniversalTime());
                                        }
                                    }
                                    else if (eventTracker.ChargeCooldownType == EChargeCooldownType.AllChargesResetTogetherFromLastCast)
                                    {
                                        var last = skillEventData.ChargeTimes.Last();
                                        last.Cooldown.StartOrUpdate(e.ActivationDateTime.ToUniversalTime());
                                        foreach (var chargeData in skillEventData.ChargeTimes)
                                        {
                                            chargeData.Cooldown.StartOrUpdate(last.Cooldown.Added.Value);
                                        }
                                    }
                                    else if (eventTracker.ChargeCooldownType == EChargeCooldownType.AllChargesResetTogetherFromFirstCast)
                                    {
                                        var first = skillEventData.ChargeTimes.First();
                                        first.Cooldown.StartOrUpdate(e.ActivationDateTime.ToUniversalTime());
                                        foreach (var chargeData in skillEventData.ChargeTimes)
                                        {
                                            chargeData.Cooldown.StartOrUpdate(first.Cooldown.Added.Value);
                                        }
                                    }

                                    if (eventData.Cooldown == null)
                                    {
                                        newChargeData.Cooldown.StartOrUpdate(e.ActivationDateTime.ToUniversalTime());
                                        eventData.Cooldown = new((float)matched.EnergyChargeTime / 1000.0f, tempCooldownReductionPct, tempCooldownReductionFlat);
                                    }
                                    else
                                    {
                                        // The overall cooldown has already been created, we're likely not the first charge used on this object
                                        // So we'll just update the Overall duration to be extended
                                        if (eventTracker.ChargeCooldownType == EChargeCooldownType.ChargeCooldownStartsAfterPrevious)
                                        {
                                            eventData.Cooldown.IncreaseBaseDuration(relativeDuration, true);
                                        }
                                        else if (eventTracker.ChargeCooldownType == EChargeCooldownType.ChargeCooldownStartsPerCast)
                                        {
                                            eventData.Cooldown.IncreaseBaseDuration((float)matched.EnergyChargeTime / 1000.0f);
                                        }
                                        else if (eventTracker.ChargeCooldownType == EChargeCooldownType.AllChargesResetTogetherFromLastCast)
                                        {
                                            eventData.Cooldown = new((float)matched.EnergyChargeTime / 1000.0f, tempCooldownReductionPct, tempCooldownReductionFlat);
                                        }
                                        else if (eventTracker.ChargeCooldownType == EChargeCooldownType.AllChargesResetTogetherFromFirstCast)
                                        {
                                            // Don't need to do anything for this type
                                        }
                                    }

                                    if (!didRaidWarning)
                                    {
                                        var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnChargeGainCooldown);

                                        if (rw != null)
                                        {
                                            if (rw.UseConditionValueCheck)
                                            {
                                                if (PerformRaidWarningConditionCheck<int>(skillEventData.MaxCharges, (int)rw.CheckConditionValue, rw.CheckConditionType))
                                                {
                                                    didRaidWarning = HandleRaidWarnings(rw, eventTracker, skillEventData, entity, entity);
                                                }
                                            }
                                            else
                                            {
                                                didRaidWarning = HandleRaidWarnings(rw, eventTracker, skillEventData, entity, entity);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    eventData.Cooldown = new(matched.Get_SkillFightLevel_PVECoolTime(), tempCooldownReductionPct, tempCooldownReductionFlat);
                                }

                                if (eventTracker.UseBossDbmCdDuration)
                                {
                                    if (HelperMethods.DataTables.Dbms.Data.TryGetValue(e.SkillId.ToString(), out var dbm))
                                    {
                                        eventData.Cooldown = new(dbm.CountCDTime, tempCooldownReductionPct, tempCooldownReductionFlat);
                                    }
                                    // Convert Skill Id to Effect Id and recheck the DBM table for the alert
                                    else if (HelperMethods.DataTables.Dbms.Data.TryGetValue(((e.SkillId * 100) + 1).ToString(), out var dbm2))
                                    {
                                        eventData.Cooldown = new(dbm2.CountCDTime, tempCooldownReductionPct, tempCooldownReductionFlat);
                                    }
                                }
                            }
                            else
                            {
                                if (HelperMethods.DataTables.Dbms.Data.TryGetValue(e.SkillId.ToString(), out var dbm))
                                {
                                    eventData.Cooldown = new(dbm.CountCDTime, 0, 0);
                                }
                            }

                            if (eventTracker.OverrideDuration)
                            {
                                eventData.Cooldown = new(eventTracker.DurationOverrideValue, 0, 0);
                            }

                            if (eventData.Cooldown == null)
                            {
                                eventData.Cooldown = new(0, 0, 0);
                            }

                            if (eventTracker.DebugLogTracker)
                            {
                                AddDebugLog($"{DateTime.Now} Skill Event SkillActivated ({e.SkillId}) FromUUID={e.CasterUuid} Name={eventTracker.Name} MaxCharges={skillEventData.MaxCharges} ChargesTimesCount={skillEventData.ChargeTimes.Count} AppliedDur={skillEventData.Cooldown.BaseDuration}");
                            }

                            eventData.Cooldown.StartOrUpdate(e.ActivationDateTime.ToUniversalTime());

                            if (!didRaidWarning)
                            {
                                var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnGain);

                                if (rw != null)
                                {
                                    didRaidWarning = HandleRaidWarnings(rw, eventTracker, skillEventData, entity, entity);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Encounter_AttributeUpdated(object sender, AttributeUpdatedEventArgs e)
        {
            return; // TODO: Implement

            foreach (var eventContainer in EventTrackerContainers)
            {
                if (!eventContainer.Value.IsContainerEnabled)
                {
                    continue;
                }

                foreach (var eventTrackerKVP in eventContainer.Value.EventTrackers)
                {
                    var eventTracker = eventTrackerKVP.Value;

                    if (eventTracker.TrackerType != ETrackerType.Attributes)
                    {
                        continue;
                    }

                    bool shouldHandle = CheckIfShouldHandleEvent(eventTracker, e.EntityUuid);

                    if (shouldHandle)
                    {
                        if (e.AttributeName == eventTracker.TrackedAttributeName)
                        {

                        }
                    }
                }
            }
        }

        private static void Encounter_SceneEvent(object sender, SceneEventEventArgs e)
        {
            if (DebugAllSceneEvents)
            {
                if (e.EventType == WorldEventType.BossDbm)
                {
                    var bossDbm = e as SceneEventBossDbmEventArgs;
                    AddDebugLog($"{DateTime.Now} SceneEvent {e.EventType} {bossDbm.SkillId} {bossDbm.Duration} {bossDbm.Timestamp} {bossDbm.Insertion}");
                }
                else if (e.EventType == WorldEventType.NoticeTip)
                {
                    var noticeTip = e as SceneEventNoticeTipEventArgs;
                    AddDebugLog($"{DateTime.Now} SceneEvent {e.EventType} {noticeTip.MessageId} {noticeTip.ExtraId}");
                }
                else
                {
                    //AddDebugLog($"{DateTime.Now} SceneEvent {e.EventType} (Unsupported)");
                }
            }

            foreach (var eventContainer in EventTrackerContainers)
            {
                if (!eventContainer.Value.IsContainerEnabled)
                {
                    continue;
                }

                foreach (var eventTrackerKVP in eventContainer.Value.EventTrackers)
                {
                    var eventTracker = eventTrackerKVP.Value;

                    if (eventTracker.TrackerType != ETrackerType.Skills)
                    {
                        continue;
                    }

                    //System.Diagnostics.Debug.WriteLine($"[0] SceneEvent {e.EventType}");

                    // We don't get an entity id with these events so we'll check against the active Boss UUID
                    if (e.EventType == WorldEventType.BossDbm)
                    {
                        if (!eventTracker.SkillEvents.Contains(ESkillEventTrackingType.BossWarning))
                        {
                            continue;

                        }
                        var bossDbm = e as SceneEventBossDbmEventArgs;

                        System.Diagnostics.Debug.WriteLine($"[1] SceneEvent {e.EventType} {bossDbm.SkillId} {bossDbm.Duration} {bossDbm.Timestamp} {bossDbm.Insertion}");

                        // Check if the base skill id is something we're tracking instead of the full effect id
                        if (bossDbm.SkillId == eventTracker.GetTrackedId() || Math.Floor((float)bossDbm.SkillId / 100.0f) == eventTracker.GetTrackedId())
                        {
                            long bossUuid = EncounterManager.Current.BossUUID;

                            eventTracker.EventData.TryGetValue(bossUuid, out var eventData);

                            if (eventData == null)
                            {
                                eventData = new SkillEventData();
                                eventTracker.EventData.TryAdd(bossUuid, eventData);
                            }

                            eventTracker.IsHidden = false;

                            eventData.OwnerEntityUuid = bossUuid;

                            if (HelperMethods.DataTables.Skills.Data.TryGetValue(eventTracker.GetTrackedId().ToString(), out var matched))
                            {
                                if (string.IsNullOrEmpty(eventTracker.Name) || eventTracker.Name != matched.Name)
                                {
                                    eventTracker.Name = matched.Name;
                                }

                                string matchedIconName = matched.GetIconName();
                                string baseDir = "Skills";
                                if (matched.IsRoleSlot())
                                {
                                    baseDir = "Skills_Imagines";
                                }
                                string resolvedIconName = Path.Combine(baseDir, matchedIconName);
                                if (eventTracker.IconPath != resolvedIconName)
                                {
                                    eventTracker.UpdateIconData(matchedIconName, true);
                                }

                                if (!string.IsNullOrEmpty(matched.Desc) && eventTracker.Desc != matched.Desc)
                                {
                                    eventTracker.Desc = matched.Desc;
                                }

                                eventData.Cooldown = new(bossDbm.Duration, 0, 0);
                            }

                            if (eventData.Cooldown == null)
                            {
                                eventData.Cooldown = new(0, 0, 0);
                            }

                            DateTime serverTime = DateTimeOffset.FromUnixTimeMilliseconds(bossDbm.Timestamp).UtcDateTime;
                            DateTime startTime = DateTime.UtcNow;

                            if (eventTracker.DebugLogTracker)
                            {
                                DebugEventTrackerLog.Enqueue($"{DateTime.Now} Skill Event SceneEvent [BossWarning] ({bossDbm.SkillId}) FromUUID={bossUuid} Name={eventTracker.Name} AppliedDur={eventData.Cooldown.BaseDuration} ServerTime={serverTime}");
                            }

                            // TODO: Currently we're reusing the BuffType to turn this bar red
                            // This should however use a Skill-specific variable to handle hostility assignment
                            eventData.BuffType = DataTypes.Enum.EBuffType.Debuff;

                            eventData.Cooldown.StartOrUpdate(startTime);

                            var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnGain);

                            if (rw != null)
                            {
                                bool didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, null);
                            }
                        }
                    }
                    else if (e.EventType == WorldEventType.NoticeTip)
                    {
                        if (!eventTracker.SkillEvents.Contains(ESkillEventTrackingType.NoticeTip))
                        {
                            continue;
                        }

                        var noticeTip = e as SceneEventNoticeTipEventArgs;

                        System.Diagnostics.Debug.WriteLine($"[2] SceneEvent {e.EventType} {noticeTip.MessageId} {noticeTip.ExtraId}");

                        if (noticeTip.MessageId == eventTracker.GetTrackedId().ToString())
                        {
                            long bossUuid = EncounterManager.Current.BossUUID;

                            eventTracker.EventData.TryGetValue(bossUuid, out var eventData);

                            if (eventData == null)
                            {
                                eventData = new SkillEventData();
                                eventTracker.EventData.TryAdd(bossUuid, eventData);
                            }

                            eventTracker.IsHidden = false;

                            eventData.OwnerEntityUuid = bossUuid;

                            if (HelperMethods.DataTables.Skills.Data.TryGetValue(eventTracker.GetTrackedId().ToString(), out var matched))
                            {
                                if (string.IsNullOrEmpty(eventTracker.Name) || eventTracker.Name != matched.Name)
                                {
                                    eventTracker.Name = matched.Name;
                                }

                                string matchedIconName = matched.GetIconName();
                                string baseDir = "Skills";
                                if (matched.IsRoleSlot())
                                {
                                    baseDir = "Skills_Imagines";
                                }
                                string resolvedIconName = Path.Combine(baseDir, matchedIconName);
                                if (eventTracker.IconPath != resolvedIconName)
                                {
                                    eventTracker.UpdateIconData(matchedIconName, true);
                                }

                                if (!string.IsNullOrEmpty(matched.Desc) && eventTracker.Desc != matched.Desc)
                                {
                                    eventTracker.Desc = matched.Desc;
                                }

                                if (eventTracker.UseBossDbmCdDuration)
                                {
                                    if (HelperMethods.DataTables.Dbms.Data.TryGetValue(eventTracker.TrackedSkillId.ToString(), out var dbm))
                                    {
                                        eventData.Cooldown = new(dbm.CountCDTime, 0, 0);
                                    }
                                    // Convert Skill Id to Effect Id and recheck the DBM table for the alert
                                    else if (HelperMethods.DataTables.Dbms.Data.TryGetValue(((eventTracker.TrackedSkillId * 100) + 1).ToString(), out var dbm2))
                                    {
                                        eventData.Cooldown = new(dbm2.CountCDTime, 0, 0);
                                    }
                                }
                            }

                            if (eventTracker.OverrideDuration)
                            {
                                eventData.Cooldown = new(eventTracker.DurationOverrideValue, 0, 0);
                            }

                            if (eventData.Cooldown == null)
                            {
                                eventData.Cooldown = new(0, 0, 0);
                            }

                            DateTime startTime = DateTime.UtcNow;

                            if (eventTracker.DebugLogTracker)
                            {
                                DebugEventTrackerLog.Enqueue($"{DateTime.Now} Skill Event SceneEvent [NoticeTip] ({noticeTip.MessageId}) FromUUID={bossUuid} Name={eventTracker.Name} AppliedDur={eventData.Cooldown.BaseDuration}");
                            }

                            eventData.Cooldown.StartOrUpdate(startTime);

                            var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnGain);

                            if (rw != null)
                            {
                                bool didRaidWarning = HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, null);
                            }
                        }
                    }
                }
            }
        }

        private static RaidWarningTrackerData? GetEnabledRaidWarning(TrackedEventEntry eventTracker, ERaidWarningActivationType activationType)
        {
            return eventTracker.RaidWarningTrackerDatas.Where(x => x.ActivationType == activationType && x.IsEnabled).FirstOrDefault();
        }

        private static bool HandleRaidWarnings(RaidWarningTrackerData raidWarningData, TrackedEventEntry eventTracker, EventData eventData, long? ownerUuid, long? casterUuid)
        {
            if (!eventTracker.IsEnabled)
            {
                return false;
            }

            if (raidWarningData == null)
            {
                return false;
            }

            Entity? owner = null;
            Entity? caster = null;
            if (ownerUuid != null && raidWarningData.MessageFormat.Contains("{OwnerE", StringComparison.OrdinalIgnoreCase))
            {
                EncounterManager.Current.Entities.TryGetValue(ownerUuid.Value, out owner);
            }
            if (casterUuid != null && raidWarningData.MessageFormat.Contains("{CasterE", StringComparison.OrdinalIgnoreCase))
            {
                EncounterManager.Current.Entities.TryGetValue(casterUuid.Value, out caster);
            }

            return HandleRaidWarnings(raidWarningData, eventTracker, eventData, owner, caster);
        }

        private static bool HandleRaidWarnings(RaidWarningTrackerData raidWarningData, TrackedEventEntry eventTracker, EventData eventData, Entity? owner, Entity? caster)
        {
            if (!eventTracker.IsEnabled)
            {
                return false;
            }

            if (raidWarningData == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(raidWarningData.MessageFormat))
            {
                string msgText = raidWarningData.MessageFormat;
                var matches = System.Text.RegularExpressions.Regex.Matches(raidWarningData.MessageFormat, @"\{([^}]+)\}");//@"\{(\w+)\}");

                if (matches.Count > 0)
                {
                    SkillEventData? skillEventData = null;
                    if (eventTracker.TrackerType == ETrackerType.Skills)
                    {
                        skillEventData = eventData as SkillEventData;
                    }

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        string matchValue = match.Groups[1].Value.ToLower();

                        switch (matchValue)
                        {
                            case "ownerentityname":
                                if (owner != null)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", owner.Name, StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "ownerentityhp":
                                if (owner != null)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", owner.Hp.ToString(), StringComparison.OrdinalIgnoreCase);
                                }

                                break;
                            case "ownerentitymaxhp":
                                if (owner != null)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", owner.MaxHp.ToString(), StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "ownerentityhppct":
                                if (owner != null && owner.MaxHp > 0)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", (MathF.Round((float)owner.Hp / (float)owner.MaxHp, 2) * 100).ToString(), StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "ownerentitytargetname":
                                if (owner != null)
                                {
                                    var targetId = owner.GetAttrKV("AttrTargetId") as long?;
                                    if (targetId != null)
                                    {
                                        if (EntityCache.Instance.Cache.Lines.TryGetValue(targetId.Value, out var cached))
                                        {
                                            msgText = msgText.Replace($"{{{matchValue}}}", cached.Name, StringComparison.OrdinalIgnoreCase);
                                        }
                                    }
                                }
                                break;
                            case "casterentityname":
                                if (caster != null)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", caster.Name, StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "casterentityhp":
                                if (caster != null)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", caster.Hp.ToString(), StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "casterentitymaxhp":
                                if (caster != null)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", caster.MaxHp.ToString(), StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "casterentityhppct":
                                if (caster != null && caster.MaxHp > 0)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", (MathF.Round((float)caster.Hp / (float)caster.MaxHp, 2) * 100).ToString(), StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "casterentitytargetname":
                                if (caster != null)
                                {
                                    var targetId = caster.GetAttrKV("AttrTargetId") as long?;
                                    if (targetId != null)
                                    {
                                        if (EntityCache.Instance.Cache.Lines.TryGetValue(targetId.Value, out var cached))
                                        {
                                            msgText = msgText.Replace($"{{{matchValue}}}", cached.Name, StringComparison.OrdinalIgnoreCase);
                                        }
                                    }
                                }
                                break;
                            case "trackername":
                                msgText = msgText.Replace($"{{{matchValue}}}", eventTracker.TrackerName, StringComparison.OrdinalIgnoreCase);
                                break;
                            case "name":
                                if (eventTracker.UseCustomName)
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", eventTracker.CustomName, StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    msgText = msgText.Replace($"{{{matchValue}}}", eventTracker.Name, StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case "duration":
                                msgText = msgText.Replace($"{{{matchValue}}}", eventData.Cooldown.BaseDuration.ToString(), StringComparison.OrdinalIgnoreCase);
                                break;
                            case "layers":
                                msgText = msgText.Replace($"{{{matchValue}}}", eventData.Layers.ToString(), StringComparison.OrdinalIgnoreCase);
                                break;
                            case "charges":
                                msgText = msgText.Replace($"{{{matchValue}}}", skillEventData?.ChargeTimes.Count.ToString(), StringComparison.OrdinalIgnoreCase);
                                break;
                            case "maxcharges":
                                msgText = msgText.Replace($"{{{matchValue}}}", skillEventData?.MaxCharges.ToString(), StringComparison.OrdinalIgnoreCase);
                                break;
                            default:
                                break;
                        }
                    }
                }

                Windows.RaidManagerRaidWarningWindow.AddRaidWarningMessage(msgText, raidWarningData.PlaySound, raidWarningData.CustomSoundPath);
                return true;
            }

            return false;
        }

        private static bool PerformRaidWarningConditionCheck<T>(T variable, T requiredValue, EConditionCheckType checkType)
        {
            var res = Comparer<T>.Default.Compare((T)variable, (T)requiredValue);
            switch (checkType)
            {
                case EConditionCheckType.EqualTo:
                    return res == 0;
                case EConditionCheckType.LessThan:
                    return res < 0;
                case EConditionCheckType.GreaterThan:
                    return res > 0;
                case EConditionCheckType.LessThanOrEqualTo:
                    return res <= 0;
                case EConditionCheckType.GreaterThanOrEqualTo:
                    return res >= 0;
            }

            return false;
        }

        public static void DrawTrackers()
        {
            if (EventTrackerContainers.Count > 0)
            {
                if (ForceHideAllContainers)
                {
                    return;
                }

                var windowSettings = Settings.Instance.WindowSettings.EventTracker;

                if (windowSettings.HideContainersWhenGameNotFocused)
                {
                    bool keepContainers = false;
                    if (windowSettings.KeepContainersWhenZDPSFocused && Utils.IsApplicationFocused())
                    {
                        keepContainers = true;
                    }

                    if (!keepContainers)
                    {
                        if (LastGameFocusCheckTime == null || DateTime.Now.Subtract(LastGameFocusCheckTime.Value).TotalSeconds > 3.0)
                        {
                            LastGameFocusCheckTime = DateTime.Now;

                            var gameProc = BPSR_ZDPSLib.Utils.GetCachedProcessEntry();
                            if (gameProc != null && gameProc.ProcessId > 0 && !string.IsNullOrEmpty(gameProc.ProcessName))
                            {
                                try
                                {
                                    System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(gameProc.ProcessId);
                                    var currentForegroundHandle = User32.GetForegroundWindow();
                                    if (process.MainWindowHandle != currentForegroundHandle)
                                    {
                                        InvalidateContainers();
                                        IsGameFocused = false;
                                        return;
                                    }

                                    IsGameFocused = true;
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            else
                            {
                                InvalidateContainers();
                                IsGameFocused = false;
                                return;
                            }
                        }
                        else
                        {
                            if (!IsGameFocused)
                            {
                                InvalidateContainers();
                                return;
                            }
                        }
                    }
                }

                ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
                foreach (var eventTrackerContainersKVP in EventTrackerContainers)
                {
                    var eventContainer = eventTrackerContainersKVP.Value;

                    if (!eventContainer.IsContainerEnabled || !eventContainer.HasEnabledTrackers)
                    {
                        eventContainer.HadTransparentBackground = false;
                        eventContainer.LastSetOpacity = 100;
                        continue;
                    }
                    else if (eventContainer.IsContainerEnabled && eventContainer.HasEnabledTrackers)
                    {
                        bool skipContainer = true;
                        // TODO: Check if any trackers should actually be loaded
                        // This check is currently very expensive. It should be cached
                        // And values should only be updated when their required events occur
                        foreach (var tracker in eventContainer.EventTrackers)
                        {
                            if (tracker.Value.IsEnabled && tracker.Value.LoadEvents.CheckShouldLoad())
                            {
                                // If at least one Tracker should be loaded, we can immediately move on to further processing
                                skipContainer = false;
                                break;
                            }
                        }

                        if (skipContainer)
                        {
                            eventContainer.HadTransparentBackground = false;
                            eventContainer.LastSetOpacity = 100;
                            continue;
                        }
                    }

                    if (eventContainer.ShowInTaskBar)
                    {
                        ImGui.SetNextWindowClass(EventTrackerDisplayInTaskBarClass);
                    }
                    else
                    {
                        ImGui.SetNextWindowClass(EventTrackerDisplayClass);
                    }

                    ImGuiWindowFlags extraFlags = ImGuiWindowFlags.None;
                    if (!windowSettings.IsContainerEditMode)
                    {
                        extraFlags |= ImGuiWindowFlags.NoMove;
                    }
                    if (eventContainer.ContainerSizeConstraint == EContainerSizeConstraint.AutoSize)
                    {
                        extraFlags |= ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
                    }
                    else
                    {
                        if (eventContainer.ContainerFixedSize != Vector2.Zero)
                        {
                            ImGui.SetNextWindowSize(eventContainer.ContainerFixedSize, ImGuiCond.Appearing);
                        }    
                    }

                    if (AppState.MousePassthrough || (windowSettings.AlwaysIgnoreInputs && !windowSettings.IsContainerEditMode))
                    {
                        extraFlags |= ImGuiWindowFlags.NoInputs;
                    }
                    if (!windowSettings.IsContainerEditMode)
                    {
                        extraFlags |= ImGuiWindowFlags.NoResize;
                    }

                    if (eventContainer.TransparentBackground)
                    {
                        extraFlags |= ImGuiWindowFlags.NoBackground;
                    }

                    if (!windowSettings.IsContainerEditMode && eventContainer.TransparentBackground)
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                    }

                    if (!eventContainer.HasLoadedSaveDataOnce)
                    {
                        eventContainer.HasLoadedSaveDataOnce = true;

                        if (windowSettings.ContainerPositions.TryGetValue(eventContainer.IdTracker, out var savedPosition))
                        {
                            ImGui.SetNextWindowPos(savedPosition, ImGuiCond.FirstUseEver);
                        }
                    }

                    float maxWidthThisPass = 0;

                    // Set the Window Name to be the stable IdTracker value as Windows will manage the window's by their title
                    if (ImGui.Begin($"{eventContainer.IdTracker}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoFocusOnAppearing | extraFlags))
                    {
                        if (eventContainer.IsWindowTitleDirty && Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                        {
                            Utils.SetWindowTitle(eventContainer.ContainerName, ImGui.GetWindowViewport());
                            eventContainer.IsWindowTitleDirty = false;
                        }

                        var windowBaseSize = ImGui.GetWindowSize();

                        if (eventContainer.ContainerSizeConstraint == EContainerSizeConstraint.FixedSize)
                        {
                            eventContainer.ContainerFixedSize = windowBaseSize;
                        }

                        if (eventContainer.ModifiedWindowOpacity && eventContainer.LastSetOpacity != eventContainer.WindowOpacityValue)
                        {
                            if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                            {
                                Utils.SetWindowOpacity(eventContainer.WindowOpacityValue * 0.01f);
                                eventContainer.LastSetOpacity = eventContainer.WindowOpacityValue;
                            }
                        }
                        else if (!eventContainer.ModifiedWindowOpacity && eventContainer.LastSetOpacity != 100)
                        {
                            if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                            {
                                Utils.SetWindowOpacity(1);
                                eventContainer.LastSetOpacity = 100;
                            }
                        }

                        if (!eventContainer.ModifiedWindowOpacity && eventContainer.TransparentBackground && !eventContainer.HadTransparentBackground)
                        {
                            if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                            {
                                eventContainer.HadTransparentBackground = true;
                            }
                        }
                        else if (!eventContainer.TransparentBackground && eventContainer.HadTransparentBackground)
                        {
                            if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                            {
                                eventContainer.HadTransparentBackground = false;
                            }
                        }

                        if (windowBaseSize == new Vector2(8, 8) && eventContainer.LastSetOpacity != 0)
                        {
                            if (Utils.CheckIfViewportValid(ImGui.GetWindowViewport()))
                            {
                                Utils.SetWindowOpacity(0);
                                eventContainer.LastSetOpacity = 0;
                            }
                        }

                        if (eventContainer.ShowContainerName)
                        {
                            ImGui.TextUnformatted(eventContainer.ContainerName);
                        }

                        List<long> hasShownEntityName = [];

                        int eventTrackersIdx = -1;
                        foreach (var eventTrackerKVP in eventContainer.EventTrackers.AsValueEnumerable())
                        {
                            if (!eventTrackerKVP.Value.LoadEvents.CheckShouldLoad())
                            {
                                continue;
                            }

                            eventTrackersIdx++;
                            if (eventContainer.TrackAllBuffs && eventTrackersIdx == 0)
                            {
                                continue;
                            }

                            var eventTracker = eventTrackerKVP.Value;

                            if (!eventTracker.IsEnabled)
                            {
                                continue;
                            }

                            bool isAddingTempData = false;
                            if (windowSettings.IsContainerEditMode && windowSettings.EditModeShowPlaceholders && eventTracker.EventData.Count == 0)
                            {
                                isAddingTempData = true;
                                eventTracker.EventData[0] = new();
                                eventTracker.EventData[0].Cooldown = new(10.0f, 0, 0);
                                eventTracker.EventData[0].Cooldown.StartOrUpdate(DateTime.UtcNow);
                            }

                            //List<long> expiredEventData = [];

                            foreach (var eventDataKVP in eventTracker.EventData.AsValueEnumerable())
                            {
                                var eventData = eventDataKVP.Value;

                                if (eventData == null)
                                {
                                    continue;
                                }

                                if (eventTracker.HideTrackerCondition == EHideTrackerCondition.HideWhenNoDuration && (eventData.Cooldown == null || eventData.Cooldown?.BaseDuration == 0) && !windowSettings.IsContainerEditMode)
                                {
                                    continue;
                                }

                                if (eventTracker.IsHidden && !windowSettings.IsContainerEditMode)
                                {
                                    continue;
                                }

                                bool hasSetSize = false;
                                float maxWindowSize = 0;
                                if (eventContainer.ContainerSizeConstraint == EContainerSizeConstraint.FixedSize)
                                {
                                    maxWindowSize = ImGui.GetContentRegionAvail().X;
                                }
                                /*if (eventContainer.EventWindowSizes.Count > 0)
                                {
                                    maxWindowSize = eventContainer.EventWindowSizes.Max(x => x.Value);
                                    hasSetSize = true;
                                }
                                if (eventContainer.ContainerSizeConstraint == EContainerSizeConstraint.FixedSize)
                                {
                                    maxWindowSize = ImGui.GetContentRegionAvail().X;
                                    hasSetSize = true;
                                }

                                bool hasAnyShown = (eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar) || (eventTracker.ShowIcon && eventTracker.IsIconValid) || (eventTracker.ShowLayers && !eventTracker.ShowLayersInsideProgressBar) || eventTracker.ShowEntityName;
                                if (!hasSetSize || (!hasAnyShown && eventContainer.ContainerSizeConstraint != EContainerSizeConstraint.FixedSize))
                                {
                                    //maxWindowSize = ImGui.GetContentRegionAvail().X;
                                    continue;
                                }

                                if (maxWindowSize == 0)
                                {
                                    maxWindowSize = ImGui.GetContentRegionAvail().X;
                                }*/

                                bool hasAnyShown = (eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar) || (eventTracker.ShowIcon && eventTracker.IsIconValid) || (eventTracker.ShowLayers && !eventTracker.ShowLayersInsideProgressBar) || eventTracker.ShowEntityName || (eventTracker.ShowDurationProgessBar && eventContainer.ContainerSizeConstraint == EContainerSizeConstraint.FixedSize);
                                if (!hasAnyShown)
                                {
                                    continue;
                                }

                                if (eventTracker.LoadEvents.IsOwnerAlive || eventTracker.LoadEvents.IsOwnerDead)
                                {
                                    if (EncounterManager.Current.Entities.TryGetValue(eventData.OwnerEntityUuid, out var ownerEntityData))
                                    {
                                        if (ownerEntityData.Hp == 0)
                                        {
                                            if (eventTracker.LoadEvents.IsOwnerAlive)
                                            {
                                                if (eventData.Cooldown != null)
                                                {
                                                    if (eventData.Cooldown.IsFinished())
                                                    {
                                                        eventData.Layers = 0;
                                                        if (eventData is SkillEventData)
                                                        {
                                                            ((SkillEventData)eventData).ChargeTimes.Clear();
                                                        }
                                                        eventData.Cooldown = null;
                                                    }
                                                }
                                                
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            if (eventTracker.LoadEvents.IsOwnerDead)
                                            {
                                                if (eventData.Cooldown != null)
                                                {
                                                    if (eventData.Cooldown.IsFinished())
                                                    {
                                                        eventData.Layers = 0;
                                                        if (eventData is SkillEventData)
                                                        {
                                                            ((SkillEventData)eventData).ChargeTimes.Clear();
                                                        }
                                                        eventData.Cooldown = null;
                                                    }
                                                }

                                                continue;
                                            }
                                        }
                                    }
                                }

                                // Cache the value in case it changes before the end of the rendering, we need to ensure the push is popped
                                bool hideTrackerBackground = eventContainer.HideTrackerBackground;
                                if (hideTrackerBackground)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0));
                                }

                                ImGuiChildFlags childFlags = ImGuiChildFlags.None;
                                if (!eventContainer.HideTrackerBorders)
                                {
                                    childFlags = ImGuiChildFlags.Borders;
                                }

                                ImGui.BeginChild($"{eventData.OwnerEntityUuid}", new Vector2(maxWindowSize, 0), childFlags | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.NoScrollbar);

                                ImGui.BeginGroup();

                                SkillEventData? skillEventData = null;
                                if (eventTracker.TrackerType == ETrackerType.Skills)
                                {
                                    skillEventData = eventData as SkillEventData;
                                }

                                if (eventTracker.ShowEntityName && !hasShownEntityName.Contains(eventData.OwnerEntityUuid))
                                {
                                    hasShownEntityName.Add(eventData.OwnerEntityUuid);

                                    if (EntityCache.Instance.Cache.Lines.TryGetValue(eventData.OwnerEntityUuid, out var cached))
                                    {
                                        ImGui.TextUnformatted($"{cached.Name}");
                                    }
                                    else
                                    {
                                        ImGui.TextUnformatted($"[{eventData.OwnerEntityUuid}]");
                                    }
                                }
                                else if (eventTracker.ShowEntityName && eventContainer.ContainerListDirection == EContainerListDirection.Right)
                                {
                                    ImGui.NewLine();
                                }

                                ImGui.BeginGroup();
                                if (eventTracker.ShowNameBeforeIcon && eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar)
                                {
                                    ImGui.PushFont(null, eventTracker.NameSize);
                                    if (eventTracker.UseCustomName && !string.IsNullOrEmpty(eventTracker.CustomName))
                                    {
                                        ImGui.TextUnformatted(eventTracker.CustomName);
                                    }
                                    else
                                    {
                                        ImGui.TextUnformatted(eventTracker.Name);
                                    }
                                    ImGui.PopFont();

                                    if (!eventTracker.NameNewLineBeforeIcon)
                                    {
                                        ImGui.SameLine();
                                    }
                                }

                                if (eventTracker.ShowLayersBeforeIcon && eventTracker.ShowLayers && !eventTracker.ShowLayersInsideProgressBar)
                                {
                                    if (eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar)
                                    {
                                        ImGui.SameLine();
                                    }
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green);
                                    ImGui.PushFont(null, eventTracker.LayerSize);
                                    if (skillEventData != null)
                                    {
                                        int chargesInQueue = skillEventData.ChargeTimes.Count;
                                        foreach (var chargeTime in skillEventData.ChargeTimes)
                                        {
                                            if (chargeTime.Cooldown != null && chargeTime.Cooldown.IsCooldownStarted && chargeTime.Cooldown.IsFinished())
                                            {
                                                chargesInQueue--;
                                            }
                                        }
                                        ImGui.TextUnformatted($"({chargesInQueue})");
                                    }
                                    else
                                    {
                                        ImGui.TextUnformatted($"({eventData.Layers})");
                                    }

                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();

                                    if (!eventTracker.LayersNewLineBeforeIcon)
                                    {
                                        ImGui.SameLine();
                                    }
                                }

                                if (eventTracker.ShowIcon && eventTracker.IsIconValid && !eventTracker.ShowIconInsideProgressBar)
                                {
                                    var tex = ImageArchive.LoadImage(eventTracker.IconPath);
                                    float texSize = eventTracker.IconSize;
                                    if (tex != null)
                                    {
                                        ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                                        if ((eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar && !eventTracker.ShowNameBeforeIcon) || eventTracker.ShowLayers)
                                        {
                                            ImGui.SameLine();
                                        }
                                    }
                                }
                                else if (eventTracker.ShowIconInsideProgressBar)
                                {
                                    ImGui.Dummy(new Vector2(0, 0));
                                }
                                if (!eventTracker.ShowNameBeforeIcon && eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar)
                                {
                                    ImGui.PushFont(null, eventTracker.NameSize);
                                    if (eventTracker.UseCustomName && !string.IsNullOrEmpty(eventTracker.CustomName))
                                    {
                                        ImGui.TextUnformatted(eventTracker.CustomName);
                                    }
                                    else
                                    {
                                        ImGui.TextUnformatted(eventTracker.Name);
                                    }
                                    ImGui.PopFont();
                                }
                                if (!eventTracker.ShowLayersBeforeIcon && eventTracker.ShowLayers && !eventTracker.ShowLayersInsideProgressBar)
                                {
                                    if (eventTracker.ShowName && !eventTracker.ShowNameInsideProgressBar)
                                    {
                                        ImGui.SameLine();
                                    }
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green);
                                    ImGui.PushFont(null, eventTracker.LayerSize);
                                    if (skillEventData != null)
                                    {
                                        int chargesInQueue = skillEventData.ChargeTimes.Count;
                                        foreach (var chargeTime in skillEventData.ChargeTimes)
                                        {
                                            if (chargeTime.Cooldown != null && chargeTime.Cooldown.IsCooldownStarted && chargeTime.Cooldown.IsFinished())
                                            {
                                                chargesInQueue--;
                                            }
                                        }
                                        ImGui.TextUnformatted($"({chargesInQueue})");
                                    }
                                    else
                                    {
                                        ImGui.TextUnformatted($"({eventData.Layers})");
                                    }
                                    
                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();
                                }
                                ImGui.EndGroup();
                                bool showTooltip = eventContainer.ShowCasterInTooltip || eventContainer.ShowNameInTooltip || eventContainer.ShowDescriptionInTooltip;
                                if (showTooltip && !eventTracker.ShowIconInsideProgressBar)
                                {
                                    DrawTrackerTooltip(eventContainer, eventTracker, eventData);
                                }

                                if (eventData.Cooldown != null)
                                {
                                    bool usingLayersForDuration = eventTracker.IgnoreCooldownDuration && eventTracker.UseLayersForDuration;

                                    if (!eventData.Cooldown.IsFinished() || usingLayersForDuration)
                                    {
                                        // Overall progress metrics
                                        float remainingSeconds = eventData.Cooldown.GetRemainingSeconds();
                                        float remainingPct = 1.0f - eventData.Cooldown.GetProgressPercent();
                                        
                                        if (skillEventData != null && skillEventData.ChargeTimes.Count > 0)
                                        {
                                            DateTime? lastFinish = null;
                                            foreach (var chargeTime in skillEventData.ChargeTimes)
                                            {
                                                if (chargeTime.Cooldown != null)
                                                {
                                                    if (chargeTime.Cooldown.IsCooldownStarted)
                                                    {
                                                        bool finished = chargeTime.Cooldown.IsFinished();
                                                        if (!finished)
                                                        {
                                                            // This charge is our active one to display
                                                            if (eventTracker.ChargeDurationDisplayType == EChargeDurationDisplayType.ResetEachCharge)
                                                            {
                                                                remainingSeconds = chargeTime.Cooldown.GetRemainingSeconds();
                                                                remainingPct = 1.0f - chargeTime.Cooldown.GetProgressPercent();
                                                            }

                                                            break;
                                                        }
                                                        lastFinish = chargeTime.Cooldown.Removed;
                                                    }
                                                    else if (!chargeTime.Cooldown.IsCooldownStarted && lastFinish != null)
                                                    {
                                                        float accelRate = 0.0f;
                                                        if (EncounterManager.Current.Entities.TryGetValue(eventData.OwnerEntityUuid, out var entity))
                                                        {
                                                            var attrCdAcceleratePct = entity.GetAttrKV("AttrCdAcceleratePct") as int?;
                                                            if (attrCdAcceleratePct != null)
                                                            {
                                                                accelRate = (1.0f - ((float)attrCdAcceleratePct / 1000.0f));
                                                            }
                                                        }

                                                        chargeTime.Cooldown.StartOrUpdate(lastFinish.Value, accelRate);

                                                        if (eventTracker.ChargeDurationDisplayType == EChargeDurationDisplayType.ResetEachCharge)
                                                        {
                                                            remainingSeconds = chargeTime.Cooldown.GetRemainingSeconds();
                                                            remainingPct = 1.0f - chargeTime.Cooldown.GetProgressPercent();
                                                        }

                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // This is not a skill, or is skill with no charge setup, the overall cooldown will be used
                                            if (usingLayersForDuration)
                                            {
                                                if (eventData.Layers > eventTracker.LayersForDurationMaxValue && eventTracker.LayersForDurationMaxValue > 0)
                                                {
                                                    remainingPct = ((float)eventTracker.LayersForDurationMaxValue / (float)eventData.Layers);
                                                    remainingSeconds = eventData.Layers;
                                                }
                                                else if (eventData.Layers < eventTracker.LayersForDurationMaxValue && eventData.Layers > 0)
                                                {
                                                    remainingPct = ((float)eventData.Layers / (float)eventTracker.LayersForDurationMaxValue);
                                                    remainingSeconds = eventTracker.LayersForDurationMaxValue;
                                                }
                                                if (remainingPct < 0)
                                                {
                                                    remainingPct = 0;
                                                }
                                            }
                                        }

                                        if (eventTracker.ShowDurationText && !eventTracker.ShowDurationTextInProgressBar)
                                        {
                                            if (eventTracker.DurationTextSameLine)
                                            {
                                                ImGui.SameLine();
                                            }

                                            ImGui.PushFont(null, eventTracker.DurationTextSize);
                                            if(usingLayersForDuration)
                                            {
                                                ImGui.TextUnformatted($"{remainingSeconds}");
                                            }
                                            else
                                            {
                                                if (eventTracker.UseMinutesForLongDuration && remainingSeconds > 60)
                                                {
                                                    ImGui.TextUnformatted($"{(int)remainingSeconds / 60}m");
                                                }
                                                else
                                                {
                                                    ImGui.TextUnformatted($"{remainingSeconds:F2}s");
                                                }
                                            }
                                            ImGui.PopFont();
                                        }
                                        if (eventTracker.ShowDurationProgessBar)
                                        {
                                            if (eventTracker.DurationProgressBarSameLine)
                                            {
                                                ImGui.SameLine();
                                                if (eventTracker.DurationProgressBarVerticalOffset > 0)
                                                {
                                                    float ratio = eventTracker.DurationProgressBarVerticalOffset * 0.01f;
                                                    var lastItemSize = ImGui.GetItemRectSize();
                                                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (lastItemSize.Y * ratio) - (eventTracker.DurationProgressBarSize * ratio));
                                                }
                                            }

                                            var itemRectSize = ImGui.GetItemRectSize().Y - ImGui.GetStyle().ItemSpacing.Y;
                                            if ((eventTracker.ShowName && eventTracker.ShowNameInsideProgressBar) || (eventTracker.ShowLayers && eventTracker.ShowLayersInsideProgressBar) || (eventTracker.ShowDurationText && eventTracker.ShowDurationTextInProgressBar))
                                            {
                                                string displayText = "";
                                                if (eventTracker.ShowName && eventTracker.ShowNameInsideProgressBar)
                                                {
                                                    if (eventTracker.UseCustomName && !string.IsNullOrEmpty(eventTracker.CustomName))
                                                    {
                                                        displayText = eventTracker.CustomName;
                                                    }
                                                    else
                                                    {
                                                        displayText = eventTracker.Name;
                                                    }
                                                }
                                                if (eventTracker.ShowLayers && eventTracker.ShowLayersInsideProgressBar)
                                                {
                                                    if (displayText.Length > 0)
                                                    {
                                                        displayText += " ";
                                                    }
                                                    displayText += $"({eventData.Layers})";
                                                }

                                                if (eventTracker.ShowDurationText && eventTracker.ShowDurationTextInProgressBar)
                                                {
                                                    if (displayText.Length > 0)
                                                    {
                                                        displayText += " ";
                                                    }
                                                    if (usingLayersForDuration)
                                                    {
                                                        displayText += $"{remainingSeconds}";
                                                    }
                                                    else
                                                    {
                                                        if (eventTracker.UseMinutesForLongDuration && remainingSeconds > 60)
                                                        {
                                                            displayText += $"{(int)remainingSeconds / 60}m";
                                                        }
                                                        else
                                                        {
                                                            displayText += $"{remainingSeconds:F2}s";
                                                        }
                                                    }
                                                }

                                                Vector4? barColor = null;
                                                if (eventTracker.ColorDurationProgressBarByType)
                                                {
                                                    barColor = eventData.BuffTypeToColor();
                                                }

                                                float offsetPct = MathF.Round((float)eventTracker.TextInsideProgressBarOffset / 100.0f, 4);
                                                if (eventContainer.ContainerListDirection == EContainerListDirection.Right)
                                                {
                                                    if (barColor != null)
                                                    {
                                                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor.Value);
                                                    }

                                                    ImGui.PushFont(null, eventTracker.DurationProgressBarTextSize);
                                                    if (eventTracker.DurationProgressBarStyle == EDurationProgressBarStyle.Circle)
                                                    {
                                                        var startPos = ImGui.GetCursorPos();

                                                        ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, eventTracker.DurationProgressBarCircleBackgroundColor);
                                                        if (eventTracker.ShowIcon && eventTracker.IsIconValid && eventTracker.ShowIconInsideProgressBar)
                                                        {
                                                            var tex = ImageArchive.LoadImage(eventTracker.IconPath);
                                                            // If the texture is null it will be skipped during the render process automatically
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness, tex, eventTracker.UseDurationProgressBarCircleBackgroundFill);
                                                            DrawTrackerTooltip(eventContainer, eventTracker, eventData);
                                                        }
                                                        else
                                                        {
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness, null, eventTracker.UseDurationProgressBarCircleBackgroundFill);
                                                        }
                                                        ImGui.PopStyleColor();

                                                        var endPos = ImGui.GetCursorPos();
                                                        var tSize = new Vector2();
                                                        if (eventTracker.UseMinutesForLongDuration && remainingSeconds > 60)
                                                        {
                                                            tSize = ImGui.CalcTextSize($"{(int)remainingSeconds / 60}m");
                                                        }
                                                        else
                                                        {
                                                            tSize = ImGui.CalcTextSize($"{remainingSeconds:F2}s");
                                                        }
                                                        
                                                        ImGui.SetCursorPosX(startPos.X + (eventTracker.DurationProgressBarSize * 0.5f) - (tSize.X * 0.5f));
                                                        ImGui.SetCursorPosY((startPos.Y * 0.5f) + (endPos.Y * 0.5f) - (tSize.Y * 0.5f));
                                                        //ImGui.SetCursorPos((startPos + endPos) * 0.5f);
                                                        bool customTextColor = eventTracker.UseCustomDurationTextColor;
                                                        if (customTextColor)
                                                        {
                                                            ImGui.PushStyleColor(ImGuiCol.Text, eventTracker.DurationTextColor);
                                                        }
                                                        ImGui.TextUnformatted(displayText);
                                                        if (customTextColor)
                                                        {
                                                            ImGui.PopFont();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ImGuiEx.TextAlignedProgressBar(remainingPct, displayText, offsetPct, ImGui.GetItemRectSize().X, eventTracker.DurationProgressBarSize);
                                                    }
                                                    ImGui.PopFont();

                                                    if (barColor != null)
                                                    {
                                                        ImGui.PopStyleColor();
                                                    }
                                                }
                                                else
                                                {
                                                    if (barColor != null)
                                                    {
                                                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor.Value);
                                                    }

                                                    ImGui.PushFont(null, eventTracker.DurationProgressBarTextSize);

                                                    if (eventTracker.DurationProgressBarStyle == EDurationProgressBarStyle.Circle)
                                                    {
                                                        var startPos = ImGui.GetCursorPos();

                                                        ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, eventTracker.DurationProgressBarCircleBackgroundColor);
                                                        if (eventTracker.ShowIcon && eventTracker.IsIconValid && eventTracker.ShowIconInsideProgressBar)
                                                        {
                                                            var tex = ImageArchive.LoadImage(eventTracker.IconPath);
                                                            // If the texture is null it will be skipped during the render process automatically
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness, tex, eventTracker.UseDurationProgressBarCircleBackgroundFill);
                                                            DrawTrackerTooltip(eventContainer, eventTracker, eventData);
                                                        }
                                                        else
                                                        {
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness, null, eventTracker.UseDurationProgressBarCircleBackgroundFill);
                                                        }
                                                        ImGui.PopStyleColor();

                                                        var endPos = ImGui.GetCursorPos();
                                                        var tSize = new Vector2();
                                                        if (eventTracker.UseMinutesForLongDuration && remainingSeconds > 60)
                                                        {
                                                            tSize = ImGui.CalcTextSize($"{(int)remainingSeconds / 60}m");
                                                        }
                                                        else
                                                        {
                                                            tSize = ImGui.CalcTextSize($"{remainingSeconds:F2}s");
                                                        }

                                                        ImGui.SetCursorPosX(startPos.X + (eventTracker.DurationProgressBarSize * 0.5f) - (tSize.X * 0.5f));
                                                        ImGui.SetCursorPosY((startPos.Y * 0.5f) + (endPos.Y * 0.5f) - (tSize.Y * 0.5f));
                                                        //ImGui.SetCursorPos((startPos + endPos) * 0.5f);
                                                        bool customTextColor = eventTracker.UseCustomDurationTextColor;
                                                        if (customTextColor)
                                                        {
                                                            ImGui.PushStyleColor(ImGuiCol.Text, eventTracker.DurationTextColor);
                                                        }
                                                        ImGui.TextUnformatted(displayText);
                                                        if (customTextColor)
                                                        {
                                                            ImGui.PopStyleColor();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ImGuiEx.TextAlignedProgressBar(remainingPct, displayText, offsetPct, ImGui.GetContentRegionAvail().X, eventTracker.DurationProgressBarSize);
                                                    }
                                                    
                                                    ImGui.PopFont();

                                                    if (barColor != null)
                                                    {
                                                        ImGui.PopStyleColor();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Vector4? barColor = null;
                                                if (eventTracker.ColorDurationProgressBarByType)
                                                {
                                                    barColor = eventData.BuffTypeToColor();
                                                }

                                                if (barColor != null)
                                                {
                                                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor.Value);
                                                }

                                                if (eventContainer.ContainerListDirection == EContainerListDirection.Right)
                                                {
                                                    ImGui.PushFont(null, eventTracker.DurationProgressBarTextSize);
                                                    if (eventTracker.DurationProgressBarStyle == EDurationProgressBarStyle.Circle)
                                                    {
                                                        if (eventTracker.ShowIcon && eventTracker.IsIconValid && eventTracker.ShowIconInsideProgressBar)
                                                        {
                                                            var tex = ImageArchive.LoadImage(eventTracker.IconPath);
                                                            // If the texture is null it will be skipped during the render process automatically
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness, tex);
                                                            DrawTrackerTooltip(eventContainer, eventTracker, eventData);
                                                        }
                                                        else
                                                        {
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ImGui.ProgressBar(remainingPct, new Vector2(ImGui.GetItemRectSize().X, eventTracker.DurationProgressBarSize), "##BuffDurationProgressBar");
                                                    }
                                                    ImGui.PopFont();
                                                }
                                                else
                                                {
                                                    ImGui.PushFont(null, eventTracker.DurationProgressBarTextSize);
                                                    if (eventTracker.DurationProgressBarStyle == EDurationProgressBarStyle.Circle)
                                                    {
                                                        if (eventTracker.ShowIcon && eventTracker.IsIconValid && eventTracker.ShowIconInsideProgressBar)
                                                        {
                                                            var tex = ImageArchive.LoadImage(eventTracker.IconPath);
                                                            // If the texture is null it will be skipped during the render process automatically
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness, tex);
                                                            DrawTrackerTooltip(eventContainer, eventTracker, eventData);
                                                        }
                                                        else
                                                        {
                                                            ImGuiEx.ProgressBarArc(eventTracker.DurationProgressBarSize, 360, remainingPct * 100.0f, eventTracker.DurationProgressBarCircleThickness);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ImGui.ProgressBar(remainingPct, new Vector2(-1, eventTracker.DurationProgressBarSize), "##BuffDurationProgressBar");
                                                    }
                                                    ImGui.PopFont();
                                                }

                                                if (barColor != null)
                                                {
                                                    ImGui.PopStyleColor();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!eventTracker.IgnoreCooldownDuration)
                                        {
                                            // Cooldown duration is complete, reset/clean up here
                                            eventData.Layers = 0;
                                            if (eventData is SkillEventData)
                                            {
                                                ((SkillEventData)eventData).ChargeTimes.Clear();
                                            }

                                            var rw = GetEnabledRaidWarning(eventTracker, ERaidWarningActivationType.OnRemove);

                                            if (rw != null)
                                            {
                                                HandleRaidWarnings(rw, eventTracker, eventData, eventData.OwnerEntityUuid, null);
                                            }

                                            eventData.Cooldown = null;
                                        }
                                    }
                                }
                                else
                                {
                                    if (eventTracker.ShowDurationText && eventTracker.ShowDurationEnded)
                                    {
                                        ImGui.PushFont(null, eventTracker.DurationTextSize);
                                        ImGui.TextUnformatted("Duration Ended");
                                        ImGui.PopFont();
                                    }
                                    //expiredEventData.Add(eventDataKVP.Key);
                                }

                                ImGui.EndGroup();
                                if (!eventContainer.EventWindowSizes.TryGetValue(eventData.OwnerEntityUuid, out var storedSize))
                                {
                                    storedSize = 0;
                                }

                                if (storedSize == float.PositiveInfinity || storedSize > 20000)
                                {
                                    storedSize = 0;
                                }

                                maxWidthThisPass = MathF.Max(maxWidthThisPass, ImGui.GetItemRectSize().X + ImGui.GetStyle().FramePadding.X);

                                if (eventContainer.ContainerListDirection == EContainerListDirection.Right)
                                {
                                    maxWidthThisPass = 0;
                                    ImGui.SameLine();
                                }

                                ImGui.EndChild();

                                if (hideTrackerBackground)
                                {
                                    ImGui.PopStyleColor();
                                }

                                eventContainer.EventWindowSizes[eventData.OwnerEntityUuid] = maxWidthThisPass;
                            }

                            if (isAddingTempData)
                            {
                                eventTracker.EventData.Remove(0, out _);
                            }

                            /*foreach (var expired in expiredEventData)
                            {
                                eventTracker.EventData.Remove(expired, out _);
                            }*/
                        }

                        Settings.Instance.WindowSettings.EventTracker.ContainerPositions[eventContainer.IdTracker] = ImGui.GetWindowPos();

                        ImGui.End();
                    }

                    if (!windowSettings.IsContainerEditMode && eventContainer.TransparentBackground)
                    {
                        ImGui.PopStyleVar();
                    }

                    if (eventContainer.TransparentBackground)
                    {
                        //ImGui.PopStyleColor();
                    }
                }
                ImGui.PopID();
            }
        }

        private static void DrawTrackerTooltip(TrackerContainer eventContainer, TrackedEventEntry eventTracker, EventData eventData)
        {
            if (ImGui.BeginItemTooltip())
            {
                if (eventContainer.ShowCasterInTooltip)
                {
                    string casterName = eventData.SourceEntityName;
                    if (string.IsNullOrEmpty(casterName))
                    {
                        casterName = $"[{eventData.SourceEntityUuid}]";
                    }
                    ImGui.TextUnformatted($"Caster: {casterName}");
                }
                if (eventContainer.ShowDurationInTooltip && eventData.Cooldown != null)
                {
                    ImGui.TextUnformatted($"Remain: {MathF.Round(eventData.Cooldown.GetRemainingSeconds(), 2)}s");
                }
                if (eventContainer.ShowNameInTooltip)
                {
                    ImGui.TextUnformatted($"Name: {eventTracker.Name}");
                }
                if (eventContainer.ShowDescriptionInTooltip)
                {
                    if (eventContainer.TrimLongDescriptionTooltips && eventTracker.Desc.Length > 120)
                    {
                        ImGui.TextUnformatted($"{eventTracker.Desc.Substring(0, 115)}...");
                    }
                    else
                    {
                        ImGui.TextUnformatted(eventTracker.Desc);
                    }
                }

                ImGui.EndTooltip();
            }
        }

        private static void DrawContainerDeletePrompt()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr("DeleteContainerModalPrompt"));
            ImGui.SetNextWindowPos(ImGui.GetCenter(ImGui.GetWindowViewport()), ImGuiCond.Appearing, new Vector2(0.5f,0.5f));
            if (ImGui.BeginPopupModal("Delete Container?###DeleteContainerModal", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextUnformatted($"The selected Container ('{ActiveTrackerContainer.ContainerName}') has {ActiveTrackerContainer.EventTrackers.Count} Tracker(s) in it.");
                ImGui.NewLine();
                ImGui.TextUnformatted("Are you sure you want to delete the Container?");
                ImGui.NewLine();
                ImGui.Separator();
                if (ImGui.Button("Yes", new Vector2(140, 0)))
                {
                    DeleteActiveContainer();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 140);
                if (ImGui.Button("No", new Vector2(140, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }

        private static void OpenContainerDeletePrompt()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr("DeleteContainerModalPrompt"));
            ImGui.OpenPopup("###DeleteContainerModal");
            ImGui.PopID();
        }

        private static void DeleteActiveContainer()
        {
            // TODO: Select the prior Container, otherwise selected the next one in the list (meaning we were at index 0 when deleting)

            EventTrackerContainers.Remove(ActiveTrackerContainer.IdTracker);

            ActiveTrackerContainer = null;

            ActiveTrackedEventEntry = null;
            ActiveTrackedEventEntryIdx = -1;
        }

        private static void DrawPresetManagerWindow()
        {
            if (!IsPresetManagerOpened)
            {
                return;
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr("EventTrackerPresetManager"));
            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 350), new Vector2(ImGui.GETFLTMAX()));
            ImGui.SetNextWindowSize(new Vector2(400, 650), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Preset Manager###EventTrackerPresetManagerWindow", ref IsPresetManagerOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                if (ShouldPresetManagerFocusNext)
                {
                    if (Utils.CheckIfViewportValid())
                    {
                        ShouldPresetManagerFocusNext = false;
                        Utils.BringWindowToFront();
                    }
                }

                if (IsPresetManagerInContainerMode)
                {
                    DrawContainerPresetManager();
                }
                else
                {
                    DrawTrackerPresetManager();
                }

                ImGui.NewLine();

                if (ImGui.Button("Close", new Vector2(-1, 0)))
                {
                    IsPresetManagerOpened = false;
                }

                ImGui.End();
            }
            ImGui.PopID();
        }

        private static void DrawContainerPresetManager()
        {
            ImGui.TextUnformatted("Container Presets:");
            if (ImGui.BeginListBox("##ContainerPresetsList", new Vector2(ImGui.GetContentRegionAvail().X, -(ImGui.GetItemRectSize().Y * 12))))
            {
                int idx = 0;
                foreach (var container in PresetContainersList)
                {
                    bool isSelected = SelectedPresetManagerContainerIdx == idx;
                    var highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                    if (ImGui.Selectable($"{container.ContainerName}##Preset_{idx}", isSelected, ImGuiSelectableFlags.SpanAllColumns | highlight))
                    {
                        SelectedPresetManagerContainerIdx = idx;
                    }
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.TextUnformatted($"Trackers: {container.EventTrackers.Count}");

                        ImGui.TextUnformatted($"Source Type: {container.SourceLocationType}");

                        if (container.EventTrackers.Count > 0)
                        {
                            bool ctrlHeld = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
                            if (!ctrlHeld)
                            {
                                ImGui.TextUnformatted("Hold CTRL to see list of all Trackers.");
                            }
                            else
                            {
                                ImGui.Indent();
                                foreach (var tracker in container.EventTrackers)
                                {
                                    ImGui.TextUnformatted($"{tracker.Value.Name}");
                                }
                                ImGui.Unindent();
                            }
                        }

                        ImGui.EndTooltip();
                    }

                    idx++;
                }

                ImGui.EndListBox();
            }

            ImGui.BeginDisabled(SelectedPresetManagerContainerIdx == -1);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
            if (ImGui.Button("Create Container From Preset", new Vector2(-1, 0)))
            {
                ActiveTrackerContainer = (TrackerContainer)PresetContainersList.ElementAt(SelectedPresetManagerContainerIdx).Clone(++PersistentContainerCount, ref PersistentTrackerCount);
                ActiveTrackerContainer.SourceLocationType = ESourceLocationType.Manual;
                EventTrackerContainers.Add(ActiveTrackerContainer.IdTracker, ActiveTrackerContainer);

                ActiveTrackedEventEntry = null;
                ActiveTrackedEventEntryIdx = -1;

                foreach (var newTracker in ActiveTrackerContainer.EventTrackers)
                {
                    newTracker.Value.UpdateIconData(newTracker.Value.OriginalIconPath, false);
                }

                ActiveTrackerContainer.RecheckTrackerStates();
            }
            ImGui.PopStyleColor();
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Creates a new Container based on the selected Preset.");

            ImGui.BeginDisabled(ActiveTrackerContainer == null);
            if (ImGui.Button("Create Preset From Selected Container", new Vector2(-1, 0)))
            {
                uint tempTrackerId = 0;
                PresetContainersList.Add((TrackerContainer)ActiveTrackerContainer.Clone(0, ref tempTrackerId));
            }
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Creates a new Preset from the currently selected Container in the Event Tracker window.");

            ImGui.BeginDisabled(SelectedPresetManagerContainerIdx == -1);
            if (ImGui.Button("Copy Preset To Clipboard", new Vector2(-1, 0)))
            {
                ImGui.SetClipboardText(JsonConvert.SerializeObject(PresetContainersList.ElementAt(SelectedPresetManagerContainerIdx)));
            }
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Copies the selected Preset data to your clipboard.");

            if (ImGui.Button("Import Preset From Clipboard", new Vector2(-1, 0)))
            {
                try
                {
                    uint tempTrackerId = 0;
                    var newContainer = JsonConvert.DeserializeObject<TrackerContainer>(ImGui.GetClipboardTextS());
                    if (newContainer != null)
                    {
                        if (string.IsNullOrEmpty(newContainer.ContainerName))
                        {
                            throw new FormatException("Imported Container was missing required data (ContainerName is null).");
                        }

                        newContainer.SourceLocationType = ESourceLocationType.Manual;
                        PresetContainersList.Add((TrackerContainer)newContainer.Clone(0, ref tempTrackerId));
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error attempting to import an Event Tracker Container Preset from clipboard. Will retry as Dictionary of Containers...");

                    try
                    {
                        uint tempTrackerId = 0;
                        var newContainers = JsonConvert.DeserializeObject<Dictionary<uint, TrackerContainer>>(ImGui.GetClipboardTextS());
                        if (newContainers != null)
                        {
                            foreach (var newContainer in newContainers)
                            {
                                if (string.IsNullOrEmpty(newContainer.Value.ContainerName))
                                {
                                    throw new FormatException("Imported Container was missing expected required data.");
                                }

                                newContainer.Value.SourceLocationType = ESourceLocationType.Manual;
                                PresetContainersList.Add((TrackerContainer)newContainer.Value.Clone(0, ref tempTrackerId));
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        Serilog.Log.Error(ex2, "Error attempting to import Event Tracker Container Preset as Dictionary.");
                    }
                }
            }
            ImGui.SetItemTooltip("Imports a Preset from data on your clipboard.");

            ImGui.NewLine();

            ImGui.BeginDisabled(SelectedPresetManagerContainerIdx == -1);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
            if (ImGui.Button("Delete Selected Preset", new Vector2(-1, 0)))
            {
                PresetContainersList.RemoveAt(SelectedPresetManagerContainerIdx);
                SelectedPresetManagerContainerIdx = -1;
            }
            ImGui.PopStyleColor();
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Deletes the currently selected Preset.");
        }

        private static void DrawTrackerPresetManager()
        {
            ImGui.TextUnformatted("Tracker Presets:");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##PresetManagerTrackersFilterText", "Filter Text", ref PresetManagerTrackersFilterText, 128);
            ImGui.SetItemTooltip("Filter the Preset List by Tracker Name.");
            var y = ImGui.GetContentRegionAvail().Y;
            var z = ImGui.GetItemRectSize().Y;
            if (ImGui.BeginListBox("##TrackerPresetsList", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - (ImGui.GetItemRectSize().Y * 8.5f))))//-(ImGui.GetItemRectSize().Y * 12))))
            {
                int idx = 0;
                foreach (var tracker in PresetTrackersList.Where(x => x.TrackerName.Contains(PresetManagerTrackersFilterText, StringComparison.OrdinalIgnoreCase)))
                {
                    bool isSelected = SelectedPresetManagerTracker == tracker;
                    var highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                    if (ImGui.Selectable($"{tracker.TrackerName}##Preset_{idx}", isSelected, ImGuiSelectableFlags.SpanAllColumns | highlight))
                    {
                        SelectedPresetManagerTracker = tracker;
                    }
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.TextUnformatted($"Source Type: {tracker.SourceLocationType}");
                        bool ctrlHeld = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
                        if (ctrlHeld)
                        {
                            ImGui.TextUnformatted($"Source Id: {tracker.SourceLocationId}");
                        }

                        if (tracker.TrackerType == ETrackerType.Buffs)
                        {
                            ImGui.TextUnformatted($"BuffId: {tracker.TrackedBuffId}");
                            ImGui.TextUnformatted($"BuffName: {tracker.Name}");
                        }
                        else if (tracker.TrackerType == ETrackerType.Skills)
                        {
                            ImGui.TextUnformatted($"SkillId: {tracker.TrackedSkillId}");
                            ImGui.TextUnformatted($"SkillName: {tracker.Name}");
                        }

                        ImGui.EndTooltip();
                    }

                    idx++;
                }

                ImGui.EndListBox();
            }

            bool hasSingleItem = ActiveTrackerContainer.ContainerLayoutStyle == EContainerLayoutStyle.SingleItem && ActiveTrackerContainer.EventTrackers.Count > 0;
            ImGui.BeginDisabled(hasSingleItem || ActiveTrackerContainer == null || SelectedPresetManagerTracker == null);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
            if (ImGui.Button("Create Tracker From Preset", new Vector2(-1, 0)))
            {
                var newTracker = (TrackedEventEntry)SelectedPresetManagerTracker.Clone(++PersistentTrackerCount);
                newTracker.SourceLocationType = ESourceLocationType.Manual;
                ActiveTrackerContainer.EventTrackers.Add(newTracker.IdTracker, newTracker);

                ActiveTrackedEventEntry = ActiveTrackerContainer.EventTrackers[newTracker.IdTracker];
                ActiveTrackedEventEntryIdx = ActiveTrackerContainer.EventTrackers.Count - 1;
                ActiveTrackedEventEntry.UpdateIconData(ActiveTrackedEventEntry.OriginalIconPath, false);
                ActiveTrackerContainer.RecheckTrackerStates();
            }
            ImGui.PopStyleColor();
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Creates a new Tracker for the currently selected Container, based on the selected Preset.");

            ImGui.BeginDisabled(ActiveTrackedEventEntry == null);
            if (ImGui.Button("Create Preset From Selected Tracker", new Vector2(-1, 0)))
            {
                var newTracker = (TrackedEventEntry)ActiveTrackedEventEntry.Clone(0);
                newTracker.SourceLocationType = ESourceLocationType.Manual;
                PresetTrackersList.Add(newTracker);
            }
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Creates a new Preset from the currently selected Tracker in the Event Tracker window.");

            ImGui.BeginDisabled(SelectedPresetManagerTracker == null);
            if (ImGui.Button("Copy Preset To Clipboard", new Vector2(-1, 0)))
            {
                var newTracker = (TrackedEventEntry)SelectedPresetManagerTracker.Clone(0);
                newTracker.SourceLocationType = ESourceLocationType.Manual;
                ImGui.SetClipboardText(JsonConvert.SerializeObject(newTracker));
            }
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Copies the selected Preset data to your clipboard.");

            if (ImGui.Button("Import Preset From Clipboard", new Vector2(-1, 0)))
            {
                try
                {
                    var newTracker = JsonConvert.DeserializeObject<TrackedEventEntry>(ImGui.GetClipboardTextS());
                    if (newTracker != null)
                    {
                        if (string.IsNullOrEmpty(newTracker.TrackerName))
                        {
                            throw new FormatException("Imported Tracker was missing required data (TrackerName is null).");
                        }
                        newTracker.SourceLocationType = ESourceLocationType.Manual;
                        PresetTrackersList.Add((TrackedEventEntry)newTracker.Clone(0));
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error attempting to import an Event Tracker Preset from clipboard.");
                }
            }
            ImGui.SetItemTooltip("Imports a Preset from data on your clipboard.");

            ImGui.NewLine();

            ImGui.BeginDisabled(SelectedPresetManagerTracker == null);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
            if (ImGui.Button("Delete Selected Preset", new Vector2(-1, 0)))
            {
                if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                {
                    PresetTrackersList.Clear();
                    SelectedPresetManagerTracker = null;
                }
                else
                {
                    PresetTrackersList.Remove(SelectedPresetManagerTracker);
                    SelectedPresetManagerTracker = null;
                }
            }
            ImGui.PopStyleColor();
            ImGui.EndDisabled();
            if (ImGui.BeginItemTooltip())
            {
                ImGui.TextUnformatted("Deletes the currently selected Preset.");
                bool ctrlHeld = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
                if (ctrlHeld)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green_Transparent);
                }
                ImGui.TextUnformatted("Hold CTRL to delete ALL Presets.");
                if (ctrlHeld)
                {
                    ImGui.PopStyleColor();
                }
                ImGui.EndTooltip();
            }
        }

        private static void OpenPresetManagerWindow()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr("EventTrackerPresetManager"));
            IsPresetManagerOpened = true;
            ImGui.OpenPopup("###EventTrackerPresetManagerWindow");
            ImGui.PopID();
            ShouldPresetManagerFocusNext = true;
        }

        public static void Draw(MainWindow mainWindow)
        {
            InitializeBindings();

            DrawTrackers();

            if (ShowDebugLogWindow)
            {
                ImGui.SetNextWindowSize(new Vector2(450, 300), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Event Tracker Debug Log", ref ShowDebugLogWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
                {
                    ImGui.Checkbox("Debug Log Scene Events", ref DebugAllSceneEvents);
                    ImGui.Separator();
                    ImGui.BeginChild("##DebugLogList", ImGui.GetContentRegionAvail(), ImGuiWindowFlags.HorizontalScrollbar);
                    foreach (var logItem in DebugEventTrackerLog)
                    {
                        ImGui.TextUnformatted(logItem);
                        if (DebugLogAutoScroll)
                        {
                            ImGui.SetScrollHereY(1.0f);
                        }
                    }

                    ImGui.EndChild();
                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.MenuItem("Auto Scroll", DebugLogAutoScroll))
                        {
                            DebugLogAutoScroll = !DebugLogAutoScroll;
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Copy Log To Clipboard"))
                        {
                            ImGui.SetClipboardText(string.Join("\n", DebugEventTrackerLog));
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Clear Log"))
                        {
                            DebugEventTrackerLog.Clear();
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.End();
                }
            }

            if (!IsOpened)
            {
                return;
            }

            var windowSettings = Settings.Instance.WindowSettings.EventTracker;

            ImGui.SetNextWindowSizeConstraints(new Vector2(1020, 500), new Vector2(ImGui.GETFLTMAX()));

            ImGui.SetNextWindowSize(new Vector2(1200, 850), ImGuiCond.FirstUseEver);

            if (windowSettings.WindowPosition != new Vector2())
            {
                ImGui.SetNextWindowPos(windowSettings.WindowPosition, ImGuiCond.FirstUseEver);
            }
            else
            {
                var glfwMonitor = Hexa.NET.GLFW.GLFW.GetPrimaryMonitor();
                var glfwVidMode = Hexa.NET.GLFW.GLFW.GetVideoMode(glfwMonitor);
                Vector2 centerPoint = new Vector2(MathF.Floor(glfwVidMode.Width * 0.5f), MathF.Floor(glfwVidMode.Height * 0.5f));
                ImGui.SetNextWindowPos(centerPoint, ImGuiCond.FirstUseEver, new Vector2(0.5f, 0.5f));
            }

            if (windowSettings.WindowSize != new Vector2())
            {
                ImGui.SetNextWindowSize(windowSettings.WindowSize, ImGuiCond.FirstUseEver);
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            ImGuiWindowFlags exWindowFlags = ImGuiWindowFlags.None;
            if (AppState.MousePassthrough && windowSettings.TopMost)
            {
                exWindowFlags |= ImGuiWindowFlags.NoInputs;
            }

            // Reset Drag and Drop target data each frame so we can safely update it on demand
            DragDropTargetName = "";

            if (ImGui.Begin($"{TITLE}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | exWindowFlags))
            {
                ShouldTrackOpenState = true;

                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();

                    if (windowSettings.TopMost && !IsPinned)
                    {
                        IsPinned = true;
                        Utils.SetWindowTopmost();
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }
                else if (RunOnceDelayed >= 2)
                {
                    if (windowSettings.TopMost && LastPinnedOpacity != windowSettings.Opacity)
                    {
                        Utils.SetWindowOpacity(windowSettings.Opacity * 0.01f);
                        LastPinnedOpacity = windowSettings.Opacity;
                    }
                }

                DrawMenuBar();

                if (ImGui.BeginTable("##EventTrackerSections", 2, ImGuiTableFlags.BordersInnerV, new Vector2(0, 0)))
                {
                    ImGui.TableSetupColumn("##ContainersColumn", ImGuiTableColumnFlags.WidthFixed, 1);
                    ImGui.TableSetupColumn("##TrackersColumn", ImGuiTableColumnFlags.WidthStretch, 1);

                    ImGui.TableNextColumn();

                    TrackerContainer? duplicateContainer = null;

                    ImGui.SeparatorText("Tracker Containers");

                    if (IsPresetManagerInContainerMode)
                    {
                        DrawPresetManagerWindow();
                    }

                    if (ImGui.BeginListBox("##ContainersListBox", new Vector2(ImGui.GetContentRegionAvail().X, -100)))
                    {
                        bool queuedReorder = false;
                        int movingIdx = -1;
                        int targetIdx = -1;
                        int containerIdx = 0;
                        foreach (var container in EventTrackerContainers)
                        {
                            bool isSelected = ActiveTrackerContainer == container.Value;
                            ImGuiSelectableFlags highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                            if (ImGui.Checkbox($"##ContainerIsEnabledCB_{container.Value.IdTracker}", ref container.Value.IsContainerEnabled))
                            {
                                container.Value.HadTransparentBackground = false;
                                container.Value.LastSetOpacity = 100;
                            }
                            ImGui.SetItemTooltip("Is Container Enabled");
                            ImGui.PopStyleVar();
                            ImGui.SameLine();
                            if (ImGui.Selectable($"{container.Value.ContainerName}##container_{container.Value.IdTracker}", isSelected, ImGuiSelectableFlags.None | highlight))
                            {
                                if (!isSelected)
                                {
                                    ActiveTrackerContainer = container.Value;

                                    ActiveTrackedEventEntry = null;
                                    ActiveTrackedEventEntryIdx = -1;
                                }
                            }
                            if (ImGui.BeginDragDropSource())
                            {
                                unsafe
                                {
                                    ImGui.SetDragDropPayload("ContainerReorder", (void*)IntPtr.Zero, 0);
                                }

                                ActiveTrackerContainer = container.Value;

                                ActiveTrackedEventEntry = null;
                                ActiveTrackedEventEntryIdx = -1;

                                ImGui.TextUnformatted(container.Value.ContainerName);

                                ImGui.EndDragDropSource();
                            }

                            ImGui.PushStyleColor(ImGuiCol.DragDropTarget, Colors.LightBlue_Transparent);
                            if (ImGui.BeginDragDropTarget())
                            {
                                var payload = ImGui.AcceptDragDropPayload("TrackedEvent", ImGuiDragDropFlags.AcceptBeforeDelivery);

                                var reorder_payload = ImGui.AcceptDragDropPayload("ContainerReorder", ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);

                                if (!reorder_payload.IsNull)
                                {
                                    var min = ImGui.GetItemRectMin();// - ImGui.GetStyle().ItemSpacing;
                                    var max = ImGui.GetItemRectMax();// + ImGui.GetStyle().ItemSpacing;
                                    float midY = (min.Y + max.Y) * 0.5f;
                                    float mouseY = ImGui.GetIO().MousePos.Y;
                                    bool insertBefore = mouseY < midY;

                                    float lineY = insertBefore ? min.Y : max.Y;

                                    ImGui.GetWindowDrawList().AddLine(new Vector2(min.X, lineY), new Vector2(max.X, lineY), ImGui.ColorConvertFloat4ToU32(Colors.LightBlue_Transparent), 2.0f);

                                    if (reorder_payload.IsDelivery())
                                    {
                                        var tempList = EventTrackerContainers.ToList();

                                        for (int i = 0; i < tempList.Count; i++)
                                        {
                                            if (tempList[i].Key == ActiveTrackerContainer.IdTracker)
                                            {
                                                movingIdx = i;
                                                break;
                                            }
                                        }

                                        queuedReorder = true;
                                        targetIdx = insertBefore ? containerIdx : containerIdx + 1;
                                        if (movingIdx < containerIdx)
                                        {
                                            targetIdx -= 1;
                                        }
                                    }
                                }

                                if (!payload.IsNull)
                                {
                                    DragDropTargetName = container.Value.ContainerName;

                                    if (payload.IsDelivery())
                                    {
                                        // If Control is held when the drop occurs, we Copy instead of Move the data
                                        if (!ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                                        {
                                            if (container.Value.EventTrackers.ContainsKey(ActiveTrackedEventEntry.IdTracker))
                                            {
                                                // User is trying to drag and drop the Tracker onto it's owning Container while in Move mode
                                            }
                                            else
                                            {
                                                // Since we're only moving the Tracker, we don't need to change any Id data

                                                // Add the tracker to the container we dropped onto, without changing the active container
                                                container.Value.EventTrackers.Add(ActiveTrackedEventEntry.IdTracker, ActiveTrackedEventEntry);

                                                // Remove all version of the Tracker from it's prior Container
                                                ActiveTrackerContainer.EventTrackers.Remove(ActiveTrackedEventEntry.IdTracker);
                                                if (ActiveTrackedEventEntryIdx > 0)
                                                {
                                                    ActiveTrackedEventEntryIdx = ActiveTrackedEventEntryIdx - 1;
                                                    ActiveTrackedEventEntry = ActiveTrackerContainer.EventTrackers.ElementAt(ActiveTrackedEventEntryIdx).Value;
                                                }
                                                else if (ActiveTrackedEventEntryIdx == 0)
                                                {
                                                    if (ActiveTrackerContainer.EventTrackers.Count > 0)
                                                    {
                                                        ActiveTrackedEventEntryIdx = 0;
                                                        ActiveTrackedEventEntry = ActiveTrackerContainer.EventTrackers.ElementAt(ActiveTrackedEventEntryIdx).Value;
                                                    }
                                                    else
                                                    {
                                                        ActiveTrackedEventEntryIdx = -1;
                                                        ActiveTrackedEventEntry = null;
                                                    }
                                                }

                                                // Revalidate the source Container as it may no longer have any valid active Trackers
                                                ActiveTrackerContainer.RecheckTrackerStates();
                                            }
                                        }
                                        else
                                        {
                                            // Performing a Copy operation

                                            // Convert down into a valid tracker for this session
                                            var newTracker = (TrackedEventEntry)ActiveTrackedEventEntry.Clone(++PersistentTrackerCount);

                                            // Add the tracker to the container we dropped onto, without changing the active container
                                            container.Value.EventTrackers.Add(newTracker.IdTracker, newTracker);

                                            // Validate the new tracker data
                                            newTracker.UpdateIconData(newTracker.OriginalIconPath, false);
                                        }

                                        container.Value.RecheckTrackerStates();
                                    }
                                }
                                
                                ImGui.EndDragDropTarget();
                            }
                            ImGui.PopStyleColor();
                            ImGui.SetItemTooltip($"Trackers: {container.Value.EventTrackers.Count}");
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.MenuItem("Copy Container To Clipboard"))
                                {
                                    uint tempTrackerId = 0;
                                    var newContainer = ((TrackerContainer)container.Value.Clone(0, ref tempTrackerId));
                                    ImGui.SetClipboardText(JsonConvert.SerializeObject(newContainer));
                                }
                                ImGui.SetItemTooltip($"Copies Container '{container.Value.ContainerName}' like a Preset to the clipboard.");

                                ImGui.Separator();

                                if (ImGui.MenuItem("Duplicate Container"))
                                {
                                    duplicateContainer = container.Value;
                                }
                                ImGui.SetItemTooltip($"Duplicates Container '{container.Value.ContainerName}' including all of the Trackers inside it.");

                                ImGui.EndPopup();
                            }

                            containerIdx++;
                        }
                        ImGui.EndListBox();
                        if (queuedReorder)
                        {
                            queuedReorder = false;

                            var tempList = EventTrackerContainers.ToList();

                            var item = tempList[movingIdx];
                            tempList.RemoveAt(movingIdx);
                            tempList.Insert(targetIdx, item);

                            EventTrackerContainers = tempList.ToDictionary();

                            movingIdx = -1;
                            targetIdx = -1;
                        }
                    }
                    if (duplicateContainer != null)
                    {
                        ActiveTrackerContainer = (TrackerContainer)duplicateContainer.Clone(++PersistentContainerCount, ref PersistentTrackerCount);
                        ActiveTrackerContainer.ContainerName = $"{duplicateContainer.ContainerName} Copy [{PersistentContainerCount}]";
                        EventTrackerContainers.Add(ActiveTrackerContainer.IdTracker, ActiveTrackerContainer);

                        ActiveTrackedEventEntry = null;
                        ActiveTrackedEventEntryIdx = -1;

                        duplicateContainer = null;

                        ActiveTrackerContainer.RecheckTrackerStates();
                    }

                    ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
                    if (ImGui.Button("Create New Tracker Container", new Vector2(230, 0)))
                    {
                        ActiveTrackerContainer = new TrackerContainer(++PersistentContainerCount);
                        ActiveTrackerContainer.ContainerName = $"Tracker Container {PersistentContainerCount}";
                        EventTrackerContainers.Add(ActiveTrackerContainer.IdTracker, ActiveTrackerContainer);

                        ActiveTrackedEventEntry = null;
                        ActiveTrackedEventEntryIdx = -1;
                    }
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    DrawContainerDeletePrompt();
                    ImGui.BeginDisabled(ActiveTrackerContainer == null);
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 230);
                    ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
                    if (ImGui.Button("Delete Selected Tracker Container", new Vector2(230, 0)))
                    {
                        if (ActiveTrackerContainer.EventTrackers.Count > 0)
                        {
                            OpenContainerDeletePrompt();
                        }
                        else
                        {
                            DeleteActiveContainer();
                        }
                    }
                    ImGui.PopStyleColor();
                    ImGui.EndDisabled();

                    ImGui.SetCursorPosX(((ImGui.GetContentRegionAvail().X - 230) / 2.0f));
                    if (ImGui.Button("Container Preset Manager", new Vector2(230, 0)))
                    {
                        IsPresetManagerInContainerMode = true;
                        OpenPresetManagerWindow();
                    }
                    ImGui.SetItemTooltip("Allows you to add new Trackers from Presets or save existing Trackers as new Presets.");

                    ImGui.TableNextColumn();

                    if (ImGui.BeginChild("##ContainerColumn", new Vector2(0, ImGui.GetContentRegionAvail().Y)))
                    {
                        if (ActiveTrackerContainer != null)
                        {
                            DrawContainerOptions();

                            if (ActiveTrackedEventEntry != null)
                            {
                                //ImGui.SeparatorText("Tracker Settings");
                                if (ImGui.CollapsingHeader("Tracker Settings", ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    ImGui.Indent();
                                    DrawTrackerOptions();
                                    ImGui.Unindent();
                                }
                            }
                        }
                        else
                        {
                            ImGui.TextUnformatted("No Container Selected");
                        }

                        ImGui.NewLine();

                        ImGui.EndChild();
                    }

                    ImGui.EndTable();
                }

                windowSettings.WindowPosition = ImGui.GetWindowPos();
                windowSettings.WindowSize = ImGui.GetWindowSize();

                ImGui.End();
            }

            if (!IsOpened && ShouldTrackOpenState)
            {
                ShouldTrackOpenState = false;
                // Window is closing

                SaveContainersToFile();
                SaveContainerPresetsToFile();
                SaveTrackerPresetsToFile();
            }

            ImGui.PopID();
        }

        static float MenuBarButtonWidth = 0.0f;
        private static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                var windowSettings = Settings.Instance.WindowSettings.EventTracker;

                MenuBarSize = ImGui.GetWindowSize();

                ImGui.TextUnformatted(TITLE);

                if (windowSettings.IsContainerEditMode)
                {
                    if (AppState.PlayerUUID != 0 && EncounterManager.Current != null)
                    {
                        if (EncounterManager.Current.Entities.TryGetValue(AppState.PlayerUUID, out var playerEnt))
                        {
                            var attrCombatState = playerEnt.GetAttrKV("AttrCombatState") as int?;
                            if (attrCombatState != null && attrCombatState > 0)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
                                ImGui.TextUnformatted("[WARNING: Edit Mode Is Enabled. Trackers May Not Behave Correctly!]");
                                ImGui.PopStyleColor();
                            }
                        }
                    }
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 4) - (ImGui.GetStyle().ItemSpacing.X * 3));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, windowSettings.IsContainerEditMode ? Colors.Red * new Vector4(1, 1, 1, 0.75f) : Colors.White);
                if (ImGui.MenuItem($"{FASIcons.Pen}##ForceEditModeBtn"))
                {
                    windowSettings.IsContainerEditMode = !windowSettings.IsContainerEditMode;
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.SetItemTooltip("Toggles Container Editing Mode.");

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 3) - (ImGui.GetStyle().ItemSpacing.X * 2));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                ImGui.PushStyleColor(ImGuiCol.Text, ForceHideAllContainers ? Colors.Red * new Vector4(1, 1, 1, 0.75f) : Colors.White);
                if (ImGui.MenuItem($"{(ForceHideAllContainers ? FASIcons.EyeSlash : FASIcons.Eye)}##ForceToggleVisibilityBtn"))
                {
                    ToggleForceHideAllContainers(!ForceHideAllContainers);
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                if (ForceHideAllContainers)
                {
                    ImGui.SetItemTooltip("Disables forcefully hiding Containers.");
                }
                else
                {
                    ImGui.SetItemTooltip("Forcefully hide all Containers.");
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth * 2) - ImGui.GetStyle().ItemSpacing.X);
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"{FASIcons.Gear}##SettingsBtn"))
                {
                    ImGui.SetNextWindowPos(ImGui.GetItemRectMax(), ImGuiCond.Appearing, new Vector2(1, 0));
                    ImGui.OpenPopup("##EventTrackerSettingsMenu");
                }
                ImGui.PopFont();
                if (ImGui.BeginPopup("##EventTrackerSettingsMenu"))
                {
                    ImGui.Checkbox("Container Edit Mode (Allows Movement)", ref windowSettings.IsContainerEditMode);

                    ImGui.Checkbox("Show Edit Mode Placeholders", ref windowSettings.EditModeShowPlaceholders);
                    ImGui.SetItemTooltip("Attempts to add Placeholder entries for Trackers while in Edit Mode to help with visualizing them.");

                    ImGui.Checkbox("Hide Containers When Game Not Focused", ref windowSettings.HideContainersWhenGameNotFocused);
                    ImGui.SetItemTooltip("Automatically hides all Containers when the game isn't in focus regardless of Container and Tracker settings.");
                    ImGui.Indent();
                    ImGui.BeginDisabled(!windowSettings.HideContainersWhenGameNotFocused);
                    ImGui.Checkbox("Keep Containers When ZDPS Focused", ref windowSettings.KeepContainersWhenZDPSFocused);
                    ImGui.SetItemTooltip("Having a ZDPS window in focus will prevent the Containers from being automatically hidden due to the game no longer being in focus.");
                    ImGui.EndDisabled();
                    ImGui.Unindent();

                    ImGui.Checkbox("Containers Always Ignore Input (Excluding Edit Mode)", ref windowSettings.AlwaysIgnoreInputs);
                    ImGui.SetItemTooltip("All input/mouse events will be ignored for Containers unless in Edit Mode.\nOtherwise, 'Pinned Window Clickthrough' (Mouse Passthrough) must be toggled on via Hotkey Keybind in Settings.");

                    ImGui.Checkbox("Show 'Force Hide Containers' Button On Main Window", ref windowSettings.ShowForceHideContainersBtnOnMainWindow);

                    ImGui.Separator();

                    if (ImGui.MenuItem("Reload Internal Presets"))
                    {
                        LoadDefaultPresets(true);
                    }
                    ImGui.SetItemTooltip("Adds the Internal Preset Trackers and Containers back to the Preset Lists\nNote: This may cause duplicate entries. Internal Presets will be put at the top of the list.");

                    ImGui.Separator();

                    ImGui.Checkbox("Show Debug Log", ref ShowDebugLogWindow);
                    ImGui.SetItemTooltip("Log Window for Scene Events and when a Tracker has 'Debug Tracker' Enabled.");

                    ImGui.EndPopup();
                }

                ImGui.SetCursorPosX(MenuBarSize.X - (MenuBarButtonWidth));
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                if (ImGui.MenuItem($"X##CloseBtn"))
                {
                    windowSettings.WindowPosition = ImGui.GetWindowPos();
                    windowSettings.WindowSize = ImGui.GetWindowSize();
                    IsOpened = false;
                }
                ImGui.PopFont();

                MenuBarButtonWidth = ImGui.GetItemRectSize().X;

                ImGui.EndMenuBar();
            }
        }

        private static void DrawContainerOptions()
        {
            ImGui.SeparatorText("Container Settings");
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Container Name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##ContainerNameInput", ref ActiveTrackerContainer.ContainerName, 128))
            {
                ActiveTrackerContainer.IsWindowTitleDirty = true;
            }

            ImGui.Checkbox("Show Container Name", ref ActiveTrackerContainer.ShowContainerName);
            ImGui.SetItemTooltip("Controls if the Container Name should be displayed at the top of the Tracker Container window.");

            ImGui.SameLine();

            if (ImGui.Checkbox("Is Container Enabled", ref ActiveTrackerContainer.IsContainerEnabled))
            {
                ActiveTrackerContainer.HadTransparentBackground = false;
                ActiveTrackerContainer.LastSetOpacity = 100;
                ActiveTrackerContainer.IsWindowTitleDirty = true;
            }
            ImGui.SetItemTooltip("Controls if the entire Container is Enabled and should be shown.");

            ImGui.SameLine();

            if (ImGui.Checkbox("Show In Task Bar", ref ActiveTrackerContainer.ShowInTaskBar))
            {
                ActiveTrackerContainer.IsWindowTitleDirty = true;
            }
            ImGui.SetItemTooltip("Hiding from the Task Bar may prevent screen recording software like OBS from seeing the Container window to capture.\nNote: You may need to toggle the Enabled state of this Container after changing this setting for it to take effect.");

            ImGui.Dummy(new Vector2(0, 0));

            if (ImGui.CollapsingHeader("Container Settings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();

                /*ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Layout Style:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                ImGui.BeginDisabled(ActiveTrackerContainer.EventTrackers.Count > 1);
                if (ImGui.BeginCombo("##LayoutStyleCombo", ActiveTrackerContainer.ContainerLayoutStyle.ToString(), ImGuiComboFlags.None))
                {
                    if (ImGui.Selectable($"{EContainerLayoutStyle.SingleItem.ToString()}"))
                    {
                        ActiveTrackerContainer.ContainerLayoutStyle = EContainerLayoutStyle.SingleItem;
                    }
                    ImGui.SetItemTooltip("A single tracker item window");

                    if (ImGui.Selectable($"{EContainerLayoutStyle.List.ToString()}"))
                    {
                        ActiveTrackerContainer.ContainerLayoutStyle = EContainerLayoutStyle.List;
                    }
                    ImGui.SetItemTooltip("A window with multiple tracker items");

                    ImGui.EndCombo();
                }
                ImGui.EndDisabled();
                if (ActiveTrackerContainer.EventTrackers.Count > 1)
                {
                    ImGui.SetItemTooltip("You have more than 1 Tracker created on this Container.\nYou cannot switch back to Single mode until you delete your extra Trackers in this Container.");
                }*/

                if (ActiveTrackerContainer.ContainerLayoutStyle == EContainerLayoutStyle.List)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Layout List Direction:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.BeginCombo("##LayoutDirectionCombo", ActiveTrackerContainer.ContainerListDirection.ToString(), ImGuiComboFlags.None))
                    {
                        int idx = 0;
                        foreach (var listDirection in System.Enum.GetValues<EContainerListDirection>())
                        {
                            bool isSelected = ActiveTrackerContainer.ContainerListDirection == listDirection;

                            if (ImGui.Selectable($"{listDirection.ToString()}", isSelected))
                            {
                                ActiveTrackerContainer.ContainerListDirection = listDirection;
                            }

                            if (isSelected)
                            {
                                ImGui.SetItemDefaultFocus();
                            }

                            idx++;
                        }
                        ImGui.EndCombo();
                    }
                }

                ImGui.SameLine();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Layout Size Constraints:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.BeginCombo("##LayoutSizeConstraintCombo", ActiveTrackerContainer.ContainerSizeConstraint.ToString(), ImGuiComboFlags.None))
                {
                    if (ImGui.Selectable($"{EContainerSizeConstraint.AutoSize.ToString()}"))
                    {
                        ActiveTrackerContainer.ContainerSizeConstraint = EContainerSizeConstraint.AutoSize;
                        ActiveTrackerContainer.EventWindowSizes = new();
                    }
                    ImGui.SetItemTooltip("Automatically adjusts window size to fit all contents");

                    if (ImGui.Selectable($"{EContainerSizeConstraint.FixedSize.ToString()}"))
                    {
                        ActiveTrackerContainer.ContainerSizeConstraint = EContainerSizeConstraint.FixedSize;
                        ActiveTrackerContainer.EventWindowSizes = new();
                    }
                    ImGui.SetItemTooltip("User defines width and height and anything outside is clipped");

                    ImGui.EndCombo();
                }

                ImGui.SeparatorText("Layout Visuals");
                //ImGui.Indent();
                ImGui.BeginDisabled(ActiveTrackerContainer.ModifiedWindowOpacity);
                ImGui.Checkbox("Transparent Background ", ref ActiveTrackerContainer.TransparentBackground);
                ImGui.EndDisabled();

                ImGui.SameLine();

                ImGui.BeginDisabled(ActiveTrackerContainer.TransparentBackground);
                ImGui.Checkbox("Window Opacity", ref ActiveTrackerContainer.ModifiedWindowOpacity);
                if (ActiveTrackerContainer.ModifiedWindowOpacity)
                {
                    ImGui.Indent();
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                    ImGui.SetNextItemWidth(-1);
                    ImGui.SameLine();
                    ImGui.SliderInt("##Opacity", ref ActiveTrackerContainer.WindowOpacityValue, 20, 100, ImGuiSliderFlags.AlwaysClamp);
                    ImGui.PopStyleColor(2);
                    ImGui.Unindent();
                }
                ImGui.EndDisabled();

                ImGui.Checkbox("Hide Tracker Background", ref ActiveTrackerContainer.HideTrackerBackground);
                ImGui.SetItemTooltip("Removes the background coloring for Trackers.\nWorks well when combined with Transparent Background.");

                ImGui.SameLine();

                ImGui.Checkbox("Hide Tracker Borders", ref ActiveTrackerContainer.HideTrackerBorders);
                ImGui.SetItemTooltip("Removes the borders around Trackers.\nWorks well when combined with Transparent Background.");

                //ImGui.Unindent();

                ImGui.SeparatorText("Tooltips");
                //ImGui.Indent();
                ImGui.Checkbox("Show Caster", ref ActiveTrackerContainer.ShowCasterInTooltip);
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 32);
                ImGui.Checkbox("Show Duration               ", ref ActiveTrackerContainer.ShowDurationInTooltip);
                ImGui.SameLine();
                ImGui.Checkbox("Show Name     ", ref ActiveTrackerContainer.ShowNameInTooltip);
                ImGui.Checkbox("Show Description", ref ActiveTrackerContainer.ShowDescriptionInTooltip);
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2);
                ImGui.Checkbox("Trim Long Descriptions", ref ActiveTrackerContainer.TrimLongDescriptionTooltips);
                ImGui.SetItemTooltip("Descriptions are limited to 120 characters with this is Enabled.");
                //ImGui.Unindent(); 

                ImGui.SeparatorText("Special");
                ImGui.BeginDisabled(ActiveTrackerContainer.ContainerLayoutStyle == EContainerLayoutStyle.SingleItem);
                //ImGui.Indent();
                if (ImGui.Checkbox("Track All Buffs##ContainerTrackAllBuffs", ref ActiveTrackerContainer.TrackAllBuffs))
                {
                    if (ActiveTrackerContainer.TrackAllBuffs && ActiveTrackerContainer.EventTrackers.Count == 0)
                    {
                        ActiveTrackedEventEntry = new TrackedEventEntry(++PersistentTrackerCount);
                        ActiveTrackedEventEntry.TrackerName = $"All Buffs Tracker";
                        ActiveTrackerContainer.EventTrackers.Add(ActiveTrackedEventEntry.IdTracker, ActiveTrackedEventEntry);
                        ActiveTrackedEventEntryIdx = ActiveTrackerContainer.EventTrackers.Count - 1;
                        ActiveTrackedEventEntry.IsEnabled = true;
                        ActiveTrackerContainer.RecheckTrackerStates();
                    }
                }
                ImGui.EndDisabled();
                ImGui.BeginDisabled();
                ImGui.Indent();
                ImGui.TextWrapped("If this is Enabled, the First Tracker in this Container will be used for the settings of all Buffs that get automatically tracked.");
                ImGui.TextWrapped("Any Buff selected within the Tracker will be ignored. Additionally, the Tracker Type MUST be set to Buff.");
                ImGui.Unindent();
                ImGui.Unindent();
                ImGui.EndDisabled();

                if (ImGui.CollapsingHeader("Extra Container Settings"))
                {
                    ImGui.Indent();
                    var windowSettings = Settings.Instance.WindowSettings.EventTracker;

                    if (windowSettings.ContainerPositions.TryGetValue(ActiveTrackerContainer.IdTracker, out var pos))
                    {
                        int[] posArray = { (int)pos.X, (int)pos.Y };
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Container Position:");
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));

                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(300);
                        if (ImGui.SliderInt2("##ContainerPos", ref posArray[0], 0, 9999))
                        {
                            pos[0] = posArray[0];
                            pos[1] = posArray[1];

                            var window = ImGuiP.FindWindowByID(ImGuiP.ImHashStr($"{ActiveTrackerContainer.IdTracker}"));
                            ImGuiP.SetWindowPos(window, pos, ImGuiCond.Always);
                        }
                        ImGui.PopStyleColor(2);

                        ImGui.SameLine();
                        if (ImGui.Button("Reset"))
                        {
                            var glfwMonitor = Hexa.NET.GLFW.GLFW.GetPrimaryMonitor();
                            var glfwVidMode = Hexa.NET.GLFW.GLFW.GetVideoMode(glfwMonitor);
                            Vector2 centerPoint = new Vector2(MathF.Floor(glfwVidMode.Width * 0.5f), MathF.Floor(glfwVidMode.Height * 0.5f));

                            windowSettings.ContainerPositions[ActiveTrackerContainer.IdTracker] = centerPoint;

                            var window = ImGuiP.FindWindowByID(ImGuiP.ImHashStr($"{ActiveTrackerContainer.IdTracker}"));
                            ImGuiP.SetWindowPos(window, centerPoint, ImGuiCond.Always);

                        }
                    }
                    else
                    {
                        ImGui.TextUnformatted("Container has not been shown yet.");
                    }

                    ImGui.Unindent();
                }

                //ImGui.Unindent();
            }
            

            ImGui.SeparatorText("Event Trackers"u8);

            if (ImGui.BeginListBox("##TrackersListbox", new Vector2(ImGui.GetContentRegionAvail().X, 140)))
            {
                int idx = 0;
                foreach (var eventTracker in ActiveTrackerContainer.EventTrackers)
                {
                    bool isSelected = ActiveTrackedEventEntryIdx == idx;
                    ImGuiSelectableFlags highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;

                    if (ImGui.Selectable($"{eventTracker.Value.TrackerName}##TrackerEntry_{idx}", true, ImGuiSelectableFlags.SpanAllColumns | highlight | ImGuiSelectableFlags.AllowOverlap))
                    {
                        ActiveTrackedEventEntryIdx = idx;
                        ActiveTrackedEventEntry = eventTracker.Value;
                    }
                    if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                    {
                        unsafe
                        {
                            ImGui.SetDragDropPayload("TrackedEvent", (void*)IntPtr.Zero, 0);
                        }
                        // Forcefully set the Tracker we're dragging as the Active Selected one so we can query it on the drop event
                        ActiveTrackedEventEntryIdx = idx;
                        ActiveTrackedEventEntry = eventTracker.Value;

                        // Preview element
                        string dropAction = "Moving";
                        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                        {
                            dropAction = "Copying";
                        }
                        ImGui.TextUnformatted($"{dropAction} Tracker '{eventTracker.Value.Name}'{(!string.IsNullOrEmpty(DragDropTargetName) ? $" to '{DragDropTargetName}'" : "")}");

                        ImGui.EndDragDropSource();
                    }
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.TextUnformatted($"IdTracker: {eventTracker.Value.IdTracker}");
                        ImGui.TextUnformatted($"TrackerType: {eventTracker.Value.TrackerType}");

                        if (eventTracker.Value.TrackerType == ETrackerType.Buffs)
                        {
                            ImGui.TextUnformatted($"Buff ID: {eventTracker.Value.TrackedBuffId}");
                            ImGui.TextUnformatted($"Buff Name: {eventTracker.Value.Name}");
                        }
                        else if (eventTracker.Value.TrackerType == ETrackerType.Skills)
                        {
                            ImGui.TextUnformatted($"Skill ID: {eventTracker.Value.TrackedSkillId}");
                            ImGui.TextUnformatted($"Skill Name: {eventTracker.Value.Name}");
                        }
                        else if (eventTracker.Value.TrackerType == ETrackerType.Attributes)
                        {
                            ImGui.TextUnformatted($"Attribute Name: {eventTracker.Value.TrackedAttributeName}");
                        }

                        ImGui.TextUnformatted($"Who To Track: {eventTracker.Value.TrackedEntityType}");

                        ImGui.EndTooltip();
                    }
                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.MenuItem("Copy Tracker To Clipboard"))
                        {
                            // Create a cleaned version of the Tracker that is dependency-free before it is copied
                            var newTracker = (TrackedEventEntry)eventTracker.Value.Clone(0);
                            ImGui.SetClipboardText(JsonConvert.SerializeObject(newTracker));
                        }
                        ImGui.SetItemTooltip($"Copies Tracker '{eventTracker.Value.Name}' like a Preset to the clipboard.");

                        ImGui.EndPopup();
                    }

                    idx++;
                }
                ImGui.EndListBox();
            }

            bool hasSingleTracker = ActiveTrackerContainer.ContainerLayoutStyle == EContainerLayoutStyle.SingleItem && ActiveTrackerContainer.EventTrackers.Count == 1;
            ImGui.BeginDisabled(hasSingleTracker);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
            if (ImGui.Button("Create New Tracker"))
            {
                ActiveTrackedEventEntry = new TrackedEventEntry(++PersistentTrackerCount);
                ActiveTrackedEventEntry.TrackerName = $"Tracker {ActiveTrackedEventEntry.IdTracker}";
                ActiveTrackerContainer.EventTrackers.Add(ActiveTrackedEventEntry.IdTracker, ActiveTrackedEventEntry);
                ActiveTrackedEventEntryIdx = ActiveTrackerContainer.EventTrackers.Count - 1;
                ActiveTrackedEventEntry.IsEnabled = true;
                ActiveTrackerContainer.RecheckTrackerStates();
            }
            ImGui.PopStyleColor();
            ImGui.EndDisabled();
            if (hasSingleTracker)
            {
                ImGui.SetItemTooltip("Container already has a Tracker in it.\nChange the Style to List to support more than one Tracker at a time or make a new Container.");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveTrackedEventEntry == null || ActiveTrackedEventEntryIdx == -1);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
            if (ImGui.Button("Delete Selected Tracker"))
            {
                if (ActiveTrackedEventEntryIdx > 0)
                {
                    ActiveTrackerContainer.EventTrackers.Remove(ActiveTrackedEventEntry.IdTracker);
                    ActiveTrackedEventEntryIdx = ActiveTrackedEventEntryIdx - 1;
                    ActiveTrackedEventEntry = ActiveTrackerContainer.EventTrackers.ElementAt(ActiveTrackedEventEntryIdx).Value;
                }
                else if (ActiveTrackedEventEntryIdx == 0)
                {
                    ActiveTrackerContainer.EventTrackers.Remove(ActiveTrackedEventEntry.IdTracker);
                    if (ActiveTrackerContainer.EventTrackers.Count > 0)
                    {
                        ActiveTrackedEventEntryIdx = 0;
                        ActiveTrackedEventEntry = ActiveTrackerContainer.EventTrackers.ElementAt(ActiveTrackedEventEntryIdx).Value;
                    }
                    else
                    {
                        ActiveTrackedEventEntryIdx = -1;
                        ActiveTrackedEventEntry = null;
                    }
                }
                ActiveTrackerContainer.RecheckTrackerStates();
            }
            ImGui.PopStyleColor();
            ImGui.EndDisabled();

            if (!IsPresetManagerInContainerMode)
            {
                DrawPresetManagerWindow();
            }
            ImGui.SameLine();
            if (ImGui.Button("Tracker Preset Manager"))
            {
                IsPresetManagerInContainerMode = false;
                OpenPresetManagerWindow();
            }
            ImGui.SetItemTooltip("Allows you to add new Trackers from Presets or save existing Trackers as new Presets.");

            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveTrackedEventEntryIdx < 1);
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
            if (ImGui.Button($"{FASIcons.ChevronUp}##TrackerMoveUpBtn"))
            {
                var currentIndex = ActiveTrackerContainer.EventTrackers.IndexOf(ActiveTrackedEventEntry.IdTracker);

                ActiveTrackerContainer.EventTrackers.Remove(ActiveTrackedEventEntry.IdTracker);

                ActiveTrackerContainer.EventTrackers.Insert(currentIndex - 1, ActiveTrackedEventEntry.IdTracker, ActiveTrackedEventEntry);
                ActiveTrackedEventEntryIdx = currentIndex - 1;
            }
            ImGui.PopFont();
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Move Selected Tracker Up.");
            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveTrackedEventEntryIdx == -1 || ActiveTrackedEventEntryIdx == ActiveTrackerContainer.EventTrackers.Count - 1);
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
            if (ImGui.Button($"{FASIcons.ChevronDown}##TrackerMoveDownBtn"))
            {
                var currentIndex = ActiveTrackerContainer.EventTrackers.IndexOf(ActiveTrackedEventEntry.IdTracker);

                ActiveTrackerContainer.EventTrackers.Remove(ActiveTrackedEventEntry.IdTracker);

                ActiveTrackerContainer.EventTrackers.Insert(currentIndex + 1, ActiveTrackedEventEntry.IdTracker, ActiveTrackedEventEntry);
                ActiveTrackedEventEntryIdx = currentIndex + 1;
            }
            ImGui.PopFont();
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("Move Selected Tracker Down.");

            ImGui.NewLine();
        }

        private static void DrawTrackerOptions()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Tracker Name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##TrackerNameInputText", ref ActiveTrackedEventEntry.TrackerName, 256);

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Tracker Type:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##TrackerType", ActiveTrackedEventEntry.TrackerType.ToString(), ImGuiComboFlags.None))
            {
                foreach (var trackerType in System.Enum.GetValues<ETrackerType>())
                {
                    // Disable Attribute types for now, there hasn't been a good reason to actually support them
                    if (trackerType == ETrackerType.Attributes)
                    {
                        continue;
                    }

                    bool isSelected = ActiveTrackedEventEntry.TrackerType == trackerType;

                    if (ImGui.Selectable($"{trackerType.ToString()}", isSelected))
                    {
                        ActiveTrackedEventEntry.TrackerType = trackerType;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.NewLine();

            if (ImGui.BeginTabBar("##TrackerOptionsTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton))
            {
                if (ImGui.BeginTabItem("Tracker##TrackerTypeOptionsTab"))
                {
                    if (ActiveTrackedEventEntry.TrackerType == ETrackerType.Buffs)
                    {
                        DrawBuffTrackerOptions();
                    }
                    else if (ActiveTrackedEventEntry.TrackerType == ETrackerType.Skills)
                    {
                        DrawSkillTrackerOptions();
                    }
                    else if (ActiveTrackedEventEntry.TrackerType == ETrackerType.Attributes)
                    {
                        DrawAttributeTrackerOptions();
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Display Format##DisplayFormatTab"))
                {
                    DrawDisplayFormatOptions();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Load Events##LoadEventsTab"))
                {
                    if (ImGui.Checkbox("Is Tracker Enabled", ref ActiveTrackedEventEntry.IsEnabled))
                    {
                        ActiveTrackerContainer.RecheckTrackerStates();
                    }

                    ImGui.Separator();

                    ImGui.Checkbox("Debug Tracker", ref ActiveTrackedEventEntry.DebugLogTracker);

                    ImGui.Separator();

                    ImGui.NewLine();

                    DrawLoadTimeOptions();

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private static void DrawBuffTrackerOptions()
        {
            ImGui.TextUnformatted("Select Buff To Track:");
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Filter: ");
            ImGui.SameLine();
            ImGui.Checkbox("(Search Descriptions)", ref BuffFilterIncludeDescriptions);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##BuffFilterText", ref BuffFilterText, 128))
            {
                if (BuffFilterText.Length > 0)
                {
                    bool isNum = Char.IsNumber(BuffFilterText[0]);
                    BuffFilterMatches = HelperMethods.DataTables.Buffs.Data.AsValueEnumerable().Where(x =>
                    isNum ? x.Key.Contains(BuffFilterText) : (x.Value.Name.Contains(BuffFilterText, StringComparison.OrdinalIgnoreCase)
                    || (BuffFilterIncludeDescriptions ? x.Value.Desc != null && x.Value.Desc.Contains(BuffFilterText, StringComparison.OrdinalIgnoreCase) : false))).ToArray();
                }
                else
                {
                    BuffFilterMatches = null;
                }
            }
            if (ImGui.BeginListBox("##BuffsListBox", new Vector2(ImGui.GetContentRegionAvail().X, 140)))
            {
                if (BuffFilterText.Length > 0 && BuffFilterMatches != null && BuffFilterMatches.Any())
                {
                    int buffIdx = 0;
                    foreach (var buff in BuffFilterMatches)
                    {
                        bool isSelected = buff.Key == ActiveTrackedEventEntry.TrackedBuffId.ToString();
                        var highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;

                        if (ImGui.Selectable($"##Buff_{buffIdx}_{buff.Key}", true, ImGuiSelectableFlags.SpanAllColumns | highlight))
                        {
                            foreach (var eventData in ActiveTrackedEventEntry.EventData.AsValueEnumerable())
                            {
                                if (eventData.Value.Cooldown != null)
                                {
                                    eventData.Value.Cooldown.EndCooldown();
                                    eventData.Value.Cooldown = null;
                                }
                            }

                            ActiveTrackedEventEntry.EventData.Clear();

                            ActiveTrackedEventEntry.TrackedBuffId = int.Parse(buff.Key);
                            ActiveTrackedEventEntry.Name = buff.Value.Name;
                            ActiveTrackedEventEntry.Desc = buff.Value.Desc;

                            ActiveTrackedEventEntry.UpdateIconData(buff.Value.GetIconName(), true);
                        }
                        if (ImGui.BeginItemTooltip())
                        {
                            //ImGui.TextUnformatted(JsonConvert.SerializeObject(buff.Value, Formatting.Indented));
                            ImGui.TextUnformatted($"Id: {buff.Value.Id}");
                            ImGui.TextUnformatted($"Level: {buff.Value.Level}");
                            ImGui.TextUnformatted($"NameDesign: {buff.Value.NameDesign}");
                            if (!string.IsNullOrEmpty(buff.Value.Note))
                            {
                                ImGui.TextUnformatted($"Note: {buff.Value.Note}");
                            }
                            ImGui.TextUnformatted($"Name: {buff.Value.Name}");
                            if (!string.IsNullOrEmpty(buff.Value.Desc))
                            {
                                ImGui.TextUnformatted($"Desc: {buff.Value.Desc}");
                            }
                            ImGui.TextUnformatted($"Icon: {buff.Value.Icon}");
                            ImGui.TextUnformatted($"BuffType: {buff.Value.BuffType}");
                            if (buff.Value.RepeatAddRule.Count > 0)
                            {
                                ImGui.TextUnformatted($"RepeatAddRule:");
                                foreach (var param in buff.Value.RepeatAddRule)
                                {
                                    ImGui.SameLine();
                                    ImGui.TextUnformatted($" {param},");
                                }
                            }
                            if (buff.Value.DestroyParam.Count > 0)
                            {
                                ImGui.TextUnformatted($"DestroyParam:");
                                foreach (var paramList in buff.Value.DestroyParam)
                                {
                                    string fmt = "";
                                    foreach (var param in paramList)
                                    {
                                        fmt += $" {param},";
                                    }
                                    ImGui.SameLine();
                                    ImGui.TextUnformatted($"[{fmt} ]");
                                }
                            }

                            ImGui.EndTooltip();
                        }

                        ImGui.SameLine();
                        ImGui.TextUnformatted($"[{buff.Value.Id}] {buff.Value.Name}");
                        buffIdx++;
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.NewLine();

            DrawWhoToTrack();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Buff Source Must Be 'Self':");
            ImGui.SameLine();
            ImGui.Checkbox("##BuffSourceMustBeSelf", ref ActiveTrackedEventEntry.EventSourceMustBeSelf);

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Limit To One Tracker Instance:");
            ImGui.SameLine();
            if (ImGui.Checkbox("##LimitToOneTrackerInstance", ref ActiveTrackedEventEntry.LimitToOneTrackerInstance))
            {
                if (ActiveTrackedEventEntry.LimitToOneTrackerInstance)
                {
                    ActiveTrackedEventEntry.EventData.Clear();
                }
            }
            ImGui.SetItemTooltip($"This will clear the current Internal Trackers when Enabled.\nCurrent Internal Trackers Count = {ActiveTrackedEventEntry.EventData.Count}");

            ImGui.NewLine();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("What Events To Track:");
            ImGui.SameLine();
            ImGui.Checkbox("View All Events", ref ShowAllBuffEventsToTrack);
            if (ImGui.BeginListBox("##TrackedBuffEventsListBox", new Vector2(ImGui.GetContentRegionAvail().X, 140)))
            {
                Zproto.EBuffEventType[] eventsList = System.Enum.GetValues<Zproto.EBuffEventType>();
                if (!ShowAllBuffEventsToTrack)
                {
                    eventsList = [
                        Zproto.EBuffEventType.BuffEventAddTo,
                        Zproto.EBuffEventType.BuffEventRemove,
                        Zproto.EBuffEventType.BuffEventReplace,
                        Zproto.EBuffEventType.BuffEventStackLayer,
                        Zproto.EBuffEventType.BuffEventRemoveLayer
                        ];
                }

                foreach (var trackedBuffEvent in eventsList)
                {
                    bool isSelected = ActiveTrackedEventEntry.BuffEvents.Contains(trackedBuffEvent);

                    ImGuiSelectableFlags highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;

                    if (ImGui.Selectable($"{trackedBuffEvent.ToString()}", isSelected, ImGuiSelectableFlags.SpanAllColumns | highlight))
                    {
                        if (isSelected == false)
                        {
                            ActiveTrackedEventEntry.BuffEvents.Add(trackedBuffEvent);
                        }
                        else
                        {
                            ActiveTrackedEventEntry.BuffEvents.Remove(trackedBuffEvent);
                        }
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.NewLine();

            ImGui.Checkbox("Override Duration", ref ActiveTrackedEventEntry.OverrideDuration);
            ImGui.SetItemTooltip("Force the Duration to be a specific value instead of a detected one.");
            if (ActiveTrackedEventEntry.OverrideDuration)
            {
                ImGui.Indent();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Override Value:");
                ImGui.SameLine();
                ImGui.InputInt("##OverrideValueInput", ref ActiveTrackedEventEntry.DurationOverrideValue, ImGuiInputTextFlags.CharsDecimal);
                ImGui.SetItemTooltip("New Duration value to force. Value is in Seconds.");
                ImGui.Unindent();
            }

            ImGui.Checkbox("Ignore Duration", ref ActiveTrackedEventEntry.IgnoreCooldownDuration);
            ImGui.SetItemTooltip("The duration of the Tracker will be ignored. This may prevent some Events from triggering.\nThis can be useful for Buffs that do not make use of a duration to end and instead rely on Layer Count or some other metric.");

            ImGui.BeginDisabled(!ActiveTrackedEventEntry.IgnoreCooldownDuration);
            ImGui.Indent();
            ImGui.Checkbox("Use Layers For Duration", ref ActiveTrackedEventEntry.UseLayersForDuration);
            ImGui.SetItemTooltip("The Buff's Layers count will be used to indicate progress via the Duration Progress Bar.");
            if (ActiveTrackedEventEntry.UseLayersForDuration)
            {
                ImGui.Indent();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Layers Duration Max Value:");
                ImGui.SameLine();
                ImGui.InputInt("##LayersForDurationMaxValue", ref ActiveTrackedEventEntry.LayersForDurationMaxValue, ImGuiInputTextFlags.CharsDecimal);
                ImGui.SetItemTooltip("This value will be used as the max target for the Buff's Layer count to reach.");
                ImGui.Unindent();
            }
            ImGui.Unindent();
            ImGui.EndDisabled();
        }

        private static void DrawSkillTrackerOptions()
        {
            ImGui.TextUnformatted("Select Skill To Track:");
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Filter: ");
            ImGui.SameLine();
            ImGui.Checkbox("(Search Descriptions)", ref SkillFilterIncludeDescriptions);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##SkillFilterText", ref SkillFilterText, 128))
            {
                if (SkillFilterText.Length > 0)
                {
                    bool isNum = Char.IsNumber(SkillFilterText[0]);
                    SkillFilterMatches = HelperMethods.DataTables.Skills.Data.AsValueEnumerable().Where(x =>
                    isNum ? x.Key.Contains(SkillFilterText) : (x.Value.Name.Contains(SkillFilterText, StringComparison.OrdinalIgnoreCase)
                    || (SkillFilterIncludeDescriptions ? x.Value.Desc != null && x.Value.Desc.Contains(SkillFilterText, StringComparison.OrdinalIgnoreCase) : false))).ToArray();
                }
                else
                {
                    SkillFilterMatches = null;
                }
            }
            if (ImGui.BeginListBox("##SkillsListBox", new Vector2(ImGui.GetContentRegionAvail().X, 140)))
            {
                if (SkillFilterText.Length > 0 && SkillFilterMatches != null && SkillFilterMatches.Any())
                {
                    int skillIdx = 0;
                    foreach (var skill in SkillFilterMatches)
                    {
                        bool isSelected = skill.Key == ActiveTrackedEventEntry.TrackedSkillId.ToString();
                        var highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;

                        if (ImGui.Selectable($"##Skill_{skillIdx}_{skill.Key}", true, ImGuiSelectableFlags.SpanAllColumns | highlight))
                        {
                            foreach (var eventData in ActiveTrackedEventEntry.EventData.AsValueEnumerable())
                            {
                                if (eventData.Value.Cooldown != null)
                                {
                                    eventData.Value.Cooldown.EndCooldown();
                                    eventData.Value.Cooldown = null;
                                }
                            }

                            ActiveTrackedEventEntry.EventData.Clear();

                            ActiveTrackedEventEntry.TrackedSkillId = int.Parse(skill.Key);
                            ActiveTrackedEventEntry.Name = skill.Value.Name;
                            ActiveTrackedEventEntry.Desc = skill.Value.Desc;

                            ActiveTrackedEventEntry.UpdateIconData(skill.Value.GetIconName(), true);
                        }
                        if (ImGui.BeginItemTooltip())
                        {
                            ImGui.TextUnformatted($"Id: {skill.Value.Id}");
                            ImGui.TextUnformatted($"NameDesign: {skill.Value.NameDesign}");
                            ImGui.TextUnformatted($"Name: {skill.Value.Name}");
                            ImGui.TextUnformatted($"Desc: {skill.Value.Desc}");
                            ImGui.TextUnformatted($"Icon: {skill.Value.Icon}");
                            ImGui.TextUnformatted($"Charges: {skill.Value.MaxEnergyChargeNum}");
                            ImGui.TextUnformatted($"Charge CD: {skill.Value.EnergyChargeTime}");
                            //ImGui.TextUnformatted(JsonConvert.SerializeObject(skill.Value, Formatting.Indented));
                            ImGui.EndTooltip();
                        }

                        ImGui.SameLine();
                        ImGui.TextUnformatted($"[{skill.Value.Id}] {skill.Value.Name}");
                        skillIdx++;
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.NewLine();

            DrawWhoToTrack();

            ImGui.NewLine();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("What Events To Track:");
            if (ImGui.BeginListBox("##TrackedSkillEventsListBox", new Vector2(ImGui.GetContentRegionAvail().X, 140)))
            {
                foreach (var skillEventType in System.Enum.GetValues<ESkillEventTrackingType>())
                {
                    bool isSelected = ActiveTrackedEventEntry.SkillEvents.Contains(skillEventType);
                    var highlight = isSelected ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                    if (ImGui.Selectable(skillEventType.ToString(), isSelected, ImGuiSelectableFlags.SpanAllColumns | highlight))
                    {
                        if (isSelected == false)
                        {
                            ActiveTrackedEventEntry.SkillEvents.Add(skillEventType);
                        }
                        else
                        {
                            ActiveTrackedEventEntry.SkillEvents.Remove(skillEventType);
                        }
                    }
                    switch (skillEventType)
                    {
                        case ESkillEventTrackingType.SkillCast:
                            ImGui.SetItemTooltip("This occurs whenever a skill is cast.");
                            break;
                        case ESkillEventTrackingType.BossWarning:
                            ImGui.SetItemTooltip("This occurs when a Boss warns of a special attack coming soon (a cast bar appears to the left of their health bar).");
                            break;
                        case ESkillEventTrackingType.NoticeTip:
                            ImGui.SetItemTooltip("This occurs when a message appears on your screen informing about how to perform a fight mechanic.");
                            break;
                        default:
                            break;
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.NewLine();

            ImGui.TextUnformatted("Charge Behavior:");
            ImGui.TextDisabled("Applies to skills with multiple Charges.");
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Charge Cooldown Type:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##ChargeCooldownType", ActiveTrackedEventEntry.ChargeCooldownType.ToString()))
            {
                foreach (var chargeCooldownType in System.Enum.GetValues<EChargeCooldownType>())
                {
                    bool isSelected = ActiveTrackedEventEntry.ChargeCooldownType == chargeCooldownType;

                    if (ImGui.Selectable($"{chargeCooldownType}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        ActiveTrackedEventEntry.ChargeCooldownType = chargeCooldownType;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Charge Duration Display Type:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##ChargeDurationDisplayType", ActiveTrackedEventEntry.ChargeDurationDisplayType.ToString()))
            {
                foreach (var chargeDurationDisplayType in System.Enum.GetValues<EChargeDurationDisplayType>())
                {
                    bool isSelected = ActiveTrackedEventEntry.ChargeDurationDisplayType == chargeDurationDisplayType;

                    if (ImGui.Selectable($"{chargeDurationDisplayType}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        ActiveTrackedEventEntry.ChargeDurationDisplayType = chargeDurationDisplayType;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.NewLine();

            ImGui.Checkbox("Override Duration", ref ActiveTrackedEventEntry.OverrideDuration);
            ImGui.SetItemTooltip("Force the Duration to be a specific value instead of a detected one.");
            if (ActiveTrackedEventEntry.OverrideDuration)
            {
                ImGui.Indent();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Override Value:");
                ImGui.SameLine();
                ImGui.InputInt("##OverrideValueInput", ref ActiveTrackedEventEntry.DurationOverrideValue, ImGuiInputTextFlags.CharsDecimal);
                ImGui.SetItemTooltip("New Duration value to force. Value is in Seconds.");
                ImGui.Unindent();
            }
            ImGui.Checkbox("Use Boss Warning Cooldown Duration", ref ActiveTrackedEventEntry.UseBossDbmCdDuration);
            ImGui.SetItemTooltip("Uses the Cooldown Duration reported in the Boss Skill Warning table.");

            ImGui.Checkbox("Override Tracked Id", ref ActiveTrackedEventEntry.OverrideTrackedId);
            ImGui.SetItemTooltip("This is primarily useful for changing the ID used with Notice Tip Events.");
            if (ActiveTrackedEventEntry.OverrideTrackedId)
            {
                ImGui.Indent();
                ImGui.InputInt("##OverrideTrackedIdValue", ref ActiveTrackedEventEntry.OverrideTrackedIdValue, ImGuiInputTextFlags.CharsDecimal);
                ImGui.Unindent();
            }
        }

        private static void DrawAttributeTrackerOptions()
        {
            if (ImGui.BeginListBox("##AttributesListBox", new Vector2(ImGui.GetContentRegionAvail().X, 140)))
            {
                foreach (var attribute in System.Enum.GetValues<Zproto.EAttrType>())
                {
                    bool isSelected = ActiveTrackedEventEntry.TrackedAttributeName == attribute.ToString();

                    if (ImGui.Selectable($"{attribute}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        ActiveTrackedEventEntry.TrackedAttributeName = attribute.ToString();
                    }
                }
                ImGui.EndListBox();
            }

            // TODO: Allow selecting a Left Side and Right Side Attribute
            // These can then be used for creating a progress bar
            // If only a single one is selected (Left Side) it is used to just display that value
        }

        private static void DrawDisplayFormatOptions()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Display Format:");
            ImGui.SameLine();
            if (ImGui.Button("Apply Settings To All Trackers In Container"))
            {
                foreach (var tracker in ActiveTrackerContainer.EventTrackers)
                {
                    if (tracker.Value.IdTracker != ActiveTrackedEventEntry.IdTracker)
                    {
                        tracker.Value.ShowEntityName = ActiveTrackedEventEntry.ShowEntityName;
                        tracker.Value.ShowIcon = ActiveTrackedEventEntry.ShowIcon;
                        tracker.Value.IconSize = ActiveTrackedEventEntry.IconSize;
                        tracker.Value.ShowName = ActiveTrackedEventEntry.ShowName;
                        tracker.Value.NameSize = ActiveTrackedEventEntry.NameSize;
                        tracker.Value.ShowNameBeforeIcon = ActiveTrackedEventEntry.ShowNameBeforeIcon;
                        tracker.Value.ShowLayers = ActiveTrackedEventEntry.ShowLayers;
                        tracker.Value.LayerSize = ActiveTrackedEventEntry.LayerSize;
                        tracker.Value.ShowLayersBeforeIcon = ActiveTrackedEventEntry.ShowLayersBeforeIcon;
                        tracker.Value.ShowDurationText = ActiveTrackedEventEntry.ShowDurationText;
                        tracker.Value.DurationTextSize = ActiveTrackedEventEntry.DurationTextSize;
                        tracker.Value.DurationTextSameLine = ActiveTrackedEventEntry.DurationTextSameLine;
                        tracker.Value.UseMinutesForLongDuration = ActiveTrackedEventEntry.UseMinutesForLongDuration;
                        tracker.Value.UseCustomDurationTextColor = ActiveTrackedEventEntry.UseCustomDurationTextColor;
                        tracker.Value.DurationTextColor = ActiveTrackedEventEntry.DurationTextColor;
                        tracker.Value.ShowDurationProgessBar = ActiveTrackedEventEntry.ShowDurationProgessBar;
                        tracker.Value.DurationProgressBarStyle = ActiveTrackedEventEntry.DurationProgressBarStyle;
                        tracker.Value.DurationProgressBarCircleThickness = ActiveTrackedEventEntry.DurationProgressBarCircleThickness;
                        tracker.Value.UseDurationProgressBarCircleBackgroundFill = ActiveTrackedEventEntry.UseDurationProgressBarCircleBackgroundFill;
                        tracker.Value.DurationProgressBarCircleBackgroundColor = ActiveTrackedEventEntry.DurationProgressBarCircleBackgroundColor;
                        tracker.Value.DurationProgressBarSize = ActiveTrackedEventEntry.DurationProgressBarSize;
                        tracker.Value.DurationProgressBarTextSize = ActiveTrackedEventEntry.DurationProgressBarTextSize;
                        tracker.Value.DurationProgressBarSameLine = ActiveTrackedEventEntry.DurationProgressBarSameLine;
                        tracker.Value.DurationProgressBarVerticalOffset = ActiveTrackedEventEntry.DurationProgressBarVerticalOffset;
                        tracker.Value.ShowIconInsideProgressBar = ActiveTrackedEventEntry.ShowIconInsideProgressBar;
                        tracker.Value.ShowNameInsideProgressBar = ActiveTrackedEventEntry.ShowNameInsideProgressBar;
                        tracker.Value.ShowLayersInsideProgressBar = ActiveTrackedEventEntry.ShowLayersInsideProgressBar;
                        tracker.Value.ShowDurationTextInProgressBar = ActiveTrackedEventEntry.ShowDurationTextInProgressBar;
                        tracker.Value.TextInsideProgressBarOffset = ActiveTrackedEventEntry.TextInsideProgressBarOffset;
                        tracker.Value.ColorDurationProgressBarByType = ActiveTrackedEventEntry.ColorDurationProgressBarByType;
                        tracker.Value.ShowDurationEnded = ActiveTrackedEventEntry.ShowDurationEnded;
                        tracker.Value.HideTrackerCondition = ActiveTrackedEventEntry.HideTrackerCondition;

                        //tracker.Value.RaidWarningTrackerDatas = new();
                        //tracker.Value.RaidWarningTrackerDatas.AddRange(ActiveTrackedEventEntry.RaidWarningTrackerDatas);
                    }
                }
            }
            ImGui.SetItemTooltip("This will apply all current 'Display Format' settings to all other Trackers in this Container.\nNote: Does NOT include Custom Name, Custom Icon, or Raid Warning values.");

            ImGui.Checkbox("Show Entity Name", ref ActiveTrackedEventEntry.ShowEntityName);
            ImGui.Checkbox("Show Icon", ref ActiveTrackedEventEntry.ShowIcon);
            if (ActiveTrackedEventEntry.ShowIcon)
            {
                ImGui.Indent();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Icon Size:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##IconSize", ref ActiveTrackedEventEntry.IconSize, 16, 64);
                ImGui.PopStyleColor(2);
                ImGui.Unindent();

                ImGui.Indent();
                if (ImGui.Checkbox("Use Custom Icon", ref ActiveTrackedEventEntry.UseCustomIcon))
                {
                    ActiveTrackedEventEntry.UpdateIconData("", false);
                }
                if (ActiveTrackedEventEntry.UseCustomIcon)
                {
                    ImGui.Indent();
                    if (ImGui.InputTextWithHint("##CustomIconPath", ActiveTrackedEventEntry.OriginalIconPath, ref ActiveTrackedEventEntry.CustomIconPath, 512))
                    {
                        ActiveTrackedEventEntry.UpdateIconData("", false);
                    }
                    ImGui.SetItemTooltip("The file path starts in the ZDPS 'Data\\Images\\Buffs or Skills' directory.\n" +
                        "It is suggested to put new icons in a new custom sub-folder such as 'Data\\Images\\Custom'\n" +
                        "Files must be in the PNG format. Values entered here MUST NOT include their file extension.\n" +
                        "Example paths may look like: 'skill_fz_01' or '..\\Custom\\NewIcon'");
                    ImGui.Unindent();
                }
                ImGui.Unindent();
            }
            ImGui.Checkbox("Show Name", ref ActiveTrackedEventEntry.ShowName);
            if (ActiveTrackedEventEntry.ShowName)
            {
                ImGui.Indent();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Name Size:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##NameSize", ref ActiveTrackedEventEntry.NameSize, 16, 64);
                ImGui.PopStyleColor(2);

                ImGui.Checkbox("Use Custom Name", ref ActiveTrackedEventEntry.UseCustomName);
                if (ActiveTrackedEventEntry.UseCustomName)
                {
                    ImGui.Indent();
                    ImGui.InputTextWithHint("##CustomName", ActiveTrackedEventEntry.Name, ref ActiveTrackedEventEntry.CustomName, 512);
                    ImGui.Unindent();
                }

                ImGui.Checkbox("Show Name Before Icon##ShowNameBeforeIcon", ref ActiveTrackedEventEntry.ShowNameBeforeIcon);
                if (ActiveTrackedEventEntry.ShowNameBeforeIcon)
                {
                    ImGui.Indent();
                    ImGui.Checkbox("New Line Before Icon##NameNewLineBeforeIcon", ref ActiveTrackedEventEntry.NameNewLineBeforeIcon);
                    ImGui.Unindent();
                }

                ImGui.Unindent();
            }

            ImGui.Checkbox("Show Layers", ref ActiveTrackedEventEntry.ShowLayers);
            if (ActiveTrackedEventEntry.ShowLayers)
            {
                ImGui.Indent();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Layers Size:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##LayersSize", ref ActiveTrackedEventEntry.LayerSize, 16, 64);
                ImGui.PopStyleColor(2);

                ImGui.Checkbox("Show Layers Before Icon##ShowLayersBeforeIcon", ref ActiveTrackedEventEntry.ShowLayersBeforeIcon);
                if (ActiveTrackedEventEntry.ShowLayersBeforeIcon)
                {
                    ImGui.Indent();
                    ImGui.Checkbox("New Line Before Icon##LayersNewLineBeforeIcon", ref ActiveTrackedEventEntry.LayersNewLineBeforeIcon);
                    ImGui.Unindent();
                }

                ImGui.Unindent();
            }

            ImGui.Checkbox("Show Duration Text", ref ActiveTrackedEventEntry.ShowDurationText);
            if (ActiveTrackedEventEntry.ShowDurationText)
            {
                ImGui.Indent();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Duration Text Size:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##DurationTextSize", ref ActiveTrackedEventEntry.DurationTextSize, 16, 64);
                ImGui.PopStyleColor(2);

                ImGui.Checkbox("Same Line##DurationTextSameLine", ref ActiveTrackedEventEntry.DurationTextSameLine);
                ImGui.SetItemTooltip("Displays Duration Text on the same line as the previous displayed option for this Tracker.");

                ImGui.Checkbox("Use Minutes Format For Long Durations##UseMinutesForLongDuration", ref ActiveTrackedEventEntry.UseMinutesForLongDuration);
                ImGui.SetItemTooltip("Displays the Duration as minutes instead of seconds when more than 60 seconds remain.");

                ImGui.Checkbox("Use Custom Duration Text Color##UseCustomDurationTextColor", ref ActiveTrackedEventEntry.UseCustomDurationTextColor);
                ImGui.SetItemTooltip("Changes the color of Duration Text when NOT combined with other elements.");

                if (ActiveTrackedEventEntry.UseCustomDurationTextColor)
                {
                    ImGui.Indent();

                    ImGui.ColorEdit4("##DurationTextColorPicker", ref ActiveTrackedEventEntry.DurationTextColor);

                    ImGui.Unindent();
                }

                ImGui.Unindent();
            }

            ImGui.Checkbox("Show Duration Progress Bar", ref ActiveTrackedEventEntry.ShowDurationProgessBar);
            if (ActiveTrackedEventEntry.ShowDurationProgessBar)
            {
                ImGui.Indent();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Duration Progress Bar Style:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##DurationProgressBarStyle", ActiveTrackedEventEntry.DurationProgressBarStyle.ToString(), ImGuiComboFlags.None))
                {
                    int idx = 0;
                    foreach (var progressBarStyle in System.Enum.GetValues<EDurationProgressBarStyle>())
                    {
                        bool isSelected = ActiveTrackedEventEntry.DurationProgressBarStyle == progressBarStyle;

                        if (ImGui.Selectable($"{progressBarStyle.ToString()}", isSelected))
                        {
                            ActiveTrackedEventEntry.DurationProgressBarStyle = progressBarStyle;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }

                        idx++;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SetItemTooltip("Note: Circle Style does not support placing anything Inside it other than the Icon.");

                if (ActiveTrackedEventEntry.DurationProgressBarStyle == EDurationProgressBarStyle.Circle)
                {
                    ImGui.Indent();

                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Circle Progress Bar Thickness:");
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                    ImGui.SetNextItemWidth(-1);
                    ImGui.SliderInt("##DurationProgressBarCircleThickness", ref ActiveTrackedEventEntry.DurationProgressBarCircleThickness, 1, 24);
                    ImGui.PopStyleColor(2);

                    ImGui.Checkbox("Apply Overlay To Circle Fill##UseDurationProgressBarCircleBackgroundFill", ref ActiveTrackedEventEntry.UseDurationProgressBarCircleBackgroundFill);
                    ImGui.SetItemTooltip("Adds a dimmed overlay to the center of the circle, potentially making it easier to read text in it.");

                    if (ActiveTrackedEventEntry.UseDurationProgressBarCircleBackgroundFill)
                    {
                        ImGui.Indent();

                        ImGui.ColorEdit4("##DurationProgressBarCircleBackgroundColorPicker", ref ActiveTrackedEventEntry.DurationProgressBarCircleBackgroundColor);

                        ImGui.Unindent();
                    }

                    ImGui.Unindent();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Duration Progress Bar Size:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##DurationProgressBarSize", ref ActiveTrackedEventEntry.DurationProgressBarSize, 16, 64);
                ImGui.PopStyleColor(2);

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Duration Progress Bar Text Size:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##DurationProgressBarTextSize", ref ActiveTrackedEventEntry.DurationProgressBarTextSize, 16, 64);
                ImGui.PopStyleColor(2);

                ImGui.Checkbox("Same Line##DurationProgressBarSameLine", ref ActiveTrackedEventEntry.DurationProgressBarSameLine);
                ImGui.SetItemTooltip("Note: Requires [Layout Size Constraint = 'FixedSize'] to work correctly.\nDisplays Duration Progress Bar on the same line as the previous displayed option for this Tracker.");

                if (ActiveTrackedEventEntry.DurationProgressBarSameLine)
                {
                    ImGui.Indent();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Duration Progress Bar Vertical Offset:");
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                    ImGui.SetNextItemWidth(-1);
                    ImGui.SliderInt("##DurationProgressBarVerticalOffset", ref ActiveTrackedEventEntry.DurationProgressBarVerticalOffset, 0, 100);
                    ImGui.PopStyleColor(2);
                    ImGui.Unindent();
                }

                ImGui.Checkbox("Icon Inside Progress Bar##ShowIconInsideProgressBar", ref ActiveTrackedEventEntry.ShowIconInsideProgressBar);
                ImGui.SetItemTooltip("Note: Only applies if 'Show Icon' is also Enabled.\nRequires Duration Progress Bar Style = Circle.");

                ImGui.Checkbox("Name Inside Progress Bar##ShowNameInsideProgressBar", ref ActiveTrackedEventEntry.ShowNameInsideProgressBar);
                ImGui.SetItemTooltip("Note: Only applies if 'Show Name' is also Enabled.");

                ImGui.Checkbox("Layers Inside Progress Bar##ShowLayersInsideProgressBar", ref ActiveTrackedEventEntry.ShowLayersInsideProgressBar);
                ImGui.SetItemTooltip("Note: Only applies if 'Show Layers' is also Enabled. Will be automatically attached to end of Name.");

                ImGui.Checkbox("Duration Text Inside Progress Bar##ShowDurationTextInProgressBar", ref ActiveTrackedEventEntry.ShowDurationTextInProgressBar);

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Inside Text Offset:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                ImGui.SetNextItemWidth(-1);
                ImGui.SliderInt("##TextInsideProgressBarOffset", ref ActiveTrackedEventEntry.TextInsideProgressBarOffset, 0, 100, ImGuiSliderFlags.AlwaysClamp);
                ImGui.PopStyleColor(2);
                ImGui.SetItemTooltip("0 = Left, 50 = Center, 100 = Right");

                ImGui.Checkbox("Color Bar By Type", ref ActiveTrackedEventEntry.ColorDurationProgressBarByType);
                ImGui.SetItemTooltip("Changes the Duration Progress Bar color to be based on the entry type (Ex: Positive Buffs are Green, Debuffs are Red).");

                ImGui.Unindent();

            }
            // TODO: Give option to place Progress Bar on new line or to side of text/icon - if on side then must also be in fixed-width window layout

            ImGui.Checkbox("Show 'Duration Ended' When No Remaining Duration", ref ActiveTrackedEventEntry.ShowDurationEnded);
            ImGui.SetItemTooltip("Progress Bars are always hidden when there is no remaining duration. This only impacts Text.");

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Hide Tracker Condition:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##HideTrackerConditionCombo", ActiveTrackedEventEntry.HideTrackerCondition.ToString()))
            {
                foreach (var hideCondition in System.Enum.GetValues<EHideTrackerCondition>())
                {
                    bool isSelected = ActiveTrackedEventEntry.HideTrackerCondition == hideCondition;

                    if (ImGui.Selectable(hideCondition.ToString(), isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        ActiveTrackedEventEntry.HideTrackerCondition = hideCondition;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            if (ActiveTrackedEventEntry.HideTrackerCondition == EHideTrackerCondition.HideOnSpecificEvent)
            {
                ImGui.Indent();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Specific Event:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##HideOnSpecificBuffEventValueCombo", ActiveTrackedEventEntry.HideOnSpecificBuffEventValue.ToString()))
                {
                    foreach (var hideCondition in ActiveTrackedEventEntry.BuffEvents)
                    {
                        bool isSelected = ActiveTrackedEventEntry.HideOnSpecificBuffEventValue == hideCondition;

                        if (ImGui.Selectable(hideCondition.ToString(), isSelected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            ActiveTrackedEventEntry.HideOnSpecificBuffEventValue = hideCondition;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.Unindent();
            }

            ImGui.SeparatorText("Raid Warnings");
            ImGui.TextUnformatted("Raid Warning Messages can be displayed during certain events.");

            ImGui.Indent();
            int raidWarningIdx = 0;
            int raidWarningToRemove = -1;
            foreach (var raidWarningData in ActiveTrackedEventEntry.RaidWarningTrackerDatas)
            {
                ImGui.TextUnformatted("Is Enabled: ");
                ImGui.SameLine();
                ImGui.Checkbox($"##IsEnabled_{raidWarningIdx}", ref raidWarningData.IsEnabled);

                ImGui.AlignTextToFramePadding();
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed_Transparent);
                if (ImGui.Button($"X##RemoveRaidWarningDataBtn_{raidWarningIdx}"))
                {
                    raidWarningToRemove = raidWarningIdx;
                }
                ImGui.PopStyleColor();
                ImGui.SetItemTooltip("Delete Raid Warning Event");
                ImGui.SameLine();
                ImGui.SeparatorText($"Raid Warning Activation Type: {raidWarningData.ActivationType}");

                ImGui.BeginDisabled(!raidWarningData.IsEnabled);

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Perform Value Condition Check: ");
                ImGui.SameLine();
                ImGui.Checkbox($"##ValueConditionCheck_{raidWarningIdx}", ref raidWarningData.UseConditionValueCheck);
                ImGui.SetItemTooltip("If an additional check should be performed to determine if this Raid Warning should be executed.\nOnly works if the Activation Type supports this check.");

                ImGui.BeginDisabled(!raidWarningData.UseConditionValueCheck);
                ImGui.SameLine();
                if (ImGui.BeginCombo($"##ConditionCombo_{raidWarningIdx}", raidWarningData.CheckConditionType.ToString(), ImGuiComboFlags.WidthFitPreview))
                {
                    foreach (var checkType in System.Enum.GetValues<EConditionCheckType>())
                    {
                        bool isSelected = raidWarningData.CheckConditionType == checkType;

                        if (ImGui.Selectable($"{checkType}", isSelected))
                        {
                            raidWarningData.CheckConditionType = checkType;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                string checkValueString = raidWarningData.CheckConditionValue.ToString();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText($"##ConditionCheckValue_{raidWarningIdx}", ref checkValueString, 16, ImGuiInputTextFlags.CharsDecimal))
                {
                    if (float.TryParse(checkValueString, out var parsed))
                    {
                        raidWarningData.CheckConditionValue = parsed;
                    }
                    else
                    {
                        raidWarningData.CheckConditionValue = 0;
                    }
                }
                ImGui.EndDisabled();

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Message Text: ");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText($"##RaidWarningMessageText_{raidWarningIdx}", ref raidWarningData.MessageFormat, 512);
                if (ImGui.BeginItemTooltip())
                {
                    ImGui.TextUnformatted("Messages support a number of replacement operations:");
                    ImGui.TextUnformatted("Owner (Prefix): When NOT a Buff, this is who is actively casting. If Buff, is the entity gaining the Buff.");
                    ImGui.TextUnformatted("Caster (Prefix): When dealing with Buffs, this is who applied the Buff.");
                    ImGui.Indent();
                    ImGui.TextUnformatted("{<Prefix>EntityName}: Name of entity.");
                    ImGui.TextUnformatted("{<Prefix>EntityHp}: Current HP.");
                    ImGui.TextUnformatted("{<Prefix>EntityMaxHp}: Max HP.");
                    ImGui.TextUnformatted("{<Prefix>EntityHpPct}: HP Percentage (without trailing %).");
                    ImGui.Unindent();
                    ImGui.TextUnformatted("{TrackerName}: Name of this Tracker.");
                    ImGui.TextUnformatted("{Name}: Name of the Skill/Buff.");
                    ImGui.TextUnformatted("{Duration}: Value of the Duration/Cooldown.");
                    ImGui.TextUnformatted("{Layers}: Number of current Layers.");
                    ImGui.TextUnformatted("{Charges}: Number of current Charges.");
                    ImGui.TextUnformatted("{MaxCharges}: Max number of changes for the Skill.");

                    ImGui.EndTooltip();
                }
                ImGui.Checkbox($"Play Raid Warning Sound##PlayRaidSound_{raidWarningIdx}", ref raidWarningData.PlaySound);
                ImGui.BeginDisabled(!raidWarningData.PlaySound);
                ImGui.Indent();
                ImGui.InputTextWithHint($"##CustomSoundPath_{raidWarningIdx}", "Default Raid Warning Sound", ref raidWarningData.CustomSoundPath, 512);
                ImGui.SetItemTooltip("File path to a sound to play. Must be in MP3 or WAV format.\n" +
                    "The file path starts in the ZDPS 'Data\\Audio' directory. It is suggested to put custom sounds in a new Custom sub-folder.\n" +
                    "Example paths may look like: 'Custom\\NewAlert.wav' or '..\\CustomAudio\\Sounds\\NewAlert2.mp3'");
                ImGui.Unindent();
                ImGui.EndDisabled();
                ImGui.Separator();

                ImGui.EndDisabled();

                raidWarningIdx++;
            }
            ImGui.Unindent();

            if (raidWarningToRemove > -1)
            {
                ActiveTrackedEventEntry.RaidWarningTrackerDatas.RemoveAt(raidWarningToRemove);
            }

            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkGreen_Transparent);
            if (ImGui.Button("Add New Event##NewRaidWarningEventBtn"))
            {
                ImGui.SetNextWindowPos(ImGui.GetItemRectMin(), ImGuiCond.Appearing, new Vector2(0, 1));
                ImGui.OpenPopup("##NewRaidWarningEventListPopup");
            }
            ImGui.PopStyleColor();
            if (ImGui.BeginPopup("##NewRaidWarningEventListPopup"))
            {
                int addedCount = 0;
                foreach (var activationType in System.Enum.GetValues<ERaidWarningActivationType>())
                {
                    bool isAdded = ActiveTrackedEventEntry.RaidWarningTrackerDatas.Any(x => x.ActivationType == activationType);

                    if (!isAdded)
                    {
                        if (ImGui.Selectable($"{activationType}", true))
                        {
                            ActiveTrackedEventEntry.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData() { ActivationType = activationType });
                            ImGui.CloseCurrentPopup();
                        }
                        addedCount++;
                    }
                }

                if (addedCount == 0)
                {
                    ImGui.TextUnformatted("[No Events To Add]");
                }
                ImGui.EndPopup();
            }
        }

        private static void DrawWhoToTrack()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Who To Track:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##TrackedEntityType", ActiveTrackedEventEntry.TrackedEntityType.ToString(), ImGuiComboFlags.None))
            {
                int idx = 0;
                foreach (var trackedEntityType in System.Enum.GetValues<ETrackedEntityType>())
                {
                    bool isSelected = ActiveTrackedEventEntry.TrackedEntityType == trackedEntityType;

                    if (ImGui.Selectable($"{trackedEntityType.ToString()}", isSelected))
                    {
                        ActiveTrackedEventEntry.TrackedEntityType = trackedEntityType;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }

                    idx++;
                }
                ImGui.EndCombo();
            }
            if (ActiveTrackedEventEntry.TrackedEntityType == ETrackedEntityType.DefinedTarget)
            {
                // TODO: Show input box for entering Target data
                ImGui.TextUnformatted("Target UID or Name [Requires UUID Currently - In Debug Tab of Entity Inspector]:");
                int definedUuid = (int)ActiveTrackedEventEntry.DefinedEntityTargetUuid;
                ImGui.InputInt("##TargetUUIDInput", ref definedUuid, ImGuiInputTextFlags.CharsDecimal);
                ActiveTrackedEventEntry.DefinedEntityTargetUuid = definedUuid;
            }
            if (ActiveTrackedEventEntry.TrackedEntityType == ETrackedEntityType.Everyone)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Exclude 'Self' From 'Everyone' Filter:");
                ImGui.SameLine();
                ImGui.Checkbox("##ExcludeSelfFromEveryoneType", ref ActiveTrackedEventEntry.ExcludeSelfFromEveryoneType);
            }
            if (ActiveTrackedEventEntry.TrackedEntityType == ETrackedEntityType.Summons)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Only Track Summons From 'Self':");
                ImGui.SameLine();
                ImGui.Checkbox("##OnlyTrackSummonsFromSelf", ref ActiveTrackedEventEntry.OnlyTrackSummonsFromSelf);
            }
        }

        private static void DrawLoadTimeOptions()
        {
            ImGui.TextUnformatted("Configure when the Tracker is allowed to run.");
            ImGui.TextUnformatted("Note: The Tracker itself must also be Enabled.\nIf nothing below is Enabled, the Tracker will always be running.");

            bool inCombat = false;
            DataTypes.Enums.Professions.ERoleType roleType = DataTypes.Enums.Professions.ERoleType.None;
            DataTypes.Enums.Professions.EProfessionId profession = DataTypes.Enums.Professions.EProfessionId.Profession_Unknown;
            DataTypes.Enums.Professions.SubProfessionId subProfession = DataTypes.Enums.Professions.SubProfessionId.SubProfession_Unknown;
            if (EncounterManager.Current.Entities.TryGetValue(AppState.PlayerUUID, out var playerEntity))
            {
                var attrCombatState = playerEntity.GetAttrKV("AttrCombatState") as int?;
                inCombat = (attrCombatState != null && attrCombatState > 0);
                roleType = DataTypes.Professions.GetRoleFromBaseProfessionId(playerEntity.ProfessionId);
                profession = (DataTypes.Enums.Professions.EProfessionId)playerEntity.ProfessionId;
                subProfession = (DataTypes.Enums.Professions.SubProfessionId)playerEntity.SubProfessionId;
            }

            void HandleApplyToOthersContextMenu(Action<TrackedEventEntry> func)
            {
                if (ImGui.BeginPopupContextItem(ImGuiPopupFlags.MouseButtonRight))
                {
                    if (ImGui.MenuItem("Apply To Other Trackers"))
                    {
                        foreach (var tracker in ActiveTrackerContainer.EventTrackers)
                        {
                            if (tracker.Value.IdTracker != ActiveTrackedEventEntry.IdTracker)
                            {
                                func(tracker.Value);
                            }
                        }
                    }
                    ImGui.EndPopup();
                }
            };

            ImGui.Checkbox("In Combat", ref ActiveTrackedEventEntry.LoadEvents.InCombat);
            ImGui.SetItemTooltip($"Tracker is only Enabled during Active Combat.\nCurrent In Combat: {inCombat}");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.InCombat = ActiveTrackedEventEntry.LoadEvents.InCombat; });

            ImGui.Checkbox("Not In Combat", ref ActiveTrackedEventEntry.LoadEvents.NotInCombat);
            ImGui.SetItemTooltip($"Tracker is only Enabled when not in Active Combat.\nCurrent In Combat: {inCombat}");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.NotInCombat = ActiveTrackedEventEntry.LoadEvents.NotInCombat; });

            ImGui.Checkbox("In Encounter", ref ActiveTrackedEventEntry.LoadEvents.InEncounter);
            ImGui.SetItemTooltip($"Tracker is only Enabled while not in the Open World.\nNote: On ZDPS startup, you are always considered in the Open World until your first map change.\nCurrently In Encounter: {!BattleStateMachine.IsInOpenWorld()}");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.InEncounter = ActiveTrackedEventEntry.LoadEvents.InEncounter; });

            ImGui.Checkbox("Is Alive", ref ActiveTrackedEventEntry.LoadEvents.IsAlive);
            ImGui.SetItemTooltip("Tracker is only Enabled while you are alive.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsAlive = ActiveTrackedEventEntry.LoadEvents.IsAlive; });

            ImGui.Checkbox("Is Dead", ref ActiveTrackedEventEntry.LoadEvents.IsDead);
            ImGui.SetItemTooltip("Tracker is only Enabled while you are dead.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsDead = ActiveTrackedEventEntry.LoadEvents.IsDead; });

            ImGui.AlignTextToFramePadding();
            ImGui.Checkbox("Is Role", ref ActiveTrackedEventEntry.LoadEvents.UseRoleTypes);
            ImGui.SetItemTooltip($"Tracker is only Enabled while you are the selected Role(s).\nCurrent Role: {roleType}");
            HandleApplyToOthersContextMenu((tracker) =>
            {
                tracker.LoadEvents.UseRoleTypes = ActiveTrackedEventEntry.LoadEvents.UseRoleTypes;
                tracker.LoadEvents.RoleType.Clear();
                tracker.LoadEvents.RoleType.AddRange(ActiveTrackedEventEntry.LoadEvents.RoleType);
            });
            ImGui.SameLine();
            ImGui.BeginDisabled(!ActiveTrackedEventEntry.LoadEvents.UseRoleTypes);
            string roleTypePreview = String.Join(", ", ActiveTrackedEventEntry.LoadEvents.RoleType);
            if (ImGui.BeginCombo("##IsRoleTypeCombo", roleTypePreview, ImGuiComboFlags.None))
            {
                foreach (var item in System.Enum.GetValues<DataTypes.Enums.Professions.ERoleType>())
                {
                    bool isSelected = ActiveTrackedEventEntry.LoadEvents.RoleType.Contains(item);
                    ImGui.BeginDisabled();
                    ImGui.Checkbox($"##{item}", ref isSelected);
                    ImGui.EndDisabled();
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{item}", isSelected))
                    {
                        if (isSelected)
                        {
                            ActiveTrackedEventEntry.LoadEvents.RoleType.Remove(item);
                        }
                        else
                        {
                            ActiveTrackedEventEntry.LoadEvents.RoleType.Add(item);
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.EndDisabled();

            ImGui.Checkbox("Is Profession", ref ActiveTrackedEventEntry.LoadEvents.UseProfession);
            ImGui.SetItemTooltip($"Tracker is only Enabled while you are the selected Profession(s).\nCurrent Profession: {profession}");
            HandleApplyToOthersContextMenu((tracker) =>
            {
                tracker.LoadEvents.UseProfession = ActiveTrackedEventEntry.LoadEvents.UseProfession;
                tracker.LoadEvents.Profession.Clear();
                tracker.LoadEvents.Profession.AddRange(ActiveTrackedEventEntry.LoadEvents.Profession);
            });
            ImGui.SameLine();
            ImGui.BeginDisabled(!ActiveTrackedEventEntry.LoadEvents.UseProfession);
            string professionPreview = String.Join(", ", ActiveTrackedEventEntry.LoadEvents.Profession);
            if (ImGui.BeginCombo("##IsProfessionCombo", professionPreview, ImGuiComboFlags.None))
            {
                foreach (var item in System.Enum.GetValues<DataTypes.Enums.Professions.EProfessionId>())
                {
                    bool isSelected = ActiveTrackedEventEntry.LoadEvents.Profession.Contains(item);
                    ImGui.BeginDisabled();
                    ImGui.Checkbox($"##{item}", ref isSelected);
                    ImGui.EndDisabled();
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{item}", isSelected))
                    {
                        if (isSelected)
                        {
                            ActiveTrackedEventEntry.LoadEvents.Profession.Remove(item);
                        }
                        else
                        {
                            ActiveTrackedEventEntry.LoadEvents.Profession.Add(item);
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.EndDisabled();

            ImGui.Checkbox("Is SubProfession", ref ActiveTrackedEventEntry.LoadEvents.UseSubProfession);
            ImGui.SetItemTooltip($"Tracker is only Enabled while you are the selected SubProfession(s).\nCurrent SubProfession: {subProfession}");
            HandleApplyToOthersContextMenu((tracker) =>
            {
                tracker.LoadEvents.UseSubProfession = ActiveTrackedEventEntry.LoadEvents.UseSubProfession;
                tracker.LoadEvents.SubProfession.Clear();
                tracker.LoadEvents.SubProfession.AddRange(ActiveTrackedEventEntry.LoadEvents.SubProfession);
            });
            ImGui.SameLine();
            ImGui.BeginDisabled(!ActiveTrackedEventEntry.LoadEvents.UseSubProfession);
            string subProfessionPreview = String.Join(", ", ActiveTrackedEventEntry.LoadEvents.SubProfession);
            if (ImGui.BeginCombo("##IsSubProfessionCombo", subProfessionPreview, ImGuiComboFlags.None))
            {
                foreach (var item in System.Enum.GetValues<DataTypes.Enums.Professions.SubProfessionId>())
                {
                    bool isSelected = ActiveTrackedEventEntry.LoadEvents.SubProfession.Contains(item);
                    ImGui.BeginDisabled();
                    ImGui.Checkbox($"##{item}", ref isSelected);
                    ImGui.EndDisabled();
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{item}", isSelected))
                    {
                        if (isSelected)
                        {
                            ActiveTrackedEventEntry.LoadEvents.SubProfession.Remove(item);
                        }
                        else
                        {
                            ActiveTrackedEventEntry.LoadEvents.SubProfession.Add(item);
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.EndDisabled();

            ImGui.AlignTextToFramePadding();
            ImGui.Checkbox("In SceneId:", ref ActiveTrackedEventEntry.LoadEvents.UseSceneIds);
            ImGui.SetItemTooltip($"Tracker is only Enabled while you are in the selected SceneId(s).\nFormat: Comma delimited list of Scene Id numbers.\nCurrent SceneId: {EncounterManager.Current?.SceneId}");
            HandleApplyToOthersContextMenu((tracker) =>
            {
                tracker.LoadEvents.UseSceneIds = ActiveTrackedEventEntry.LoadEvents.UseSceneIds;
                tracker.LoadEvents.SceneIds = ActiveTrackedEventEntry.LoadEvents.SceneIds;
                tracker.LoadEvents.UpdateSceneIdValuesFromString();
            });
            ImGui.BeginDisabled(!ActiveTrackedEventEntry.LoadEvents.UseSceneIds);
            ImGui.SameLine();
            if (ImGui.Button("Select..."))
            {
                ImGui.SetNextWindowPos(ImGui.GetItemRectMax(), ImGuiCond.Appearing, new Vector2(1, 1));
                ImGui.OpenPopup("##SceneIdListPopup");
            }
            if (ImGui.BeginPopup("##SceneIdListPopup"))
            {
                if (ImGui.BeginCombo("##SceneIdListCombo", "Select A SceneId To Add"))
                {
                    foreach (var sceneIds in HelperMethods.DataTables.Scenes.Data)
                    {
                        bool isAdded = ActiveTrackedEventEntry.LoadEvents.SceneIdValues.Contains(int.Parse(sceneIds.Key));
                        ImGuiSelectableFlags highlight = isAdded ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None;
                        if (ImGui.Selectable($"[{sceneIds.Key}] {sceneIds.Value.Name}", isAdded, ImGuiSelectableFlags.NoAutoClosePopups | highlight))
                        {
                            if (isAdded)
                            {
                                ActiveTrackedEventEntry.LoadEvents.SceneIdValues.Remove(int.Parse(sceneIds.Key));
                                ActiveTrackedEventEntry.LoadEvents.UpdateSceneIdFromList();
                            }
                            else
                            {
                                ActiveTrackedEventEntry.LoadEvents.SceneIdValues.Add(int.Parse(sceneIds.Key));
                                ActiveTrackedEventEntry.LoadEvents.UpdateSceneIdFromList();
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            if (ImGui.InputText("##SceneIdValues", ref ActiveTrackedEventEntry.LoadEvents.SceneIds, 512))
            {
                ActiveTrackedEventEntry.LoadEvents.UpdateSceneIdValuesFromString();
            }

            ImGui.EndDisabled();

            ImGui.SeparatorText("Data Owner Related Options");
            ImGui.Indent();
            ImGui.BeginDisabled();
            ImGui.TextWrapped("Note: These are checked after the Container is created.\nYou may see the Container appear even if the state of these would otherwise hide it.");
            ImGui.EndDisabled();
            ImGui.Unindent();

            ImGui.Checkbox("Is Owner Alive", ref ActiveTrackedEventEntry.LoadEvents.IsOwnerAlive);
            ImGui.SetItemTooltip("Tracker is only Enabled while the Owner is alive.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsOwnerAlive = ActiveTrackedEventEntry.LoadEvents.IsOwnerAlive; });

            ImGui.Checkbox("Is Owner Dead", ref ActiveTrackedEventEntry.LoadEvents.IsOwnerDead);
            ImGui.SetItemTooltip("Tracker is only Enabled while the Owner is dead.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsOwnerDead = ActiveTrackedEventEntry.LoadEvents.IsOwnerDead; });

            ImGui.SeparatorText("Extra Options");
            ImGui.Checkbox("Keep On Scene Change", ref ActiveTrackedEventEntry.LoadEvents.KeepOnSceneChange);
            ImGui.SetItemTooltip("The Tracker will persist through Scene (Map) changes.\nNote: Encounter events like wipes will still remove it.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsOwnerDead = ActiveTrackedEventEntry.LoadEvents.IsOwnerDead; });

            ImGui.Checkbox("Keep On Wipe", ref ActiveTrackedEventEntry.LoadEvents.KeepOnWipe);
            ImGui.SetItemTooltip("The Tracker will persist through wipes.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsOwnerDead = ActiveTrackedEventEntry.LoadEvents.IsOwnerDead; });

            ImGui.Checkbox("Keep On Restart", ref ActiveTrackedEventEntry.LoadEvents.KeepOnRestart);
            ImGui.SetItemTooltip("The Tracker will persist through Restart events. These are typically when a Raid Boss is killed.");
            HandleApplyToOthersContextMenu((tracker) => { tracker.LoadEvents.IsOwnerDead = ActiveTrackedEventEntry.LoadEvents.IsOwnerDead; });
        }

        public static void ToggleForceHideAllContainers(bool newState)
        {
            ForceHideAllContainers = newState;

            InvalidateContainers();
        }

        public static void InvalidateContainers()
        {
            foreach (var container in EventTrackerContainers)
            {
                container.Value.HadTransparentBackground = false;
                container.Value.LastSetOpacity = 100;
            }
        }

        private static void LoadDefaultPresets(bool forceAdd = false)
        {
            var backupPresetTrackersList = PresetTrackersList;
            var backupPresetContainersList = PresetContainersList;

            if (forceAdd)
            {
                PresetTrackersList = new();
                PresetContainersList = new();
            }

            if (PresetTrackersList.Count == 0)
            {
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Tina Lower CD Tracker", 2110034));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Tina Time Stasis Tracker", 2110056));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Weakened Tracker", 2110057));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Basilisk All-Element Bonus", 2110125));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Basilisk Element Stasis", 2110050));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Tatta Heart of Flames", 2110061));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Tatta Exhausted Flame Devour", 2110055));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Wound (Heal Blocked)", 510571));
                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Boarrier Wound (Heal Blocked)", 2110026));

                PresetTrackersList.Add(CreateNewBasicBuffEventEntry("Life Wave", 2302421));

                var dmgStackBuff = CreateNewBasicBuffEventEntry("DMG Stack", 2300621);
                dmgStackBuff.TrackedEntityType = ETrackedEntityType.UserTarget;
                PresetTrackersList.Add(dmgStackBuff);

                // Wraith Shrine Boss
                var newThunderTribulation = CreateNewBasicSkillEventEntry("Boss: Thunder Tribulation (Stack)", 4050002);
                newThunderTribulation.TrackedEntityType = ETrackedEntityType.Everyone;
                newThunderTribulation.ExcludeSelfFromEveryoneType = true;
                newThunderTribulation.OverrideDuration = true;
                newThunderTribulation.DurationOverrideValue = 4;
                PresetTrackersList.Add(newThunderTribulation);
                var newShadowbreakStrike = CreateNewBasicSkillEventEntry("Boss: Shadowbreak Strike (Tankbuster)", 4050005);
                newShadowbreakStrike.TrackedEntityType = ETrackedEntityType.Everyone;
                newShadowbreakStrike.ExcludeSelfFromEveryoneType = true;
                newShadowbreakStrike.OverrideDuration = true;
                newShadowbreakStrike.DurationOverrideValue = 3;
                newShadowbreakStrike.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "Tankbuster on {OwnerEntityTarget}!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newShadowbreakStrike);
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Dagger of Execution (Warning)", 4050006));
                var newDOEIncoming = CreateNewBasicSkillEventEntry("Boss: Dagger of Execution (Notice)", 4050006);
                newDOEIncoming.SkillEvents = [ESkillEventTrackingType.NoticeTip];
                newDOEIncoming.TrackedEntityType = ETrackedEntityType.Everyone;
                newDOEIncoming.ExcludeSelfFromEveryoneType = true;
                newDOEIncoming.OverrideTrackedId = true;
                newDOEIncoming.OverrideTrackedIdValue = 4050010;
                PresetTrackersList.Add(newDOEIncoming);
                var daggerOfExecutionDebuff = CreateNewBasicBuffEventEntry("Boss: Dagger of Execution (Debuff)", 882811);
                daggerOfExecutionDebuff.TrackedEntityType = ETrackedEntityType.Self;
                PresetTrackersList.Add(daggerOfExecutionDebuff);
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Undead Revival", 4050010));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Shackles of Souls", 4050011));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Infected Laser", 4050012));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Gate of Wraith", 4050013));

                // Soundless City Boss
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Malice Combo", 3330110));
                var newLockOn = CreateNewBossWarningBuffEventEntry("Boss: Lock-On", 882403);
                newLockOn.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has {Name}! Get to a Stasis Bubble!",
                    PlaySound = false,
                });
                PresetTrackersList.Add(newLockOn);
                var newDeployPulseFountain = CreateNewBossWarningBuffEventEntry("Boss: Deploy Pulse Fountain", 882576);
                newDeployPulseFountain.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has {Name}",
                    PlaySound = false,
                });
                PresetTrackersList.Add(newDeployPulseFountain);

                // Dreambloom Ruins - Caprahorn - Bloom and Steel (Raid)
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Judgment Hour", 10281202));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Territory Banish", 10281205));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Territory Resonance", 10281206));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Vow of Valor", 10281204));
                PresetTrackersList.Add(CreateNewBossWarningBuffEventEntry("Territory Resonance - Lunalusion", 827110));
                PresetTrackersList.Add(CreateNewBossWarningBuffEventEntry("Territory Resonance - Solarshine", 827111));
                var newTerritoryExecutionLuna = CreateNewBossWarningBuffEventEntry("Territory Execution - Lunalusion", 827147);
                newTerritoryExecutionLuna.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has Tankbuster!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newTerritoryExecutionLuna);
                var newTerritoryExecutionSolar = CreateNewBossWarningBuffEventEntry("Territory Execution - Solarshine", 827144);
                newTerritoryExecutionSolar.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has Tankbuster!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newTerritoryExecutionSolar);
                var newTerritorySplitDamageLuna = CreateNewBossWarningBuffEventEntry("Territory Split Damage - Lunalusion", 827148);
                newTerritorySplitDamageLuna.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has Party Stack!",
                    PlaySound = false,
                });
                PresetTrackersList.Add(newTerritorySplitDamageLuna);
                var newTerritorySplitDamageSolar = CreateNewBossWarningBuffEventEntry("Territory Split Damage - Solarshine", 827145);
                newTerritorySplitDamageSolar.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has Party Stack!",
                    PlaySound = false,
                });
                PresetTrackersList.Add(newTerritorySplitDamageSolar);
                var newSunfireLink = CreateNewBossWarningBuffEventEntry("Sunfire Link", 827130);
                newSunfireLink.TrackedEntityType = ETrackedEntityType.Self;
                newSunfireLink.ExcludeSelfFromEveryoneType = false;
                newSunfireLink.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has {Name}!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newSunfireLink);
                var newMoonstrikeLink = CreateNewBossWarningBuffEventEntry("Moonstrike Link", 827131);
                newMoonstrikeLink.TrackedEntityType = ETrackedEntityType.Self;
                newMoonstrikeLink.ExcludeSelfFromEveryoneType = false;
                newMoonstrikeLink.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has {Name}!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newMoonstrikeLink);

                // Dreambloom Ruins - Withered Bloomshard (Raid)
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Earthshatter Fist (Line Stack)", 10290111));
                var newUppercutTankbuster = CreateNewBasicSkillEventEntry("Boss: Uppercut (Tankbuster)", 10290105);
                newUppercutTankbuster.TrackedEntityType = ETrackedEntityType.Everyone;
                newUppercutTankbuster.ExcludeSelfFromEveryoneType = true;
                PresetTrackersList.Add(newUppercutTankbuster);
                var newUppercutDebuff = CreateNewBossWarningBuffEventEntry("Boss: Uppercut (Tankbuster) (Debuff)", 827200);
                newUppercutDebuff.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} targets {OwnerEntityName}!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newUppercutDebuff);
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Earthshatter Fist - Combo", 10290124));
                var newElementalOverflow = CreateNewBossWarningBuffEventEntry("Boss: Elemental Overflow", 827263);
                newElementalOverflow.TrackedEntityType = ETrackedEntityType.Self;
                newElementalOverflow.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} targets {OwnerEntityName}!",
                    PlaySound = true,
                });
                newElementalOverflow.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnRemove,
                    MessageFormat = "Stack With Device Holder (Place Crag Ring)!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newElementalOverflow);

                var newRockfallSpread = CreateNewBossWarningBuffEventEntry("Boss: Rockfall Spread", 827219);
                newRockfallSpread.TrackedEntityType = ETrackedEntityType.Self;
                newRockfallSpread.HideTrackerCondition = EHideTrackerCondition.HideWhenNoDuration;
                newRockfallSpread.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} on {OwnerEntityName}!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newRockfallSpread);

                var newRockfallTeamStack = CreateNewBossWarningBuffEventEntry("Boss: Rockfall Team Stack", 827246);
                newRockfallTeamStack.HideTrackerCondition = EHideTrackerCondition.HideWhenNoDuration;
                newRockfallTeamStack.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} on {OwnerEntityName}!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newRockfallTeamStack);

                // Dreambloom Ruins - Erosion Bloom Afterimage (Raid)
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Cinderfall Trail", 10300105));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Inferno Ring", 10300106));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Arc Slash", 10300204));
                PresetTrackersList.Add(CreateNewBossSkillEventEntry("Boss: Lotus Dance", 10300206));

                // Executing Sword Sweep
                var newArcSlashSweep = CreateNewBasicSkillEventEntry("Boss: Arc Slash (Sweep)", 10300204);
                newArcSlashSweep.SkillEvents = [ESkillEventTrackingType.NoticeTip];
                newArcSlashSweep.TrackedEntityType = ETrackedEntityType.Everyone;
                newArcSlashSweep.ExcludeSelfFromEveryoneType = true;
                newArcSlashSweep.OverrideTrackedId = true;
                newArcSlashSweep.OverrideTrackedIdValue = 8000508;
                newArcSlashSweep.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} - Tanks Dodge Sword Now!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newArcSlashSweep);

                var newAbyssalSealMarker = CreateNewBossWarningBuffEventEntry("Boss: Abyssal Seal Marker (Debuff)", 828107);
                newAbyssalSealMarker.UseCustomName = true;
                newAbyssalSealMarker.CustomName = "Abyssal Seal";
                newAbyssalSealMarker.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{OwnerEntityName} has {Name} - Place Swordshade Correctly!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newAbyssalSealMarker);

                var newInfernoRing = CreateNewBossSkillEventEntry("Boss: Inferno Ring", 10300106);
                newInfernoRing.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} - Run Away From Impact!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newInfernoRing);

                var newBewilderment = CreateNewBossSkillEventEntry("Boss: Bewilderment", 10300107);
                newBewilderment.RaidWarningTrackerDatas.Add(new RaidWarningTrackerData()
                {
                    IsEnabled = true,
                    ActivationType = ERaidWarningActivationType.OnGain,
                    MessageFormat = "{Name} - Look Away Soon!",
                    PlaySound = true,
                });
                PresetTrackersList.Add(newBewilderment);

                var newErosionBloomSickness = CreateNewBasicBuffEventEntry("Boss: Erosion Bloom Sickness", 828153);
                newErosionBloomSickness.TrackedEntityType = ETrackedEntityType.Everyone;
                newErosionBloomSickness.ShowLayers = true;
                PresetTrackersList.Add(newErosionBloomSickness);
            }



            if (PresetContainersList.Count == 0)
            {
                // Containers
                TrackerContainer groupDebuffsContainer = new TrackerContainer(0)
                {
                    ContainerName = "Group Debuffs",
                    SourceLocationType = ESourceLocationType.Internal,
                    SourceLocationId = "Group Debuffs",
                    ShowContainerName = true,
                    IsContainerEnabled = true,
                    ContainerLayoutStyle = EContainerLayoutStyle.List,
                    ContainerListDirection = EContainerListDirection.Down,
                    ContainerSizeConstraint = EContainerSizeConstraint.AutoSize
                };
                var newWeakenedTracker = CreateNewBasicBuffEventEntry("Weakened Tracker", 2110057);
                newWeakenedTracker.TrackedEntityType = ETrackedEntityType.Everyone;
                newWeakenedTracker.ShowEntityName = true;
                newWeakenedTracker.DurationProgressBarSameLine = true;
                newWeakenedTracker.ShowNameInsideProgressBar = true;
                newWeakenedTracker.ShowDurationTextInProgressBar = true;
                groupDebuffsContainer.EventTrackers.Add(0, newWeakenedTracker);
                var newTimeStasisTracker = CreateNewBasicBuffEventEntry("Tina Time Stasis Tracker", 2110056);
                newTimeStasisTracker.TrackedEntityType = ETrackedEntityType.Everyone;
                newTimeStasisTracker.ShowEntityName = true;
                newTimeStasisTracker.DurationProgressBarSameLine = true;
                newTimeStasisTracker.ShowNameInsideProgressBar = true;
                newTimeStasisTracker.ShowDurationTextInProgressBar = true;
                groupDebuffsContainer.EventTrackers.Add(1, newTimeStasisTracker);
                var newWoundTracker = CreateNewBasicBuffEventEntry("Wound (Heal Blocked)", 510571);
                newWoundTracker.TrackedEntityType = ETrackedEntityType.Everyone;
                newWoundTracker.ShowEntityName = true;
                newWoundTracker.DurationProgressBarSameLine = true;
                newWoundTracker.ShowNameInsideProgressBar = true;
                newWoundTracker.ShowDurationTextInProgressBar = true;
                groupDebuffsContainer.EventTrackers.Add(2, newWoundTracker);
                var newExhaustedFlameDevour = CreateNewBasicBuffEventEntry("Tatta Exhausted Flame Devour", 2110055);
                newExhaustedFlameDevour.TrackedEntityType = ETrackedEntityType.Everyone;
                newExhaustedFlameDevour.ShowEntityName = true;
                newExhaustedFlameDevour.DurationProgressBarSameLine = true;
                newExhaustedFlameDevour.ShowNameInsideProgressBar = true;
                newExhaustedFlameDevour.ShowDurationTextInProgressBar = true;
                groupDebuffsContainer.EventTrackers.Add(3, newExhaustedFlameDevour);

                PresetContainersList.Add(groupDebuffsContainer);
            }

            if (forceAdd)
            {
                // Iterate through each newly added Preset then scan the old list for a match
                // If a match is found, replace it with our new Preset version
                int insertIdx = 0;
                foreach (var tracker in PresetTrackersList)
                {
                    bool updated = false;
                    for (int i = 0; i < backupPresetTrackersList.Count; i++)
                    {
                        var newTracker = backupPresetTrackersList[i];
                        if (newTracker.SourceLocationType == tracker.SourceLocationType && !string.IsNullOrEmpty(newTracker.SourceLocationId) && newTracker.SourceLocationId == tracker.SourceLocationId)
                        {
                            newTracker = tracker;
                            updated = true;
                            break;
                        }
                    }
                    if (!updated)
                    {
                        backupPresetTrackersList.Insert(insertIdx, tracker);
                        insertIdx++;
                    }
                }

                PresetTrackersList = backupPresetTrackersList;

                insertIdx = 0;
                foreach (var container in PresetContainersList)
                {
                    bool updated = false;
                    for (int i = 0; i < backupPresetContainersList.Count; i++)
                    {
                        var newTracker = backupPresetContainersList[i];
                        if (newTracker.SourceLocationType == container.SourceLocationType && !string.IsNullOrEmpty(newTracker.SourceLocationId) && newTracker.SourceLocationId == container.SourceLocationId)
                        {
                            newTracker = container;
                            updated = true;
                            break;
                        }
                    }
                    if (!updated)
                    {
                        backupPresetContainersList.Insert(insertIdx, container);
                        insertIdx++;
                    }
                }

                PresetTrackersList = backupPresetTrackersList;
                PresetContainersList = backupPresetContainersList;

                //PresetTrackersList.AddRange(backupPresetTrackersList);
                //PresetContainersList.AddRange(backupPresetContainersList);
            }
        }

        private static TrackedEventEntry CreateNewBasicBuffEventEntry(string trackerName, int buffId)
        {
            var newEntry = new TrackedEventEntry(0);
            newEntry.SourceLocationType = ESourceLocationType.Internal;
            newEntry.SourceLocationId = trackerName;
            newEntry.TrackerName = trackerName;
            newEntry.TrackerType = ETrackerType.Buffs;

            newEntry.TrackedEntityType = ETrackedEntityType.Self;

            newEntry.BuffEvents = [
                Zproto.EBuffEventType.BuffEventAddTo,
                Zproto.EBuffEventType.BuffEventRemove,
                Zproto.EBuffEventType.BuffEventReplace,
                Zproto.EBuffEventType.BuffEventStackLayer,
                Zproto.EBuffEventType.BuffEventRemoveLayer
                ];

            newEntry.TrackedBuffId = buffId;

            if (HelperMethods.DataTables.Buffs.Data.TryGetValue(newEntry.TrackedBuffId.ToString(), out var buff))
            {
                newEntry.Name = buff.Name;
                newEntry.Desc = buff.Desc;

                newEntry.UpdateIconData(buff.GetIconName(), true);
            }

            newEntry.ShowIcon = true;
            newEntry.ShowName = true;
            newEntry.ShowDurationText = true;
            newEntry.ShowDurationProgessBar = true;
            newEntry.ColorDurationProgressBarByType = true;
            newEntry.HideTrackerCondition = EHideTrackerCondition.HideWhenNoDuration;
            newEntry.IsEnabled = true;

            return newEntry;
        }

        private static TrackedEventEntry CreateNewBossWarningBuffEventEntry(string trackerName, int buffId)
        {
            var newEntry = CreateNewBasicBuffEventEntry(trackerName, buffId);

            newEntry.TrackedEntityType = ETrackedEntityType.Everyone;
            newEntry.ExcludeSelfFromEveryoneType = false;

            newEntry.DurationProgressBarSameLine = true;
            newEntry.ShowNameInsideProgressBar = true;
            newEntry.ShowDurationTextInProgressBar = true;
            newEntry.ColorDurationProgressBarByType = true;

            newEntry.HideTrackerCondition = EHideTrackerCondition.HideOnRemoveEvent;

            return newEntry;
        }

        private static TrackedEventEntry CreateNewBasicSkillEventEntry(string trackerName, int skillId)
        {
            var newEntry = new TrackedEventEntry(0);
            newEntry.SourceLocationType = ESourceLocationType.Internal;
            newEntry.SourceLocationId = trackerName;
            newEntry.TrackerName = trackerName;
            newEntry.TrackerType = ETrackerType.Skills;

            newEntry.TrackedEntityType = ETrackedEntityType.Self;

            newEntry.SkillEvents = [ ESkillEventTrackingType.SkillCast ];

            newEntry.TrackedSkillId = skillId;

            if (HelperMethods.DataTables.Skills.Data.TryGetValue(newEntry.TrackedSkillId.ToString(), out var skill))
            {
                newEntry.Name = skill.Name;
                newEntry.Desc = skill.Desc;

                newEntry.UpdateIconData(skill.GetIconName(), true);
            }

            newEntry.ShowIcon = true;
            newEntry.ShowName = true;
            newEntry.ShowDurationText = true;
            newEntry.ShowDurationProgessBar = true;
            newEntry.HideTrackerCondition = EHideTrackerCondition.HideWhenNoDuration;
            newEntry.IsEnabled = true;

            return newEntry;
        }

        private static TrackedEventEntry CreateNewBossSkillEventEntry(string trackerName, int skillId)
        {
            var newEntry = CreateNewBasicSkillEventEntry(trackerName, skillId);

            newEntry.TrackedEntityType = ETrackedEntityType.Everyone;
            newEntry.ExcludeSelfFromEveryoneType = true;

            newEntry.SkillEvents = [ ESkillEventTrackingType.BossWarning ];

            return newEntry;
        }
    }

    public class EventTrackerDataContainers
    {
        public int Version { get; set; } = 0;
        public Dictionary<uint, TrackerContainer> Containers { get; set; } = new();
    }

    public class EventTrackerDataContainerPresets
    {
        public int Version { get; set; } = 0;
        public List<TrackerContainer> PresetsList { get; set; } = new();
    }

    public class EventTrackerDataTrackerPresets
    {
        public int Version { get; set; } = 0;
        public List<TrackedEventEntry> PresetsList { get; set; } = new();
    }

    public enum ETrackerType
    {
        Buffs = 0,
        Skills = 1,
        Attributes = 2
    }

    public enum EContainerLayoutStyle
    {
        SingleItem = 0,
        List = 1
    }

    public enum EContainerListDirection
    {
        //Up = 0,
        Down = 1,
        //Left = 2,
        Right = 3
    }

    public enum EContainerSizeConstraint
    {
        AutoSize = 0,
        FixedSize = 1
    }

    public class TrackerContainer
    {
        [JsonProperty]
        public uint IdTracker { get; private set; } = 0;

        public TrackerContainer(uint counter)
        {
            IdTracker = counter;
        }

        public string ContainerName = "";
        public bool ShowContainerName = true;
        public bool IsContainerEnabled = true;
        public bool ShowInTaskBar = false;

        public ESourceLocationType SourceLocationType = ESourceLocationType.Manual;
        public string SourceLocationId = "";

        [JsonIgnore]
        public bool IsWindowTitleDirty = true;

        public EContainerLayoutStyle ContainerLayoutStyle = EContainerLayoutStyle.List;
        public EContainerListDirection ContainerListDirection = EContainerListDirection.Down;
        public EContainerSizeConstraint ContainerSizeConstraint = EContainerSizeConstraint.AutoSize;
        public Vector2 ContainerFixedSize = new();
        public bool TransparentBackground = false;
        [JsonIgnore]
        public bool HadTransparentBackground = false;
        public bool ModifiedWindowOpacity = false;
        public int WindowOpacityValue = 100;
        [JsonIgnore]
        public int LastSetOpacity = 100;

        public bool ShowCasterInTooltip = true;
        public bool ShowDurationInTooltip = false;
        public bool ShowNameInTooltip = true;
        public bool ShowDescriptionInTooltip = false;
        public bool TrimLongDescriptionTooltips = true;

        public bool TrackAllBuffs = false;

        [JsonProperty]
        public bool HasEnabledTrackers { get; private set; } = false;

        public bool HideTrackerBackground = false;
        public bool HideTrackerBorders = false;

        public void RecheckTrackerStates()
        {
            HasEnabledTrackers = EventTrackers.Any(x => x.Value.IsEnabled);
        }

        public OrderedDictionary<uint, TrackedEventEntry> EventTrackers = new();

        [JsonIgnore]
        public Dictionary<long, float> EventWindowSizes = new();

        [JsonIgnore]
        public bool HasLoadedSaveDataOnce = false;

        public object Clone(uint counter, ref uint trackerCounter)
        {
            var cloned = (TrackerContainer)this.MemberwiseClone();
            cloned.IdTracker = counter;
            cloned.EventTrackers = new();
            cloned.EventWindowSizes = new();
            cloned.LastSetOpacity = 100;
            cloned.HadTransparentBackground = false;
            // If we ever need to perform a clone we are breaking the original source type
            // This can be manually restored in the rare case we didn't want to break it
            cloned.SourceLocationType = ESourceLocationType.Manual;
            foreach (var item in EventTrackers)
            {
                var newTracker = (TrackedEventEntry)item.Value.Clone(++trackerCounter);

                cloned.EventTrackers.Add(newTracker.IdTracker, newTracker);
            }
            return cloned;
        }
    }

    public enum ETrackedEntityType
    {
        Self = 0,
        UserTarget = 1,
        DefinedTarget = 2,
        Party = 3,
        //Raid = 4,
        Everyone = 5,
        Summons = 6
    }

    public enum EHideTrackerCondition
    {
        NeverHide = 0,
        HideWhenNoDuration = 1,
        HideOnRemoveEvent = 2,
        HideOnSpecificEvent = 3
    }

    public enum ESkillEventTrackingType
    {
        SkillCast = 0,
        BossWarning = 1,
        NoticeTip = 2
    }

    public enum EDurationProgressBarStyle
    {
        Line = 0,
        Circle = 1
    }

    public enum ESourceLocationType
    {
        Manual = 0,
        Internal = 1,
        Custom = 2,
    }

    public class TrackedEventEntry
    {
        [JsonProperty]
        public uint IdTracker { get; private set; } = 0;

        public int Version = EventTrackerWindow.EventTrackerSaveVersion;

        public TrackedEventEntry(uint counter)
        {
            IdTracker = counter;
        }

        public string TrackerName = "";
        public bool IsEnabled = false;
        public bool IsHidden = false;

        public ESourceLocationType SourceLocationType = ESourceLocationType.Manual;
        //public string SourceLocationPath = ""; // Where the Source is located
        public string SourceLocationId = ""; // Unique Id (Key) per SourceLocationType and SourceLocationPath

        public LoadEvent LoadEvents = new();

        public ETrackerType TrackerType = ETrackerType.Buffs;

        public ETrackedEntityType TrackedEntityType = ETrackedEntityType.Self;
        public bool ExcludeSelfFromEveryoneType = false;
        public long DefinedEntityTargetUuid = 0;
        [JsonIgnore]
        public long LastUserTargetEntityUuid = 0;
        public bool EventSourceMustBeSelf = false;
        public bool OnlyTrackSummonsFromSelf = false;

        public int TrackedBuffId = 0;
        public List<Zproto.EBuffEventType> BuffEvents = new();

        public int TrackedSkillId = 0;
        public List<ESkillEventTrackingType> SkillEvents = new();
        public bool UseBossDbmCdDuration = false;

        public bool OverrideTrackedId = false;
        public int OverrideTrackedIdValue = 0;

        public string TrackedAttributeName = "";

        public bool LimitToOneTrackerInstance = false;

        public bool ShowIcon = false;
        public string OriginalIconPath = "";
        public bool UseCustomIcon = false;
        public string CustomIconPath = "";
        public bool ShowEntityName = false;
        public bool ShowName = false;
        public bool UseCustomName = false;
        public string CustomName = "";
        public bool ShowNameBeforeIcon = false;
        public bool NameNewLineBeforeIcon = false;
        public bool ShowLayers = false;
        public bool ShowLayersBeforeIcon = false;
        public bool LayersNewLineBeforeIcon = false;
        public bool ShowDurationText = false;
        public bool DurationTextSameLine = false;
        public bool UseMinutesForLongDuration = false;
        public bool UseCustomDurationTextColor = false;
        public Vector4 DurationTextColor = new Vector4(1, 1, 1, 1);
        public bool ShowDurationProgessBar = false;
        public bool ShowIconInsideProgressBar = false;
        public bool ShowNameInsideProgressBar = false;
        public int TextInsideProgressBarOffset = 0;
        public bool ShowLayersInsideProgressBar = false;
        public bool DurationProgressBarSameLine = false;
        public int DurationProgressBarVerticalOffset = 0;
        public bool ShowDurationTextInProgressBar = false;
        public bool ColorDurationProgressBarByType = false;

        public EDurationProgressBarStyle DurationProgressBarStyle = EDurationProgressBarStyle.Line;
        public bool UseDurationProgressBarCircleBackgroundFill = false;
        public Vector4 DurationProgressBarCircleBackgroundColor = new Vector4(0, 0, 0, 0.25f);

        public int NameSize = 18;
        public int IconSize = 18;
        public int LayerSize = 18;
        public int DurationTextSize = 18;
        public int DurationProgressBarSize = 18;
        public int DurationProgressBarTextSize = 18;
        public int DurationProgressBarCircleThickness = 5;

        public bool ShowDurationEnded = false;
        public bool HideIfNoDuration = false;
        public EHideTrackerCondition HideTrackerCondition = EHideTrackerCondition.NeverHide;
        public Zproto.EBuffEventType? HideOnSpecificBuffEventValue = null;

        public bool OverrideDuration = false;
        public int DurationOverrideValue = 0;

        public bool IgnoreCooldownDuration = false;
        public bool UseLayersForDuration = false;
        public int LayersForDurationMaxValue = 0;

        public List<RaidWarningTrackerData> RaidWarningTrackerDatas = new();

        public string Name = "";
        public string Desc = "";
        public bool IsIconValid = false;
        public string IconPath = "";

        public EChargeCooldownType ChargeCooldownType = EChargeCooldownType.ChargeCooldownStartsAfterPrevious;
        public bool ShowChargeCooldownsAsSingleBar = true;
        public EChargeDurationDisplayType ChargeDurationDisplayType = EChargeDurationDisplayType.CombinedTotal;

        public bool DebugLogTracker = false;

        [JsonIgnore]
        public ConcurrentDictionary<long, EventData> EventData = new();

        public void UpdateIconData(string officialIconPath, bool setOfficialPath)
        {
            if (ShowIcon || setOfficialPath)
            {
                if (setOfficialPath)
                {
                    OriginalIconPath = officialIconPath;
                }
                
                if (UseCustomIcon && string.IsNullOrEmpty(CustomIconPath))
                {
                    return;
                }

                string typePath = "Buffs";
                if (TrackerType == ETrackerType.Buffs)
                {
                    typePath = "Buffs";
                }
                else if (TrackerType == ETrackerType.Skills)
                {
                    typePath = "Skills";
                }
                else if (TrackerType == ETrackerType.Attributes)
                {
                    typePath = "Attributes";
                }

                string iconPath = "";
                if (!UseCustomIcon)
                {
                    iconPath = Path.Combine(Utils.DATA_DIR_NAME, "Images", typePath, $"{OriginalIconPath}.png");
                }
                else
                {
                    iconPath = Path.Combine(Utils.DATA_DIR_NAME, "Images", typePath, $"{CustomIconPath}.png");
                }

                if (File.Exists(iconPath))
                {
                    IsIconValid = true;
                    if (!UseCustomIcon)
                    {
                        IconPath = Path.Combine(typePath, OriginalIconPath);
                    }
                    else
                    {
                        IconPath = Path.Combine(typePath, CustomIconPath);
                    }
                }
                else
                {
                    IsIconValid = false;
                    if (File.Exists(Path.Combine(Utils.DATA_DIR_NAME, "Images", "Misc", $"com_img_empty.png")))
                    {
                        IsIconValid = true;
                        IconPath = Path.Combine("Misc", $"com_img_empty");
                    }
                }
            }
        }

        public int GetTrackedId()
        {
            if (TrackerType == ETrackerType.Skills)
            {
                if (OverrideTrackedId)
                {
                    if (OverrideTrackedIdValue > 0)
                    {
                        return OverrideTrackedIdValue;
                    }
                }
                return TrackedSkillId;
            }

            return TrackedBuffId;
        }

        public object Clone(uint counter)
        {
            var cloned = (TrackedEventEntry)this.MemberwiseClone();
            cloned.IdTracker = counter;
            cloned.EventData = new();
            cloned.LastUserTargetEntityUuid = 0;
            // If we ever need to perform a clone we are breaking the original source type
            // This can be manually restored in the rare case we didn't want to break it
            cloned.SourceLocationType = ESourceLocationType.Manual;
            return cloned;
        }
    }

    public class RaidWarningTrackerData
    {
        public bool IsEnabled = false;
        public ERaidWarningActivationType ActivationType = ERaidWarningActivationType.OnGain;
        public bool UseConditionValueCheck = false;
        public EConditionCheckType CheckConditionType = EConditionCheckType.EqualTo;
        public float CheckConditionValue = 0;
        public string MessageFormat = "";
        public bool PlaySound = false;
        public string CustomSoundPath = "";
    }

    public enum ERaidWarningActivationType
    {
        OnGain = 1,
        OnRemove = 2,
        OnLayerCount = 3,
        OnChargeGainCooldown = 4,
    }

    public enum EConditionCheckType
    {
        EqualTo = 0,
        LessThan = 1,
        GreaterThan = 2,
        LessThanOrEqualTo = 3,
        GreaterThanOrEqualTo = 4
    }

    public class LoadEvent
    {
        public bool InCombat = false;
        public bool NotInCombat = false;
        public bool InEncounter = false;
        public bool IsAlive = false;
        public bool IsDead = false;
        public bool UseRoleTypes = false;
        public List<DataTypes.Enums.Professions.ERoleType> RoleType = [];
        public bool UseProfession = false;
        public List<DataTypes.Enums.Professions.EProfessionId> Profession = [];
        public bool UseSubProfession = false;
        public List<DataTypes.Enums.Professions.SubProfessionId> SubProfession = [];
        public bool UseSceneIds = false;
        public string SceneIds = "";

        // These are checked outside of the CheckShouldLoad function
        public bool IsOwnerAlive = false;
        public bool IsOwnerDead = false;

        public bool KeepOnSceneChange = false;
        public bool KeepOnWipe = false;
        public bool KeepOnRestart = false;

        [JsonProperty]
        public List<int> SceneIdValues { get; private set; } = new();

        public void UpdateSceneIdValuesFromString()
        {
            var ids = SceneIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            SceneIdValues = new();
            foreach (var id in ids)
            {
                if (int.TryParse(id, out var newId))
                {
                    if (newId < 9_999_999 && newId > -9_999_999)
                    {
                        SceneIdValues.Add(newId);
                    }
                }
            }
            SceneIds = string.Join(", ", SceneIdValues);
        }

        public void UpdateSceneIdFromList()
        {
            SceneIds = string.Join(", ", SceneIdValues);
        }

        public bool CheckShouldLoad()
        {
            bool isInCombat = false;
            EActorState? actorState = null;
            DataTypes.Enums.Professions.ERoleType roleType = DataTypes.Enums.Professions.ERoleType.None;
            if (EncounterManager.Current.Entities.TryGetValue(AppState.PlayerUUID, out var playerEntity))
            {
                var attrCombatState = playerEntity.GetAttrKV("AttrCombatState") as int?;
                isInCombat = (attrCombatState != null && attrCombatState > 0);

                var attrState = playerEntity.GetAttrKV("AttrState") as EActorState?;
                actorState = attrState;

                roleType = DataTypes.Professions.GetRoleFromBaseProfessionId(playerEntity.ProfessionId);
            }

            if (InCombat)
            {
                if (!isInCombat)
                {
                    return false;
                }
            }

            if (NotInCombat)
            {
                if (isInCombat)
                {
                    return false;
                }
            }

            if (InEncounter)
            {
                if (BattleStateMachine.IsInOpenWorld())
                {
                    return false;
                }
            }

            if (IsAlive)
            {
                if (actorState != null && actorState == EActorState.ActorStateDead)
                {
                    return false;
                }
            }

            if (IsDead)
            {
                if (actorState == null || actorState != EActorState.ActorStateDead)
                {
                    return false;
                }
            }

            if (UseRoleTypes)
            {
                if (!RoleType.Contains(roleType))
                {
                    return false;
                }
            }

            if (UseSceneIds)
            {
                if (!SceneIdValues.Contains((int)EncounterManager.Current.SceneId))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class EventData
    {
        public int Uuid = 0;
        public int Layers = 0;
        public long SourceEntityUuid = 0;
        public string SourceEntityName = "";
        public long OwnerEntityUuid = 0;
        public DataTypes.Enum.EBuffType? BuffType = null;

        [JsonIgnore]
        public EventCooldownData? Cooldown;

        public Vector4? BuffTypeToColor()
        {
            if (BuffType != null)
            {
                switch (BuffType.Value)
                {
                    case DataTypes.Enum.EBuffType.Debuff:
                        return Colors.DarkRed_Transparent;
                    case DataTypes.Enum.EBuffType.Gain:
                        return Colors.DarkGreen_Transparent;
                    case DataTypes.Enum.EBuffType.GainRecovery:
                        return Colors.LightGreen_Transparent;
                    case DataTypes.Enum.EBuffType.Item:
                        return Colors.Goldenrod_Transparent;
                    default:
                        return null;
                }
            }

            return null;
        }
    }

    public class SkillEventData : EventData
    {
        public int MaxCharges = 0;
        public List<ChargeData> ChargeTimes = new();

        public int PermanentPercentReduction = 0;
        public int PermanentFlatReduction = 0;
        public int TemporaryAcceleration = 0;
    }

    public class ChargeData
    {
        [JsonIgnore]
        public EventCooldownData? Cooldown;
    }

    public class EventCooldownData
    {
        public DateTime? Added { get; private set; }
        public DateTime? Removed { get; private set; }

        public float BaseDuration = 0;

        public float PermanentPercentReduction = 0;
        public float PermanentFlatReduction = 0;

        public float TemporaryAcceleration { get; private set; } = 0;

        public bool IsCooldownStarted { get; private set; } = false;
        public bool IsCooldownEnded { get; private set; } = false;

        private float effectiveDuration = 0;
        private float cooldownConsumed = 0;
        private DateTime updated;

        public EventCooldownData(float duration, float percentReduction, float flatReduction)
        {
            BaseDuration = duration;
            PermanentPercentReduction = percentReduction;
            PermanentFlatReduction = flatReduction;
        }

        public void StartOrUpdate(DateTime added, float? acceleration = null, bool restart = false)
        {
            if (!IsCooldownStarted || restart)
            {
                Start(added);
            }

            if (acceleration != null)
            {
                UpdateAcceleration(acceleration.Value);
            }

            UpdateProgress();
        }

        public void Start(DateTime added)
        {
            Added = added;

            effectiveDuration = (BaseDuration * (1.0f - PermanentPercentReduction)) - PermanentFlatReduction;

            effectiveDuration = MathF.Max(0.0f, effectiveDuration);

            cooldownConsumed = 0.0f;
            TemporaryAcceleration = 0;

            updated = Added.Value;

            IsCooldownStarted = true;
            IsCooldownEnded = false;

            Removed = Added.Value.AddSeconds(effectiveDuration);
        }

        public void IncreaseBaseDuration(float additionalDuration, bool isNewAbsoluteDuration = false)
        {
            if (isNewAbsoluteDuration)
            {
                BaseDuration = additionalDuration;
            }
            else
            {
                BaseDuration += additionalDuration;
            }

            effectiveDuration = (BaseDuration * (1.0f - PermanentPercentReduction)) - PermanentFlatReduction;

            effectiveDuration = MathF.Max(0.0f, effectiveDuration);

            RecalculateRemoved();
        }

        private void UpdateAcceleration(float newAcceleration)
        {
            TemporaryAcceleration = newAcceleration;
            RecalculateRemoved();
        }

        public void UpdateProgress()
        {
            if (IsCooldownEnded)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;

            if (nowUtc < Added)
            {
                return;
            }

            float delta = (float)(nowUtc - updated).TotalSeconds;

            float multiplier = 1.0f + TemporaryAcceleration;

            cooldownConsumed += delta * multiplier;
            cooldownConsumed = MathF.Min(cooldownConsumed, effectiveDuration);

            updated = nowUtc;

            RecalculateRemoved();
        }

        public float GetRemainingSeconds()
        {
            float remain = effectiveDuration - cooldownConsumed;
            return MathF.Max(0.0f, remain);
        }

        public void RecalculateRemoved()
        {
            float multiplier = 1.0f + TemporaryAcceleration;

            float realSecondsRemain = GetRemainingSeconds() / multiplier;

            Removed = DateTime.UtcNow.AddSeconds(realSecondsRemain);
        }

        public void EndCooldown()
        {
            IsCooldownEnded = true;

            Removed = DateTime.UtcNow;
        }

        public float GetProgressPercent()
        {
            UpdateProgress();

            if (effectiveDuration > 0)
            {
                return cooldownConsumed / effectiveDuration;
            }

            return 1.0f;
        }

        public bool IsFinished()
        {
            UpdateProgress();
            return (cooldownConsumed >= effectiveDuration) || IsCooldownEnded;
        }
    }

    public enum EChargeCooldownType
    {
        ChargeCooldownStartsAfterPrevious = 0,
        ChargeCooldownStartsPerCast = 1,
        AllChargesResetTogetherFromLastCast = 2,
        AllChargesResetTogetherFromFirstCast = 3,
    }

    public enum EChargeDurationDisplayType
    {
        CombinedTotal = 0,
        ResetEachCharge = 1
    }

    public class EventTrackerWindowSettings : WindowSettingsBase
    {
        public bool IsContainerEditMode = true;
        public bool EditModeShowPlaceholders = false;

        public bool HideContainersWhenGameNotFocused = false;
        public bool KeepContainersWhenZDPSFocused = false;

        public bool AlwaysIgnoreInputs = false;
        public bool ShowForceHideContainersBtnOnMainWindow = false;

        public Dictionary<uint, Vector2> ContainerPositions = new();
    }
}
