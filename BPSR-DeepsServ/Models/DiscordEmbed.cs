namespace BPSR_DeepsServ.Models
{
    using System.Text.Json.Serialization;

    public class DiscordWebhookPayload
    {
        [JsonPropertyName("embeds")]
        public List<DiscordEmbed> Embeds { get; set; } = [];
    }

    public class DiscordEmbed
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "rich";

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("color")]
        public int? Color { get; set; }

        [JsonPropertyName("footer")]
        public EmbedFooter Footer { get; set; }

        [JsonPropertyName("image")]
        public EmbedImage Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public EmbedThumbnail Thumbnail { get; set; }

        [JsonPropertyName("author")]
        public EmbedAuthor Author { get; set; }

        [JsonPropertyName("fields")]
        public List<EmbedField> Fields { get; set; } = [];
    }

    public class EmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }
    }

    public class EmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class EmbedThumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class EmbedAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }
    }

    public class EmbedField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("inline")]
        public bool? Inline { get; set; }
    }

}
