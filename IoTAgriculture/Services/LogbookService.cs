using System.Globalization;
using System.Text.Json;
using IoTAgriculture.DTOs.Firebase;
using IoTAgriculture.Services.Interfaces;

namespace IoTAgriculture.Services
{
    public class LogbookService : ILogbookService
    {
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();
        private readonly IFirebaseRtdbService _firebase;

        public LogbookService(IFirebaseRtdbService firebase)
        {
            _firebase = firebase;
        }

        public async Task CaptureSensorSnapshotsAsync(CancellationToken cancellationToken = default)
        {
            var devices = await _firebase.GetAsync<Dictionary<string, JsonElement>>(
                "devices",
                cancellationToken) ?? new Dictionary<string, JsonElement>();

            if (devices.Count == 0)
            {
                return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var device in devices)
            {
                if (device.Value.ValueKind != JsonValueKind.Object || !IsSensorPayload(device.Value))
                {
                    continue;
                }

                var timestamp = ReadTimestamp(device.Value) ?? nowMs;
                var record = new
                {
                    timestamp = timestamp.ToString(CultureInfo.InvariantCulture),
                    temperature = ReadDouble(device.Value, "temperature"),
                    humidity = ReadDouble(device.Value, "humidity"),
                    ground_temperature = ReadLayerDouble(device.Value, "ground", "lower", "temperature"),
                    top_temperature = ReadLayerDouble(device.Value, "top", "upper", "temperature"),
                    ground_humidity = ReadLayerDouble(device.Value, "ground", "lower", "humidity"),
                    top_humidity = ReadLayerDouble(device.Value, "top", "upper", "humidity"),
                    soil_moisture = ReadDouble(device.Value, "soil_moisture")
                        ?? ReadDouble(device.Value, "soilMoisture")
                };

                await _firebase.PushAsync(
                    $"history/{device.Key}",
                    record,
                    cancellationToken);
            }
        }

        public async Task GenerateTodayLogbookAsync(CancellationToken cancellationToken = default)
        {
            var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone);
            if (!IsWithinAutoLogbookWindow(nowLocal.DateTime))
            {
                return;
            }

            await GenerateDailyLogbookAsync(DateOnly.FromDateTime(nowLocal.Date), cancellationToken);
        }

        public Task<DailyLogbookDto?> GetDailyLogbookAsync(
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            return _firebase.GetAsync<DailyLogbookDto>($"dailyLogbooks/{date:yyyy-MM-dd}", cancellationToken);
        }

        public async Task<DailyLogbookDto> GenerateDailyLogbookAsync(
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            var startLocal = date.ToDateTime(TimeOnly.MinValue);
            var endLocal = startLocal.AddDays(1);
            var startMs = ToUtcOffset(startLocal).ToUnixTimeMilliseconds();
            var endMs = ToUtcOffset(endLocal).ToUnixTimeMilliseconds();
            var nowUtc = DateTimeOffset.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, VietnamTimeZone);

            var records = await ReadSensorRecordsAsync(startMs, endMs, cancellationToken);

            var logbook = new DailyLogbookDto
            {
                Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                GeneratedAt = nowUtc.ToString("O"),
                GeneratedLocal = nowLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                Records = records
            };

