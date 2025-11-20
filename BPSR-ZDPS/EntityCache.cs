using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public class EntityCache
    {
        public static EntityCache Instance = new();

        public EntityCacheFile Cache = new();
        public string FilePath = Path.Combine(Utils.DATA_DIR_NAME, "EntityCache.json");
        public TimeSpan BufferDelay = TimeSpan.FromSeconds(5);
        private Task? SaveTask;
        private readonly System.Threading.Lock syncLock = new();

        public EntityCacheLine? Get(long uuid)
        {
            if (Cache.Lines.TryGetValue(uuid, out var outLine))
            {
                return outLine;
            }

            return null;
        }

        public EntityCacheLine GetOrCreate(long uuid)
        {
            if (Cache.Lines.TryGetValue(uuid, out var outLine))
            {
                return outLine;
            }

            var newEntityCacheLine = new EntityCacheLine() { UUID = uuid, UID = Utils.UuidToEntityId(uuid) };
            Cache.Lines.TryAdd(uuid, newEntityCacheLine);

            return newEntityCacheLine;
        }

        // Updates an entire entry item in the EntityCache
        // Does not delta update
        public void Set(EntityCacheLine item)
        {
            if (Cache != null)
            {
                Cache.Lines[item.UUID] = item;
            }
        }

        public void SetName(long uuid, string name)
        {
            if (Cache != null)
            {
                if (Cache.Lines.TryGetValue(uuid, out var item))
                {
                    item.Name = name;
                }
                else
                {
                    Cache.Lines.TryAdd(uuid, new EntityCacheLine() { UUID = uuid, UID = Utils.UuidToEntityId(uuid), Name = name });
                }
            }
        }

        public void Load()
        {
            //var data = File.OpenRead(FilePath);
            //Cache = ProtoBuf.Serializer.Deserialize<EntityCacheFile>(data);
            //Cache = JsonConvert.DeserializeObject<EntityCacheFile>(data);
            if (File.Exists(FilePath))
            {
                using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader file = new StreamReader(fs, Encoding.UTF8))
                    {
                        JsonSerializer serializer = new();
                        Cache = (EntityCacheFile)serializer.Deserialize(file, typeof(EntityCacheFile));
                    }
                }
            }
            else
            {
                Cache = new();
            }
        }

        public void Save(bool force = false)
        {
            if (force)
            {
                //var file = File.OpenWrite(FilePath);
                //ProtoBuf.Serializer.Serialize<EntityCacheFile>(file, Cache);
                //file.Close();
                using (FileStream fs = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    using (StreamWriter file = new StreamWriter(fs))
                    {
                        JsonSerializer serializer = new();
                        serializer.Serialize(file, Cache);
                    }
                }
            }
            else
            {
                if (SaveTask == null)
                {
                    SaveTask = Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(BufferDelay);
                        Save(true);
                        SaveTask = null;
                    });
                }
            }
        }

        public void PortToDB()
        {
            DB.UpdateEntityCacheLines(Cache.Lines.Values);
        }

        // TODO: Will store very basic info for each entity encountered and update it if a change is found
        // Details will include the UUID, UID, Name, AbilityScore, Profession/SubProfession
        // This data will be used as a first-to-load dataset for giving a starting point of resolved data in the meter
        // Values synced from the server should be treated as the new source of truth and replace anything in here
        // This will also be stored in an offline file to be read back on application startup - not just held in memory between encounters
    }

    [DataContract]
    public class EntityCacheLine
    {
        [DataMember(Order = 1)]
        public long UUID { get; set; }
        [DataMember(Order = 2)]
        public long UID { get; set; }
        [DataMember(Order = 3)]
        public string Name { get; set; } = "";
        [DataMember(Order = 4)]
        public int Level { get; set; } = 0;
        [DataMember(Order = 5)]
        public int AbilityScore { get; set; } = 0;
        [DataMember(Order = 6)]
        public int ProfessionId { get; set; } = 0;
        [DataMember(Order = 7)]
        public int SubProfessionId { get; set; } = 0;
    }

    [DataContract]
    public class EntityCacheFile
    {
        // Is there any value in switching to a ConcurrentDictionary for access to AddOrUpdate calls?
        // If so, the value (and update) assignment of AddOrUpdate IS NOT thread-safe still, reading is the only actual atomic operation
        [DataMember(Order = 1)]
        public System.Collections.Concurrent.ConcurrentDictionary<long, EntityCacheLine> Lines { get; set; } = [];
    }
}