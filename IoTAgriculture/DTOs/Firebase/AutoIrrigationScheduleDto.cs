using System.Text.Json.Serialization;

namespace IoTAgriculture.DTOs.Firebase
{
    public class AutoIrrigationScheduleDto
    {
        [JsonPropertyName("pumpKey")]
        public string PumpKey { get; set; } = string.Empty;

        [JsonPropertyName("relayKey")]
        public string RelayKey { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("intervalMinutes")]
        public int IntervalMinutes { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = "06:00";

        [JsonPropertyName("smartEnabled")]
        public bool SmartEnabled { get; set; }

        [JsonPropertyName("sensorKey")]
        public string? SensorKey { get; set; }

        [JsonPropertyName("soilMoistureThreshold")]
        public double SoilMoistureThreshold { get; set; } = 30;

        [JsonPropertyName("maxDurationMinutes")]
        public int MaxDurationMinutes { get; set; } = 10;

        [JsonPropertyName("cooldownMinutes")]
        public int CooldownMinutes { get; set; } = 30;

        [JsonPropertyName("lastRunAt")]
        public string? LastRunAt { get; set; }

        [JsonPropertyName("lastRunLocal")]
        public string? LastRunLocal { get; set; }

        [JsonPropertyName("activeUntilAt")]
        public string? ActiveUntilAt { get; set; }

        [JsonPropertyName("activeUntilLocal")]
        public string? ActiveUntilLocal { get; set; }

        [JsonPropertyName("nextRunAt")]
        public string? NextRunAt { get; set; }

        [JsonPropertyName("nextRunLocal")]
        public string? NextRunLocal { get; set; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("updatedLocal")]
        public string? UpdatedLocal { get; set; }

        [JsonPropertyName("lastSmartRunAt")]
        public string? LastSmartRunAt { get; set; }

        [JsonPropertyName("lastSmartRunLocal")]
        public string? LastSmartRunLocal { get; set; }
    }
}
