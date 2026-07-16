using System.Text.Json.Serialization;

namespace IoTAgriculture.DTOs.Firebase
{
    public class AlertEntryDto
    {
        [JsonPropertyName("deviceKey")]
        public string DeviceKey { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("alertType")]
        public string AlertType { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "warning";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("metric")]
        public string Metric { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public double? Value { get; set; }

        [JsonPropertyName("threshold")]
        public double? Threshold { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("utcTime")]
        public string UtcTime { get; set; } = string.Empty;

        [JsonPropertyName("localTime")]
        public string LocalTime { get; set; } = string.Empty;

        [JsonPropertyName("resolved")]
        public bool Resolved { get; set; }
    }
}
