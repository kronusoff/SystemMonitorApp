using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;

namespace HardwareMonitorApp
{
    public partial class MainWindow : Window
    {
        private Computer computer;
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHardwareMonitor();
            InitializeTimer();
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
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Обновление каждые 1 секунду
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var hardware in computer.Hardware)
            {
                hardware.Update(); // Обновить данные о железе
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            textBlockCpuInfo.Text = GetCpuInfo();
            textBlockGpuInfo.Text = GetGpuInfo();
            textBlockRamUsage.Text = GetRamUsage();
        }

        private string GetCpuInfo()
        {
            var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.CPU);
            if (cpu == null) return "CPU: Not found";

            string info = $"CPU: {cpu.Name}\n";

            // Получение нагрузки и температуры процессора
            var loadSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
            if (loadSensor != null)
            {
                info += $"CPU Total Usage: {Math.Round(loadSensor.Value.GetValueOrDefault())} %\n";
            }

            // Получение температуры процессора
            var temperatureSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);
            if (temperatureSensors.Any())
            {
                info += "CPU Temperatures:\n";
                foreach (var tempSensor in temperatureSensors)
                {
                    info += $"{tempSensor.Name}: {Math.Round(tempSensor.Value.GetValueOrDefault())} °C\n";
                }
            }
            else
            {
                info += "CPU Temperatures: Not available\n";
            }

            // Получение информации о ядрах
            info += "Core Usage:\n";
            foreach (var core in cpu.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core")))
            {
                info += $"{core.Name}: Usage: {Math.Round(core.Value.GetValueOrDefault())} %\n";
            }

            return info;
        }

        private string GetGpuInfo()
        {
            var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
            if (gpu == null) return "GPU: Not found";

            string info = $"GPU: {gpu.Name}\n";

            // Получение информации о загрузке, температуре и видеопамяти
            var loadSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
            var memoryUsedSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("GPU Memory Used"));
            var memoryLoadSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory"));
            var temperatureSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);

            if (loadSensor != null)
            {
                info += $"GPU Total Usage: {Math.Round(loadSensor.Value.GetValueOrDefault())} %\n";
            }

            // Вывод температур видеокарты
            if (temperatureSensors.Any())
            {
                info += "GPU Temperatures:\n";
                foreach (var tempSensor in temperatureSensors)
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

            if (memoryLoadSensor != null)
            {
                info += $"Memory Load: {Math.Round(memoryLoadSensor.Value.GetValueOrDefault())} %\n";
            }
            else
            {
                info += "Memory Load: Not available\n";
            }

            return info;
        }

        private string GetRamUsage()
        {
            var ram = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.RAM);
            if (ram == null) return "RAM: Not found";

            var totalMemory = ram.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory"));
            var usedMemory = ram.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used Memory"));

            float totalMemoryValue = totalMemory?.Value.GetValueOrDefault() ?? 0;
            float usedMemoryValue = usedMemory?.Value.GetValueOrDefault() ?? 0;

            // Исправлено вычисление процентного соотношения использованной оперативной памяти
            float ramUsagePercentage = (totalMemoryValue > 0) ? (usedMemoryValue / totalMemoryValue) * 100 : 0;

            return $"RAM Usage: {Math.Round(ramUsagePercentage)} %\nCurrent Usage: {Math.Round(usedMemoryValue)} MB\nTotal Memory: {Math.Round(totalMemoryValue)} MB";
        }
    }
}