            await _firebase.SetAsync($"dailyLogbooks/{logbook.Date}", logbook, cancellationToken);
            return logbook;
        }

        private async Task<List<DailyLogbookRecordDto>> ReadSensorRecordsAsync(
            long startMs,
            long endMs,
            CancellationToken cancellationToken)
        {
            var raw = await _firebase.GetAsync<Dictionary<string, Dictionary<string, JsonElement>>>(
                "history",
                cancellationToken) ?? new Dictionary<string, Dictionary<string, JsonElement>>();

            var devices = await _firebase.GetAsync<Dictionary<string, JsonElement>>(
                "devices",
                cancellationToken) ?? new Dictionary<string, JsonElement>();

            var records = new List<DailyLogbookRecordDto>();
            foreach (var sensorHistory in raw)
            {
                if (sensorHistory.Value == null)
                {
                    continue;
                }

                foreach (var entry in sensorHistory.Value)
                {
                    if (entry.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var timestamp = ReadTimestamp(entry.Value) ?? ParseTimestamp(entry.Key);
                    if (timestamp == null || timestamp < startMs || timestamp >= endMs)
                    {
                        continue;
                    }

                    var record = ReadSensorRecord(sensorHistory.Key, timestamp.Value, entry.Value, devices);
                    if (record.HasValue)
                    {
                        records.Add(record);
                    }
                }
            }

            if (records.Count == 0)
            {
                records.AddRange(ReadDeviceSnapshots(devices, startMs, endMs));
            }

            return records
                .OrderBy(x => ParseTimestamp(x.Timestamp) ?? 0)
                .ThenBy(x => x.DeviceKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<DailyLogbookRecordDto> ReadDeviceSnapshots(
            Dictionary<string, JsonElement> devices,
            long startMs,
            long endMs)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var device in devices)
            {
                if (device.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var timestamp = ReadTimestamp(device.Value) ?? nowMs;
                if (timestamp < startMs || timestamp >= endMs)
                {
                    continue;
                }

                var record = ReadSensorRecord(device.Key, timestamp, device.Value, devices);
                if (record.HasValue)
                {
                    yield return record;
                }
            }
        }

        private static DailyLogbookRecordDto ReadSensorRecord(
            string deviceKey,
            long timestamp,
            JsonElement json,
            Dictionary<string, JsonElement> devices)
        {
            var deviceName = ReadDeviceName(deviceKey, devices);
            var local = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(timestamp), VietnamTimeZone);
            var groundHumidity = ReadLayerDouble(json, "ground", "lower", "humidity");

            return new DailyLogbookRecordDto
            {
                Timestamp = timestamp.ToString(CultureInfo.InvariantCulture),
                LocalTime = local.ToString("yyyy-MM-dd HH:mm:ss"),
                DeviceKey = deviceKey,
                DeviceName = deviceName,
                Temperature = ReadDouble(json, "temperature"),
                Humidity = ReadDouble(json, "humidity"),
                GroundTemperature = ReadLayerDouble(json, "ground", "lower", "temperature"),
                TopTemperature = ReadLayerDouble(json, "top", "upper", "temperature"),
                GroundHumidity = groundHumidity,
                TopHumidity = ReadLayerDouble(json, "top", "upper", "humidity"),
                SoilMoisture = ReadDouble(json, "soil_moisture") ?? ReadDouble(json, "soilMoisture") ?? groundHumidity
            };
        }

        private static string ReadDeviceName(string deviceKey, Dictionary<string, JsonElement> devices)
        {
            if (devices.TryGetValue(deviceKey, out var deviceJson) &&
                deviceJson.ValueKind == JsonValueKind.Object)
            {
                return ReadString(deviceJson, "device_name")
                    ?? ReadString(deviceJson, "deviceName")
                    ?? deviceKey;
            }

            return deviceKey;
        }

        private static double? ReadLayerDouble(JsonElement json, string primaryPrefix, string alternatePrefix, string metric)
        {
            return ReadDouble(json, $"{primaryPrefix}_{metric}")
                ?? ReadDouble(json, ToCamel(primaryPrefix, metric))
                ?? ReadDouble(json, $"{alternatePrefix}_{metric}")
                ?? ReadDouble(json, ToCamel(alternatePrefix, metric));
        }

        private static string ToCamel(string prefix, string metric)
        {
            return prefix + char.ToUpperInvariant(metric[0]) + metric[1..];
        }

        private static double? ReadDouble(JsonElement json, string name)
        {
            if (!json.TryGetProperty(name, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            return value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static long? ReadTimestamp(JsonElement json)
        {
            return ReadString(json, "timestamp") is { } raw ? ParseTimestamp(raw) : null;
        }

        private static string? ReadString(JsonElement json, string name)
        {
            if (!json.TryGetProperty(name, out var value))
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        private static long? ParseTimestamp(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (long.TryParse(raw, out var parsed))
            {
                return parsed < 1_000_000_000_000 ? parsed * 1000 : parsed;
            }

            return DateTimeOffset.TryParse(raw, out var date)
                ? date.ToUniversalTime().ToUnixTimeMilliseconds()
                : null;
        }

        private static bool IsSensorPayload(JsonElement json)
        {
            return ReadDouble(json, "temperature") != null ||
                ReadDouble(json, "humidity") != null ||
                ReadDouble(json, "ground_humidity") != null ||
                ReadDouble(json, "groundHumidity") != null ||
                ReadDouble(json, "top_humidity") != null ||
                ReadDouble(json, "topHumidity") != null ||
                ReadDouble(json, "soil_moisture") != null ||
                ReadDouble(json, "soilMoisture") != null;
        }

        private static DateTimeOffset ToUtcOffset(DateTime localTime)
        {
            var offset = VietnamTimeZone.GetUtcOffset(localTime);
            return new DateTimeOffset(localTime, offset).ToUniversalTime();
        }

        private static bool IsWithinAutoLogbookWindow(DateTime localTime)
        {
            var timeOfDay = localTime.TimeOfDay;
            var start = new TimeSpan(6, 0, 0);
            var end = new TimeSpan(18, 0, 0);

            return timeOfDay >= start && timeOfDay < end;
        }

        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
        }
    }
}
