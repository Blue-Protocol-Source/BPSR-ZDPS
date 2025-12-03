using Newtonsoft.Json;
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
}
