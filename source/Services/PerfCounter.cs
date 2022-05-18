using OpenHardwareMonitor.Hardware;
using Playnite.SDK;
using CommonPluginsShared;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace GameActivity.Services
{
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }


    public class PerfCounter
    {
        private static readonly ILogger logger = LogManager.GetLogger();

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

        private static Computer _myComputer;
        private static Computer myComputer
        {
            get
            {
                if (_myComputer == null)
                {
                    _myComputer = new Computer
                    {
                        CPUEnabled = true,
                        GPUEnabled = true
                    };
                    myComputer.Open();
                    UpdateVisitor updateVisitor = new UpdateVisitor();
                    myComputer.Accept(updateVisitor);
                }
                return _myComputer;
            }
        }



        public static int GetCpuPercentage()
        {
            double cpuUsage = 0;
            int totalCpuUsage = 0;

            try
            {
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time");

                cpuCounter.InstanceName = "_Total";

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
                foreach (var hardwareItem in myComputer.Hardware)
                {
                    if (hardwareItem.HardwareType == HardwareType.CPU)
                    {
                        hardwareItem.Update();
                        foreach (var sensor in hardwareItem.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                return (int)Math.Round((float)sensor.Value);
                            }
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

                double ram = (double)statEX.ullTotalPhys;

                ram /= 1024;
                ram /= 1024;

                TotalRamMemory = Convert.ToInt32(Math.Round(ram, 0));

                for (int i = 0; i < 5; i++)
                {
                    ramUsage += ramCounter.NextValue();
                    System.Threading.Thread.Sleep(1000);
                }
                AvailableRamMemory = Convert.ToInt32(Math.Round((ramUsage / 5), 0));
                UsedRamMemory = TotalRamMemory - AvailableRamMemory;
                RamUsagePercentage = ((UsedRamMemory * 100) / TotalRamMemory);
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
                foreach (var hardwareItem in myComputer.Hardware)
                {
                    if (hardwareItem.HardwareType == HardwareType.GpuNvidia)
                    {
                        hardwareItem.Update();
                        foreach (var sensor in hardwareItem.Sensors)
                        {
                            if (sensor.Identifier.ToString().Contains("load/0"))
                            {
                                return (int)Math.Round((float)sensor.Value);
                            }
                        }
                    }

                    if (hardwareItem.HardwareType == HardwareType.GpuAti)
                    {
                        hardwareItem.Update();
                        foreach (var sensor in hardwareItem.Sensors)
                        {
                            if (sensor.Identifier.ToString().Contains("load/0"))
                            {
                                return (int)Math.Round((float)sensor.Value);
                            }
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
                foreach (var hardwareItem in myComputer.Hardware)
                {
                    if (hardwareItem.HardwareType == HardwareType.GpuNvidia)
                    {
                        hardwareItem.Update();
                        foreach (var sensor in hardwareItem.Sensors)
                        {
                            if (sensor.Identifier.ToString().Contains("temperature/0"))
                            {
                                return (int)Math.Round((float)sensor.Value);
                            }
                        }
                    }

                    if (hardwareItem.HardwareType == HardwareType.GpuAti)
                    {
                        hardwareItem.Update();
                        foreach (var sensor in hardwareItem.Sensors)
                        {
                            if (sensor.Identifier.ToString().Contains("temperature/0"))
                            {
                                return (int)Math.Round((float)sensor.Value);
                            }
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
    }
}
