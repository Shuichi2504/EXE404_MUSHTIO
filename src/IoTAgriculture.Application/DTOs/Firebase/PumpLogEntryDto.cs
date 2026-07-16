using System.Text.Json.Serialization;

namespace IoTAgriculture.DTOs.Firebase
{
    public class PumpLogEntryDto
    {
        [JsonPropertyName("pumpKey")]
        public string PumpKey { get; set; } = string.Empty;

        [JsonPropertyName("relayKey")]
        public string RelayKey { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public bool Value { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = "manual";

        [JsonPropertyName("actorUserId")]
        public string? ActorUserId { get; set; }

        [JsonPropertyName("actorName")]
        public string ActorName { get; set; } = "System";

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("utcTime")]
        public string UtcTime { get; set; } = string.Empty;

        [JsonPropertyName("localTime")]
        public string LocalTime { get; set; } = string.Empty;
    }
}
