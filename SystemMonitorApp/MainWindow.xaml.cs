using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;
using System.Diagnostics;

namespace HardwareMonitorApp
{
    public partial class MainWindow : Window
    {
        private Computer computer;
        private DispatcherTimer timer;
        private string hardwareInfo;
        private PerformanceCounter ramCounter;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHardwareMonitor();
            InitializeTimer();
            SetTransparentWindow();
        }

        private void InitializeHardwareMonitor()
        {
            computer = new Computer
            {
                CPUEnabled = true,
                GPUEnabled = true,
                RAMEnabled = true
            };
            computer.Open();
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();
            }
            hardwareInfo = GetHardwareInfo();
            textBlockHardwareInfo.Text = hardwareInfo;
        }

        private void SetTransparentWindow()
        {
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.Opacity = 0.8;
        }

        private string GetHardwareInfo()
        {
            string info = "";

            var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.CPU);
            if (cpu != null)
            {
                info += $"CPU: {cpu.Name}\n";

                var temperatureSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);
                if (temperatureSensors.Any())
                {
                    info += "CPU Temperatures:\n";
                    foreach (var tempSensor in temperatureSensors)
                    {
                        float tempValue = tempSensor.Value.GetValueOrDefault();
                        if (tempValue > 0)
                        {
                            info += $"{tempSensor.Name}: {Math.Round(tempValue)} °C\n";
                        }
                        else
                        {
                            info += $"{tempSensor.Name}: Not available\n";
                        }
                    }
                }
                else
                {
                    info += "CPU Temperatures: Not available\n";
                }

                var coreSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core"));
                info += "Core Usage:\n";
                foreach (var core in coreSensors)
                {
                    info += $"{core.Name}: {Math.Round(core.Value.GetValueOrDefault())} %\n";
                }

                var totalLoadSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"));
                if (totalLoadSensor != null)
                {
                    info += $"Total CPU Load: {Math.Round(totalLoadSensor.Value.GetValueOrDefault())} %\n";
                }
            }
            else
            {
                info += "CPU: Not found\n";
            }

            var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
            if (gpu != null)
            {
                info += $"GPU: {gpu.Name}\n";

                var loadSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                var temperatureSensorsGpu = gpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);
                var memoryUsedSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("GPU Memory Used"));
                var memorySpeedSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory"));

                if (loadSensor != null)
                {
                    info += $"GPU Usage: {Math.Round(loadSensor.Value.GetValueOrDefault())} %\n";
                }

                if (temperatureSensorsGpu.Any())
                {
                    info += "GPU Temperatures:\n";
                    foreach (var tempSensor in temperatureSensorsGpu)
                    {
                        info += $"{tempSensor.Name}: {Math.Round(tempSensor.Value.GetValueOrDefault())} °C\n";
                    }
                }
                else
                {
                    info += "GPU Temperatures: Not available\n";
                }

                if (memoryUsedSensor != null)
                {
                    info += $"Video Memory Usage: {Math.Round(memoryUsedSensor.Value.GetValueOrDefault())} MB\n";
                }
                else
                {
                    info += "Video Memory Usage: Not available\n";
                }

                if (memorySpeedSensor != null)
                {
                    info += $"GPU Memory Speed: {Math.Round(memorySpeedSensor.Value.GetValueOrDefault())} MHz\n";
                }
                else
                {
                    info += "GPU Memory Speed: Not available\n";
                }
            }
            else
            {
                info += "GPU: Not found\n";
            }

            var ram = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.RAM);
            if (ram != null)
            {
                var totalMemory = 16 * 1024;
                var usedMemory = ram.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used Memory"));

                float usedMemoryValue = 0;

                if (usedMemory != null)
                    usedMemoryValue = usedMemory.Value.GetValueOrDefault();

                float availableMemory = ramCounter.NextValue();

                info += $"RAM Usage: {Math.Round((usedMemoryValue / totalMemory) * 100)} %\n";
                info += $"Used Memory: {Math.Round(usedMemoryValue * 1024)} MB\n";
                info += $"Total Memory: {totalMemory} MB\n";
                info += $"Available Memory: {Math.Round(availableMemory)} MB\n";
            }
            else
            {
                info += "RAM: Not found\n";
            }

            return info;
        }
    }
}
