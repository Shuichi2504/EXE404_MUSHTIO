using System.Text.Json.Serialization;

namespace IoTAgriculture.DTOs.Firebase
{
    public class DailyLogbookDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("generatedAt")]
        public string GeneratedAt { get; set; } = string.Empty;

        [JsonPropertyName("generatedLocal")]
        public string GeneratedLocal { get; set; } = string.Empty;

        [JsonPropertyName("records")]
        public List<DailyLogbookRecordDto> Records { get; set; } = [];
    }
}
