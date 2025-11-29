using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Zproto;

namespace BPSR_ZDPS.DataTypes
{
    public class DiscordWebhookPayload(string username, string content)
    {
        [JsonProperty("username")]
        public string Username { get; set; } = username;

        [JsonProperty("avatar_url ")]
        public string AvatarURL { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; } = content;

        [JsonProperty("embeds")]
        public List<DiscordEmbed> Embeds { get; set; } = [];
    }

    public class DiscordEmbed
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "rich";

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonProperty("color")]
        public int? Color { get; set; }

        [JsonProperty("footer")]
        public EmbedFooter Footer { get; set; }

        [JsonProperty("image")]
        public EmbedImage Image { get; set; }

        [JsonProperty("thumbnail")]
        public EmbedThumbnail Thumbnail { get; set; }

        [JsonProperty("author")]
        public EmbedAuthor Author { get; set; }

        [JsonProperty("fields")]
        public List<EmbedField> Fields { get; set; } = [];
    }

    public class EmbedFooter
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }

    public class EmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class EmbedThumbnail
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class EmbedAuthor
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }

    public class EmbedField
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("inline")]
        public bool? Inline { get; set; }
    }
}
