using Playnite.SDK;
using CommonPluginsShared;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace GameActivity.Services
{
    public class PerfCounter
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private static ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;


        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX
        {
            internal uint dwLength;
            internal uint dwMemoryLoad;
            internal ulong ullTotalPhys;
            internal ulong ullAvailPhys;
            internal ulong ullTotalPageFile;
            internal ulong ullAvailPageFile;
            internal ulong ullTotalVirtual;
            internal ulong ullAvailVirtual;
            internal ulong ullAvailExtendedVirtual;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);


        private static OpenHardwareMonitor.Hardware.Computer _myComputerOH;
        private static OpenHardwareMonitor.Hardware.Computer myComputerOH
        {
            get
            {
                if (_myComputerOH == null)
                {
                    _myComputerOH = new OpenHardwareMonitor.Hardware.Computer
                    {
                        CPUEnabled = true,
                        GPUEnabled = true
                    };
                    _myComputerOH.Open();
                    UpdateVisitorOpenHardware updateVisitor = new UpdateVisitorOpenHardware();
                    _myComputerOH.Accept(updateVisitor);
                }
                return _myComputerOH;
            }
        }

        private static LibreHardwareMonitor.Hardware.Computer _myComputerLH;
        private static LibreHardwareMonitor.Hardware.Computer myComputerLH
        {
            get
            {
                if (_myComputerLH == null)
                {
                    _myComputerLH = new LibreHardwareMonitor.Hardware.Computer
                    {
                        IsCpuEnabled = true,
                        IsGpuEnabled = true
                    };
                    _myComputerLH.Open();
                    UpdateVisitorLibreHardware updateVisitor = new UpdateVisitorLibreHardware();
                    _myComputerLH.Accept(updateVisitor);
                }
                return _myComputerLH;
            }
        }


        public static int GetCpuPercentage()
        {
            double cpuUsage = 0;
            int totalCpuUsage = 0;

            try
            {
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time")
                {
                    InstanceName = "_Total"
                };

                for (int i = 0; i < 5; i++)
                {
                    cpuUsage += cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(1000);
                }
                totalCpuUsage = Convert.ToInt32(Math.Ceiling(cpuUsage / 5));
            }
            catch(Exception ex)
            {
                logger.Warn($"No CPU usage find");
                Common.LogError(ex, true);
            }

            return totalCpuUsage;
        }

        public static int GetCpuTemperature()
        {
            try
            {
                if (PluginDatabase.PluginSettings.Settings.UsedLibreHardware)
                {
                    LibreHardwareMonitor.Hardware.IHardware hardwareItem = myComputerLH.Hardware
                        .Where(x => x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.Cpu)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        LibreHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
                else
                {
                    OpenHardwareMonitor.Hardware.IHardware hardwareItem = myComputerOH.Hardware
                        .Where(x => x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.CPU)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        OpenHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"No CPU temperature find");
                Common.LogError(ex, true);
            }

            return 0;
        }

        public static int GetCpuPower()
        {
            try
            {
                if (PluginDatabase.PluginSettings.Settings.UsedLibreHardware)
                {
                    LibreHardwareMonitor.Hardware.IHardware hardwareItem = myComputerLH.Hardware
                        .Where(x => x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.Cpu)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        LibreHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
                else
                {
                    OpenHardwareMonitor.Hardware.IHardware hardwareItem = myComputerOH.Hardware
                        .Where(x => x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.CPU)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        OpenHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == OpenHardwareMonitor.Hardware.SensorType.Power && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"No CPU power find");
                Common.LogError(ex, true);
            }

            return 0;
        }


        public static int GetRamPercentage()
        {
            double ramUsage = 0;
            int TotalRamMemory = 0;
            int AvailableRamMemory = 0;
            int UsedRamMemory = 0;
            int RamUsagePercentage = 0;

            try
            {
                PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

                MEMORYSTATUSEX statEX = new MEMORYSTATUSEX();
                statEX.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                GlobalMemoryStatusEx(ref statEX);

                double ram = statEX.ullTotalPhys;

                ram /= 1024;
                ram /= 1024;

                TotalRamMemory = Convert.ToInt32(Math.Round(ram, 0));

                for (int i = 0; i < 5; i++)
                {
                    ramUsage += ramCounter.NextValue();
                    System.Threading.Thread.Sleep(1000);
                }
                AvailableRamMemory = Convert.ToInt32(Math.Round(ramUsage / 5, 0));
                UsedRamMemory = TotalRamMemory - AvailableRamMemory;
                RamUsagePercentage = UsedRamMemory * 100 / TotalRamMemory;
            }
            catch (Exception ex)
            {
                logger.Warn($"No RAM usage find");
                Common.LogError(ex, true);
            }

            return RamUsagePercentage;
        }


        public static int GetGpuPercentage()
        {
            try
            {
                if (PluginDatabase.PluginSettings.Settings.UsedLibreHardware)
                {
                    LibreHardwareMonitor.Hardware.IHardware hardwareItem = myComputerLH.Hardware
                        .Where(x => x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd || x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        LibreHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
                else
                {
                    OpenHardwareMonitor.Hardware.IHardware hardwareItem = myComputerOH.Hardware
                        .Where(x => x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.GpuAti || x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.GpuNvidia)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        OpenHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == OpenHardwareMonitor.Hardware.SensorType.Load && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"No GPU usage find");
                Common.LogError(ex, true);
            }

            return 0;
        }

        public static int GetGpuTemperature()
        {
            try
            {
                if (PluginDatabase.PluginSettings.Settings.UsedLibreHardware)
                {
                    LibreHardwareMonitor.Hardware.IHardware hardwareItem = myComputerLH.Hardware
                        .Where(x => x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd || x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        LibreHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
                else
                {
                    OpenHardwareMonitor.Hardware.IHardware hardwareItem = myComputerOH.Hardware
                        .Where(x => x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.GpuAti || x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.GpuNvidia)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        OpenHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"No GPU temperature find");
                Common.LogError(ex, true);
            }

            return 0;
        }

        public static int GetGpuPower()
        {
            try
            {
                if (PluginDatabase.PluginSettings.Settings.UsedLibreHardware)
                {
                    LibreHardwareMonitor.Hardware.IHardware hardwareItem = myComputerLH.Hardware
                        .Where(x => x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd || x.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        LibreHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
                else
                {
                    OpenHardwareMonitor.Hardware.IHardware hardwareItem = myComputerOH.Hardware
                        .Where(x => x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.GpuAti || x.HardwareType == OpenHardwareMonitor.Hardware.HardwareType.GpuNvidia)?.FirstOrDefault();
                    if (hardwareItem != null)
                    {
                        hardwareItem.Update();
                        OpenHardwareMonitor.Hardware.ISensor sensorItem = hardwareItem.Sensors
                            .Where(x => x.SensorType == OpenHardwareMonitor.Hardware.SensorType.Power && x.Value.HasValue)?.FirstOrDefault();
                        if (sensorItem != null)
                        {
                            return (int)Math.Round((float)sensorItem.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"No GPU power find");
                Common.LogError(ex, true);
            }

            return 0;
        }
    }
}
