using System.ComponentModel.DataAnnotations;

namespace IoTAgriculture.DTOs.Firebase
{
    public class UpsertAutoIrrigationScheduleDto
    {
        public bool Enabled { get; set; }

        [Range(1, 1440)]
        public int IntervalMinutes { get; set; }

        [Range(1, 1440)]
        public int DurationMinutes { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string StartTime { get; set; } = "06:00";

        public bool SmartEnabled { get; set; }

        public string? SensorKey { get; set; }

        [Range(1, 100)]
        public double SoilMoistureThreshold { get; set; } = 30;

        [Range(1, 240)]
        public int MaxDurationMinutes { get; set; } = 10;

        [Range(1, 1440)]
        public int CooldownMinutes { get; set; } = 30;
    }
}
