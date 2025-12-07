using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes.External
{
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
