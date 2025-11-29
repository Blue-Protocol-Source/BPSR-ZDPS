using BPSR_DeepsServ.Models;
using System.Text.Json.Serialization;

namespace BPSR_DeepsServ
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ReportForm))]
    [JsonSerializable(typeof(EncounterReport))]
    [JsonSerializable(typeof(PartyMember))]
    [JsonSerializable(typeof(DiscordWebhookPayload))]
    [JsonSerializable(typeof(DiscordEmbed))]
    [JsonSerializable(typeof(EmbedFooter))]
    [JsonSerializable(typeof(EmbedImage))]
    [JsonSerializable(typeof(EmbedThumbnail))]
    [JsonSerializable(typeof(EmbedAuthor))]
    [JsonSerializable(typeof(EmbedField))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
