using System.Text.Json.Serialization;

namespace maFileTool.Services.SteamAuth
{
    public class Confirmation
    {
        [JsonPropertyName("id")]
        public ulong ID { get; set; }

        [JsonPropertyName("nonce")]
        public ulong Key { get; set; }

        [JsonPropertyName("creator_id")]
        public ulong Creator { get; set; }

        [JsonPropertyName("headline")]
        public string? Headline { get; set; }

        [JsonPropertyName("summary")]
        public List<String>? Summary { get; set; }

        [JsonPropertyName("accept")]
        public string? Accept { get; set; }

        [JsonPropertyName("cancel")]
        public string? Cancel { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EMobileConfirmationType ConfType { get; set; } = EMobileConfirmationType.Invalid;

        public enum EMobileConfirmationType
        {
            Invalid = 0,
            Test = 1,
            Trade = 2,
            MarketListing = 3,
            FeatureOptOut = 4,
            PhoneNumberChange = 5,
            AccountRecovery = 6
        }
    }

    public class ConfirmationsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("needauth")]
        public bool NeedAuthentication { get; set; }

        [JsonPropertyName("conf")]
        public Confirmation[]? Confirmations { get; set; }
    }
}
