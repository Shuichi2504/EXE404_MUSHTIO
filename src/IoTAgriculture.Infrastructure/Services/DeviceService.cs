using IoTAgriculture.DTOs.Firebase;
using IoTAgriculture.Services.Interfaces;

namespace IoTAgriculture.Services
{
    public class DeviceService : IDeviceService
    {
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();
        private readonly IFirebaseRtdbService _firebase;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IFirebaseRtdbService firebase,
            ILogger<DeviceService> logger)
        {
            _firebase = firebase;
            _logger = logger;
        }

        public Task<PumpStateDto?> GetPumpStateAsync(string pumpKey)
        {
            return _firebase.GetAsync<PumpStateDto>($"devices/{pumpKey}");
        }

        public async Task SetRelayAsync(
            string pumpKey,
            string relayKey,
            bool value,
            string source = "manual",
            string? actorUserId = null,
            string? actorName = null,
            CancellationToken cancellationToken = default)
        {
            var cleanRelay = relayKey.Trim();
            var nowUtc = DateTimeOffset.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, VietnamTimeZone);

            await _firebase.SetAsync($"devices/{pumpKey}/{cleanRelay}", value, cancellationToken);
            await _firebase.PatchAsync(
                $"devices/{pumpKey}",
                new
                {
                    timestamp = nowUtc.ToUnixTimeSeconds().ToString(),
                    lastActionAt = nowUtc.ToString("O"),
                    lastActionLocal = nowLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                    lastActionSource = source,
                    lastActionBy = actorName ?? "System"
                },
                cancellationToken);

