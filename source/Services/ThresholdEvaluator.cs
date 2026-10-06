using GameActivity.Models;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK;

namespace GameActivity.Services
{
    /// <summary>
    /// Builds warning snapshots by comparing live hardware metrics to user thresholds.
    /// Shared by end-of-session warnings and the in-game alarm tick.
    /// </summary>
    internal static class ThresholdEvaluator
    {
        /// <summary>
        /// Returns a <see cref="WarningData"/> snapshot when at least one threshold is breached;
        /// otherwise <c>null</c>.
        /// </summary>
        /// <param name="settings">Plugin settings holding threshold values (<c>0</c> = disabled).</param>
        /// <param name="metrics">Current hardware sample.</param>
        /// <returns>Snapshot with warm flags set, or <c>null</c>.</returns>
        public static WarningData BuildWarningIfBreached(GameActivitySettings settings, HardwareMetrics metrics)
        {
            if (settings == null || metrics == null)
            {
                return null;
            }

            bool warningMinFps = settings.MinFps != 0 && metrics.FPS.HasValue && settings.MinFps >= metrics.FPS;
            bool warningMaxCpuTemp = settings.MaxCpuTemp != 0 && metrics.CpuTemperature.HasValue && settings.MaxCpuTemp <= metrics.CpuTemperature;
            bool warningMaxGpuTemp = settings.MaxGpuTemp != 0 && metrics.GpuTemperature.HasValue && settings.MaxGpuTemp <= metrics.GpuTemperature;
            bool warningMaxCpuUsage = settings.MaxCpuUsage != 0 && metrics.CpuUsage.HasValue && settings.MaxCpuUsage <= metrics.CpuUsage;
            bool warningMaxGpuUsage = settings.MaxGpuUsage != 0 && metrics.GpuUsage.HasValue && settings.MaxGpuUsage <= metrics.GpuUsage;
            bool warningMaxRamUsage = settings.MaxRamUsage != 0 && metrics.RamUsage.HasValue && settings.MaxRamUsage <= metrics.RamUsage;
            bool warningMaxCpuPower = settings.MaxCpuPower != 0 && metrics.CpuPower.HasValue && settings.MaxCpuPower <= metrics.CpuPower;
            bool warningMaxGpuPower = settings.MaxGpuPower != 0 && metrics.GpuPower.HasValue && settings.MaxGpuPower <= metrics.GpuPower;

            if (!warningMinFps && !warningMaxCpuTemp && !warningMaxGpuTemp
                && !warningMaxCpuUsage && !warningMaxGpuUsage && !warningMaxRamUsage
                && !warningMaxCpuPower && !warningMaxGpuPower)
            {
                return null;
            }

            return new WarningData
            {
                At = System.DateTime.Now.ToLocalTime().ToString("HH:mm"),
                FpsData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityFps"),
                    Value = metrics.FPS ?? 0,
                    IsWarm = warningMinFps,
                },
                CpuTempData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityCpuTemp"),
                    Value = metrics.CpuTemperature ?? 0,
                    IsWarm = warningMaxCpuTemp,
                },
                GpuTempData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityGpuTemp"),
                    Value = metrics.GpuTemperature ?? 0,
                    IsWarm = warningMaxGpuTemp,
                },
                CpuUsageData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityCpuUsage"),
                    Value = metrics.CpuUsage ?? 0,
                    IsWarm = warningMaxCpuUsage,
                },
                GpuUsageData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityGpuUsage"),
                    Value = metrics.GpuUsage ?? 0,
                    IsWarm = warningMaxGpuUsage,
                },
                RamUsageData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityRamUsage"),
                    Value = metrics.RamUsage ?? 0,
                    IsWarm = warningMaxRamUsage,
                },
                CpuPowerData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityCpuPower"),
                    Value = metrics.CpuPower ?? 0,
                    IsWarm = warningMaxCpuPower,
                },
                GpuPowerData = new Data
                {
                    Name = ResourceProvider.GetString("LOCGameActivityGpuPower"),
                    Value = metrics.GpuPower ?? 0,
                    IsWarm = warningMaxGpuPower,
                },
            };
        }

        /// <summary>
        /// Returns <c>true</c> when the snapshot has at least one warm (breached) sensor.
        /// </summary>
        /// <param name="warning">Snapshot to inspect.</param>
        /// <returns><c>true</c> if any <see cref="Data.IsWarm"/> flag is set.</returns>
        public static bool HasWarmSensor(WarningData warning)
        {
            if (warning == null)
            {
                return false;
            }

            return IsWarm(warning.FpsData)
                || IsWarm(warning.CpuTempData)
                || IsWarm(warning.GpuTempData)
                || IsWarm(warning.CpuUsageData)
                || IsWarm(warning.GpuUsageData)
                || IsWarm(warning.RamUsageData)
                || IsWarm(warning.CpuPowerData)
                || IsWarm(warning.GpuPowerData);
        }

        private static bool IsWarm(Data data)
        {
            return data != null && data.IsWarm;
        }
    }
}
