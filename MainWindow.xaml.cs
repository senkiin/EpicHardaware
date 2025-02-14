using System;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;
using System.IO;

namespace SystemMonitorApp
{
    public partial class MainWindow : Window
    {
        private readonly Computer _computer;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializar LibreHardwareMonitor para obtener datos del hardware
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsBatteryEnabled = true,
                IsStorageEnabled = true,
                IsMotherboardEnabled = true, 
                IsControllerEnabled = true
            };
            _computer.Open();

            // Iniciar actualización periódica cada 2 segundos
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += UpdateSystemMetrics;
            timer.Start();
        }

        private void UpdateSystemMetrics(object sender, EventArgs e)
        {
            UpdateTemperature();
            UpdateBatteryStatus();
            UpdateMicrophoneCameraStatus();
            UpdateStorageStatus();
            UpdateFanSpeedAndCpuLoad();
        }

        private void UpdateTemperature()
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                hardware.Update(); // Asegurarse de actualizar los sensores

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        // Detecta temperatura del CPU (Intel y AMD Ryzen)
                        if (hardware.HardwareType == HardwareType.Cpu)
                        {
                            // Algunos Ryzen muestran "Package", otros "Core (Tctl/Tdie)"
                            if (sensor.Name.Contains("Package") || sensor.Name.Contains("Core (Tctl/Tdie)"))
                            {
                                CpuTempText.Text = $"{sensor.Value?.ToString("0.0")} °C";
                            }
                        }

                        // Detecta temperatura de la GPU (AMD o NVIDIA)
                        else if (hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuNvidia)
                        {
                            GpuTempText.Text = $"{sensor.Value?.ToString("0.0")} °C";
                        }
                    }
                }
            }
        }


        private void UpdateBatteryStatus()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
            foreach (ManagementObject obj in searcher.Get())
            {
                BatteryStatusText.Text = $"Charge: {obj["EstimatedChargeRemaining"]}%";
            }
        }

        private void UpdateMicrophoneCameraStatus()
        {
            bool micActive = System.Diagnostics.Process.GetProcesses()
                .Any(p => p.ProcessName.ToLower().Contains("audiodg"));

            bool camActive = System.Diagnostics.Process.GetProcesses()
                .Any(p => p.ProcessName.ToLower().Contains("camera"));

            MicStatusText.Text = micActive ? "Active" : "Inactive";
            CamStatusText.Text = camActive ? "Active" : "Inactive";
        }

        private void UpdateStorageStatus()
        {
            DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (drive != null)
            {
                StorageText.Text = $"Free: {drive.AvailableFreeSpace / (1024 * 1024 * 1024)} GB";
            }
        }

        private void UpdateFanSpeedAndCpuLoad()
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                hardware.Update();
                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Fan)
                        FanSpeedText.Text = $"{sensor.Value?.ToString("0")} RPM";
                    if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU"))
                        CpuLoadText.Text = $"{sensor.Value?.ToString("0.0")} %";
                }
            }
        }
    }
}