            await _firebase.PushAsync(
                $"pumpLogs/{pumpKey}",
                new PumpLogEntryDto
                {
                    PumpKey = pumpKey,
                    RelayKey = cleanRelay,
                    Value = value,
                    Action = value ? "ON" : "OFF",
                    Source = source,
                    ActorUserId = actorUserId,
                    ActorName = actorName ?? "System",
                    Timestamp = nowUtc.ToUnixTimeMilliseconds(),
                    UtcTime = nowUtc.ToString("O"),
                    LocalTime = nowLocal.ToString("yyyy-MM-dd HH:mm:ss")
                },
                cancellationToken);
        }

        public async Task<IReadOnlyList<PumpLogEntryDto>> GetPumpLogsAsync(string pumpKey, int limit = 50)
        {
            var raw = await _firebase.GetAsync<Dictionary<string, PumpLogEntryDto>>($"pumpLogs/{pumpKey}")
                ?? new Dictionary<string, PumpLogEntryDto>();

            return raw
                .Select(kvp =>
                {
                    var item = kvp.Value ?? new PumpLogEntryDto();
                    item.PumpKey = string.IsNullOrWhiteSpace(item.PumpKey) ? pumpKey : item.PumpKey;
                    return item;
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(Math.Clamp(limit, 1, 200))
                .ToList();
        }

        public Task<AutoIrrigationScheduleDto?> GetScheduleAsync(string pumpKey, string relayKey)
        {
            return _firebase.GetAsync<AutoIrrigationScheduleDto>($"pumpSchedules/{pumpKey}/{relayKey.Trim()}");
        }

        public async Task<AutoIrrigationScheduleDto> SaveScheduleAsync(
            string pumpKey,
            string relayKey,
            UpsertAutoIrrigationScheduleDto dto)
        {
            ValidateSchedule(dto);
            var cleanRelay = relayKey.Trim();
            var existing = await GetScheduleAsync(pumpKey, cleanRelay);
            var nowUtc = DateTimeOffset.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, VietnamTimeZone);
            var schedule = new AutoIrrigationScheduleDto
            {
                PumpKey = pumpKey,
                RelayKey = cleanRelay,
                Enabled = dto.Enabled,
                IntervalMinutes = dto.IntervalMinutes,
                DurationSeconds = dto.DurationSeconds,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                SmartEnabled = dto.SmartEnabled,
                SensorKey = string.IsNullOrWhiteSpace(dto.SensorKey) ? existing?.SensorKey : dto.SensorKey.Trim(),
                SoilMoistureThresholdEnabled = dto.SoilMoistureThresholdEnabled,
                SoilMoistureThreshold = dto.SoilMoistureThreshold,
                AirTempThresholdEnabled = dto.AirTempThresholdEnabled,
                AirTempMin = dto.AirTempMin,
                AirTempMax = dto.AirTempMax,
                AirHumidityThresholdEnabled = dto.AirHumidityThresholdEnabled,
                AirHumidityThreshold = dto.AirHumidityThreshold,
                CooldownMinutes = dto.CooldownMinutes,
                LastRunAt = existing?.LastRunAt,
                LastRunLocal = existing?.LastRunLocal,
                ActiveUntilAt = existing?.ActiveUntilAt,
                ActiveUntilLocal = existing?.ActiveUntilLocal,
                LastSmartRunAt = existing?.LastSmartRunAt,
                LastSmartRunLocal = existing?.LastSmartRunLocal,
                LastTriggeredAt = existing?.LastTriggeredAt,
                LastTriggeredLocal = existing?.LastTriggeredLocal,
                UpdatedAt = nowUtc.ToString("O"),
                UpdatedLocal = nowLocal.ToString("yyyy-MM-dd HH:mm:ss")
            };

            if (schedule.Enabled)
            {
                var nextRun = CalculateNextRun(schedule, nowLocal.DateTime);
                schedule.NextRunAt = ToUtcOffset(nextRun).ToString("O");
                schedule.NextRunLocal = nextRun.ToString("yyyy-MM-dd HH:mm:ss");
            }

            await _firebase.SetAsync($"pumpSchedules/{pumpKey}/{schedule.RelayKey}", schedule);
            return schedule;
        }

        public async Task ProcessSchedulesAsync(CancellationToken cancellationToken = default)
        {
            var schedules = await _firebase.GetAsync<Dictionary<string, Dictionary<string, AutoIrrigationScheduleDto>>>(
                "pumpSchedules",
                cancellationToken);

            if (schedules == null || schedules.Count == 0)
            {
                return;
            }

            var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone).DateTime;
            foreach (var pumpEntry in schedules)
            {
                var pumpKey = pumpEntry.Key;
                if (pumpEntry.Value == null)
                {
                    continue;
                }

                foreach (var relayEntry in pumpEntry.Value)
                {
                    var schedule = relayEntry.Value;
                    if (schedule == null)
                    {
                        continue;
                    }

                    schedule.PumpKey = pumpKey;
                    schedule.RelayKey = string.IsNullOrWhiteSpace(schedule.RelayKey)
                        ? relayEntry.Key
                        : schedule.RelayKey;

                    var activeUntilLocal = ParseLocalDateTime(schedule.ActiveUntilLocal);
                    if (activeUntilLocal != null)
                    {
                        if (activeUntilLocal <= nowLocal)
                        {
                            await SetRelayAsync(
                                pumpKey,
                                schedule.RelayKey,
                                false,
                                "schedule",
                                cancellationToken: cancellationToken);
                            schedule.ActiveUntilAt = null;
                            schedule.ActiveUntilLocal = null;
                            await _firebase.SetAsync(
                                $"pumpSchedules/{pumpKey}/{schedule.RelayKey}",
                                schedule,
                                cancellationToken);
                        }

                        continue;
                    }

                    if (!schedule.Enabled)
                    {
                        continue;
                    }

                    var nextRunLocal = ParseLocalDateTime(schedule.NextRunLocal);
                    if (nextRunLocal == null)
                    {
                        nextRunLocal = CalculateNextRun(schedule, nowLocal);
                    }

                    if (nextRunLocal > nowLocal)
                    {
                        continue;
                    }

                    await SetRelayAsync(
                        pumpKey,
                        schedule.RelayKey,
                        true,
                        "schedule",
                        cancellationToken: cancellationToken);
                    var stopAtLocal = nowLocal.AddSeconds(schedule.DurationSeconds);
                    schedule.LastRunAt = ToUtcOffset(nowLocal).ToString("O");
                    schedule.LastRunLocal = nowLocal.ToString("yyyy-MM-dd HH:mm:ss");
                    schedule.ActiveUntilAt = ToUtcOffset(stopAtLocal).ToString("O");
                    schedule.ActiveUntilLocal = stopAtLocal.ToString("yyyy-MM-dd HH:mm:ss");

                    // Move next run strictly forward so the scheduler won't retrigger the same slot.
                    var recomputeFrom = nowLocal.AddMinutes(schedule.IntervalMinutes);
                    var nextRun = CalculateNextRun(schedule, recomputeFrom);
                    if (nextRun < stopAtLocal)
                    {
                        nextRun = stopAtLocal;
                    }
                    schedule.NextRunAt = ToUtcOffset(nextRun).ToString("O");
                    schedule.NextRunLocal = nextRun.ToString("yyyy-MM-dd HH:mm:ss");

                    await _firebase.SetAsync(
                        $"pumpSchedules/{pumpKey}/{schedule.RelayKey}",
                        schedule,
                        cancellationToken);
                }
            }
        }

        public async Task ProcessSmartIrrigationAsync(CancellationToken cancellationToken = default)
        {
            var schedules = await _firebase.GetAsync<Dictionary<string, Dictionary<string, AutoIrrigationScheduleDto>>>(
                "pumpSchedules",
                cancellationToken);
            if (schedules == null || schedules.Count == 0)
            {
                return;
            }

            var sensors = await _firebase.GetAsync<Dictionary<string, SensorStateDto>>(
                "devices",
                cancellationToken) ?? new Dictionary<string, SensorStateDto>();
            var nowUtc = DateTimeOffset.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, VietnamTimeZone);

            foreach (var pumpEntry in schedules)
            {
                if (pumpEntry.Value == null)
                {
                    continue;
                }

                foreach (var relayEntry in pumpEntry.Value)
                {
                    try
                    {
                        var schedule = relayEntry.Value;
                        if (schedule == null || !schedule.SmartEnabled)
                        {
                            continue;
                        }

                        schedule.PumpKey = pumpEntry.Key;
                        schedule.RelayKey = string.IsNullOrWhiteSpace(schedule.RelayKey)
                            ? relayEntry.Key
                            : schedule.RelayKey;

                        if (!IsInsideOperatingWindow(schedule, nowLocal.DateTime))
                        {
                            continue;
                        }

                        var activeUntil = ParseLocalDateTime(schedule.ActiveUntilLocal);
                        if (activeUntil != null)
                        {
                            if (activeUntil <= nowLocal.DateTime)
                            {
                                await SetRelayAsync(
                                    pumpEntry.Key,
                                    schedule.RelayKey,
                                    false,
                                    "smart-threshold",
                                    cancellationToken: cancellationToken);
                                schedule.ActiveUntilAt = null;
                                schedule.ActiveUntilLocal = null;
                                await _firebase.SetAsync(
                                    $"pumpSchedules/{pumpEntry.Key}/{schedule.RelayKey}",
                                    schedule,
                                    cancellationToken);
                            }

                            continue;
                        }

                        var preferredSensor = !string.IsNullOrWhiteSpace(schedule.SensorKey) &&
                            sensors.TryGetValue(schedule.SensorKey, out var selected)
                                ? selected
                                : null;
                        var soilMoisture = preferredSensor?.GroundHumidity ??
                            sensors.Values.Select(x => x?.GroundHumidity).FirstOrDefault(x => x.HasValue);
                        var airTemperature = preferredSensor?.Temperature ??
                            sensors.Values.Select(x => x?.Temperature).FirstOrDefault(x => x.HasValue);
                        var airHumidity = preferredSensor?.Humidity ??
                            sensors.Values.Select(x => x?.Humidity).FirstOrDefault(x => x.HasValue);

                        var soilViolation = schedule.SoilMoistureThresholdEnabled &&
                            soilMoisture.HasValue &&
                            schedule.SoilMoistureThreshold.HasValue &&
                            soilMoisture.Value < schedule.SoilMoistureThreshold.Value;
                        var temperatureViolation = schedule.AirTempThresholdEnabled &&
                            airTemperature.HasValue &&
                            schedule.AirTempMax.HasValue &&
                            (decimal)airTemperature.Value > schedule.AirTempMax.Value;
                        var humidityViolation = schedule.AirHumidityThresholdEnabled &&
                            airHumidity.HasValue &&
                            schedule.AirHumidityThreshold.HasValue &&
                            airHumidity.Value < schedule.AirHumidityThreshold.Value;

                        if (!soilViolation && !temperatureViolation && !humidityViolation)
                        {
                            continue;
                        }

                        var lastTriggered = ParseUtcDateTimeOffset(
                            schedule.LastTriggeredAt ?? schedule.LastSmartRunAt);
                        if (lastTriggered.HasValue &&
                            nowUtc - lastTriggered.Value < TimeSpan.FromMinutes(schedule.CooldownMinutes))
                        {
                            continue;
                        }

                        await SetRelayAsync(
                            pumpEntry.Key,
                            schedule.RelayKey,
                            true,
                            "smart-threshold",
                            cancellationToken: cancellationToken);

                        var stopAtLocal = nowLocal.DateTime.AddSeconds(schedule.DurationSeconds);
                        schedule.ActiveUntilAt = ToUtcOffset(stopAtLocal).ToString("O");
                        schedule.ActiveUntilLocal = stopAtLocal.ToString("yyyy-MM-dd HH:mm:ss");
                        schedule.LastTriggeredAt = nowUtc.ToString("O");
                        schedule.LastTriggeredLocal = nowLocal.ToString("yyyy-MM-dd HH:mm:ss");
                        schedule.LastSmartRunAt = schedule.LastTriggeredAt;
                        schedule.LastSmartRunLocal = schedule.LastTriggeredLocal;
                        await _firebase.SetAsync(
                            $"pumpSchedules/{pumpEntry.Key}/{schedule.RelayKey}",
                            schedule,
                            cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Smart irrigation failed for pump {PumpKey}, relay {RelayKey}; continuing other schedules.",
                            pumpEntry.Key,
                            relayEntry.Key);
                    }
                }
            }
        }

        private static DateTime CalculateNextRun(AutoIrrigationScheduleDto schedule, DateTime referenceLocal)
        {
            var start = ParseTimeOfDay(schedule.StartTime);
            var end = ParseTimeOfDay(schedule.EndTime);
            var firstRunToday = referenceLocal.Date.Add(start);
            var endToday = referenceLocal.Date.Add(end);

            if (!schedule.Enabled)
            {
                return firstRunToday;
            }

            if (referenceLocal <= firstRunToday)
            {
                return firstRunToday;
            }

            if (referenceLocal >= endToday)
            {
                return firstRunToday.AddDays(1);
            }

            var interval = Math.Max(1, schedule.IntervalMinutes);
            var elapsedMinutes = (referenceLocal - firstRunToday).TotalMinutes;
            var cycles = Math.Ceiling(elapsedMinutes / interval);
            var candidate = firstRunToday.AddMinutes(cycles * interval);
            return candidate < endToday ? candidate : firstRunToday.AddDays(1);
        }

        private static bool IsInsideOperatingWindow(
            AutoIrrigationScheduleDto schedule,
            DateTime localTime)
        {
            var time = localTime.TimeOfDay;
            return time >= ParseTimeOfDay(schedule.StartTime) &&
                time < ParseTimeOfDay(schedule.EndTime);
        }

        private static void ValidateSchedule(UpsertAutoIrrigationScheduleDto dto)
        {
            var start = ParseTimeOfDay(dto.StartTime);
            var end = ParseTimeOfDay(dto.EndTime);
            if (end <= start)
            {
                throw new ArgumentException("EndTime must be later than StartTime.");
            }

            if (dto.SoilMoistureThresholdEnabled && !dto.SoilMoistureThreshold.HasValue)
            {
                throw new ArgumentException("SoilMoistureThreshold is required when enabled.");
            }

            if (dto.AirTempThresholdEnabled && !dto.AirTempMax.HasValue)
            {
                throw new ArgumentException("AirTempMax is required when enabled.");
            }

            if (dto.AirTempMin.HasValue && dto.AirTempMax.HasValue &&
                dto.AirTempMin.Value >= dto.AirTempMax.Value)
            {
                throw new ArgumentException("AirTempMax must be greater than AirTempMin.");
            }

            if (dto.AirHumidityThresholdEnabled && !dto.AirHumidityThreshold.HasValue)
            {
                throw new ArgumentException("AirHumidityThreshold is required when enabled.");
            }
        }

        private static TimeSpan ParseTimeOfDay(string? raw)
        {
            return TimeSpan.TryParse(raw, out var parsed) ? parsed : new TimeSpan(6, 0, 0);
        }

        private static DateTime? ParseLocalDateTime(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return DateTime.TryParse(raw, out var parsed) ? parsed : null;
        }

        private static DateTimeOffset? ParseUtcDateTimeOffset(string? raw)
        {
            return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
        }

        private static DateTimeOffset ToUtcOffset(DateTime localTime)
        {
            var offset = VietnamTimeZone.GetUtcOffset(localTime);
            return new DateTimeOffset(localTime, offset).ToUniversalTime();
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
