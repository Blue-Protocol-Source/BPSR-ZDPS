using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes.External
{
    public class MobsResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("location")]
        public bool Location { get; set; }
        [JsonProperty("map")]
        public string Map { get; set; }
        [JsonProperty("monster_id")]
        public long MonsterId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("respawn_time")]
        public int RespawnTime { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("uid")]
        public int UID { get; set; }
        [JsonProperty("expand")]
        public MobsResponseExpand Expand { get; set; }
    }

    public class MobsResponseExpand
    {
        [JsonProperty("map")]
        public MobsResponseExpandMap Map { get; set; }
    }

    public class MobsResponseExpandMap
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("region_data")]
        public Dictionary<string, int> RegionData { get; set; }
        [JsonProperty("uid")]
        public int UID { get; set; }
    }

    public class StatusResponse
    {
        [JsonProperty("channel_number")]
        public int ChannelNumber { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("last_hp")]
        public int LastHP { get; set; }
        [JsonProperty("last_update")]
        public string? LastUpdate { get; set; }
        [JsonProperty("update")]
        public string? Update { get; set; }
        [JsonProperty("location_image")]
        public string LocationImage { get; set; }
        [JsonProperty("mob")]
        public string Mob { get; set; }
        [JsonProperty("region")]
        public string Region { get; set; }
    }

    public class BPTimerHpReport
    {
        [JsonProperty("monster_id")]
        public long MonsterId;
        [JsonProperty("hp_pct")]
        public int HpPct;
        [JsonProperty("line")]
        public uint Line;
        [JsonProperty("pos_x", NullValueHandling = NullValueHandling.Ignore)]
        public float? PosX;
        [JsonProperty("pos_y", NullValueHandling = NullValueHandling.Ignore)]
        public float? PosY;
        [JsonProperty("pos_z", NullValueHandling = NullValueHandling.Ignore)]
        public float? PosZ;
        [JsonProperty("account_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? AccountId;
        [JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
        public long? UID;
        [JsonProperty("player_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? PlayerName;
        [JsonProperty("scene_ip", NullValueHandling = NullValueHandling.Ignore)]
        public string? SceneIp;
    }

    public class BPTimerSubscribe
    {
        [JsonProperty("clientId")]
        public string ClientId;
        [JsonProperty("subscriptions")]
        public List<string> Subscriptions;
    }

    public class BPTimerMobHpUpdate
    {
        public string MobId;
        public int Channel;
        public int Hp;
        public string? Location;
    }

    public class BPTimerMobHpUpdateConverter : JsonConverter<BPTimerMobHpUpdate>
    {
        public override BPTimerMobHpUpdate ReadJson(JsonReader reader, Type objectType, BPTimerMobHpUpdate? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray arr = JArray.Load(reader);

            return new BPTimerMobHpUpdate
            {
                MobId = (string)arr[0],
                Channel = (int)arr[1],
                Hp = (int)arr[2],
                Location = arr[3].Type == JTokenType.Null ? null : (string)arr[3],
            };
        }

        public override void WriteJson(JsonWriter writer, BPTimerMobHpUpdate? value, JsonSerializer serializer)
        {
            JArray arr = new JArray
            {
                value.MobId,
                value.Channel,
                value.Hp,
                value.Location
            };
        }
    }
}

namespace BPSR_ZDPS.Managers.External
{
    public static partial class BPTimerManager
    {
        static byte[] DataHandle =
        {
            0xF1, 0x54, 0xA9, 0x73, 0x1C, 0xCF, 0x35, 0x6A,
            0xD2, 0x2F, 0x06, 0x8A, 0x47, 0xFB, 0xC4, 0x58,
            0x33, 0x17, 0x66, 0x8C, 0x06, 0x1B, 0x5B, 0x5E,
            0x1C, 0x08, 0x0A, 0x53, 0x01, 0x53, 0x52, 0x0D,
            0x08, 0x07, 0x02, 0x11, 0x5B, 0x01, 0x0C, 0x1E,
            0x0F, 0x08, 0x1B, 0x11, 0x0C, 0x5B, 0x5D, 0x5B,
            0x04, 0x1D, 0x1C, 0x11, 0x13, 0x1A, 0x11, 0x5E,
            0x0D, 0x0D, 0x0D, 0x0A, 0x1D, 0x00, 0x18, 0x5F,
            0x03, 0x1F, 0x19, 0x02, 0x0E, 0x0F, 0x2A, 0x85,
            0xF9, 0x60, 0x01, 0x98, 0x43, 0xAC, 0x3E, 0x76,
            0xEF, 0xF4, 0x0B, 0xCA, 0x50, 0xBF, 0x65, 0x12,
            0xD7, 0x41, 0x72, 0xE3, 0xB1, 0x24, 0x1B, 0x49,
            0xA2, 0xFA, 0x6D, 0xD5, 0x73, 0xA9, 0x54, 0xF1,
        };
    }
}