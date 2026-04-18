using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace SystemMonitorDesktop.Services;

public record CpuInfo(string Name, int Cores, int Threads, int MaxMHz);
public record GpuInfo(string Name, long VramMB);
public record RamStaticInfo(long TotalMB, string Type, int SpeedMHz, int Slots);
public record OsInfo(string Name, string Build, string Architecture);
public record NetSample(double DownKbps, double UpKbps);
public record BatteryInfo(bool Present, int Percent, bool OnAc, string Status);

public record DiskInfo(string Letter, string Label, long TotalGB, long FreeGB)
{
    public long UsedGB => TotalGB - FreeGB;
    public double UsedPercent => TotalGB > 0 ? Math.Round((double)UsedGB / TotalGB * 100, 1) : 0;
}

public class HardwareService
{
    private long _cachedTotalMB;

    private readonly PerformanceCounter _cpuCounter =
        new("Processor", "% Processor Time", "_Total", readOnly: true);
    private bool _cpuWarmedUp;

    private long _prevNetBytesRecv;
    private long _prevNetBytesSent;
    private DateTime _prevNetTime = DateTime.MinValue;

    public CpuInfo GetCpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return new CpuInfo(
                    Name: obj["Name"]?.ToString()?.Trim() ?? "Desconocido",
                    Cores: Convert.ToInt32(obj["NumberOfCores"] ?? 0),
                    Threads: Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0),
                    MaxMHz: Convert.ToInt32(obj["MaxClockSpeed"] ?? 0)
                );
            }
        }
        catch { }
        return new CpuInfo("No disponible", 0, 0, 0);
    }

    public double GetCpuUsage()
    {
        try
        {
            if (!_cpuWarmedUp)
            {
                _cpuCounter.NextValue();
                _cpuWarmedUp = true;
                return 0;
            }
            return Math.Round(_cpuCounter.NextValue(), 1);
        }
        catch { return 0; }
    }

    public GpuInfo GetGpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterRAM FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(name)
                    || name.Contains("Remote Display", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Microsoft Display", StringComparison.OrdinalIgnoreCase))
                    continue;

                long vramMB = 0;
                if (obj["AdapterRAM"] != null)
                {
                    // AdapterRAM is uint32 — overflows at 4 GB on WMI
                    var raw = Convert.ToUInt32(obj["AdapterRAM"]);
                    vramMB = raw / (1024 * 1024);
                }

                if (vramMB == 0 || vramMB >= 4094)
                {
                    var regVram = GetVramFromRegistry();
                    if (regVram > 0) vramMB = regVram;
                }

                return new GpuInfo(name, vramMB);
            }
        }
        catch { }
        return new GpuInfo("No disponible", 0);
    }

    private long GetVramFromRegistry()
    {
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (baseKey == null) return 0;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                if (!int.TryParse(subKeyName, out _)) continue;
                using var subKey = baseKey.OpenSubKey(subKeyName);
                var val = subKey?.GetValue("HardwareInformation.qwMemorySize");
                if (val is byte[] bytes && bytes.Length == 8)
                    return BitConverter.ToInt64(bytes, 0) / (1024 * 1024);
                if (val is long lv && lv > 0)
                    return lv / (1024 * 1024);
            }
        }
        catch { }
        return 0;
    }

    public RamStaticInfo GetRamStatic()
    {
        long totalMB = 0;
        int speedMHz = 0;
        int slots = 0;
        string type = "DDR4";

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Capacity, Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                slots++;
                totalMB += Convert.ToInt64(obj["Capacity"] ?? 0L) / (1024 * 1024);
                if (speedMHz == 0 && obj["Speed"] != null)
                    speedMHz = Convert.ToInt32(obj["Speed"]);
                if (obj["SMBIOSMemoryType"] != null)
                    type = Convert.ToInt32(obj["SMBIOSMemoryType"]) switch
                    {
                        34 => "DDR5",
                        26 => "DDR4",
                        24 => "DDR3",
                        20 => "DDR2",
                        _ => type
                    };
            }
        }
        catch { }

        _cachedTotalMB = totalMB;
        return new RamStaticInfo(totalMB, type, speedMHz, slots);
    }

    public (long UsedMB, long TotalMB, long AvailableMB) GetRamRealtime()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                long freeKB  = Convert.ToInt64(obj["FreePhysicalMemory"]  ?? 0L);
                long totalKB = Convert.ToInt64(obj["TotalVisibleMemorySize"] ?? 0L);
                long freeMB  = freeKB  / 1024;
                long totalMB = totalKB / 1024;
                if (_cachedTotalMB == 0) _cachedTotalMB = totalMB;
                return (totalMB - freeMB, totalMB, freeMB);
            }
        }
        catch { }
        return (_cachedTotalMB, _cachedTotalMB, 0);
    }

    public OsInfo GetOs()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Caption, BuildNumber, OSArchitecture FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return new OsInfo(
                    Name: obj["Caption"]?.ToString()?.Trim() ?? "Windows",
                    Build: obj["BuildNumber"]?.ToString() ?? "?",
                    Architecture: obj["OSArchitecture"]?.ToString() ?? "64-bit"
                );
            }
        }
        catch { }
        return new OsInfo("Windows", "?", "64-bit");
    }

    public TimeSpan GetUptime() => TimeSpan.FromMilliseconds(Environment.TickCount64);

    public List<DiskInfo> GetDisks()
    {
        var disks = new List<DiskInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, VolumeName, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3");
            foreach (ManagementObject obj in searcher.Get())
            {
                var letter = obj["DeviceID"]?.ToString() ?? "?";
                var label  = obj["VolumeName"]?.ToString() ?? "";
                var total  = Convert.ToInt64(obj["Size"]      ?? 0L) / (1024L * 1024 * 1024);
                var free   = Convert.ToInt64(obj["FreeSpace"] ?? 0L) / (1024L * 1024 * 1024);
                if (total > 0)
                    disks.Add(new DiskInfo(letter, label, total, free));
            }
        }
        catch { }
        return disks;
    }

    public List<(string Name, int Pid, long MemoryMB)> GetTopProcesses(int count = 10)
    {
        try
        {
            return Process.GetProcesses()
                .Select(p =>
                {
                    try { return (p.ProcessName, p.Id, p.WorkingSet64 / (1024 * 1024)); }
                    catch { return (p.ProcessName, p.Id, 0L); }
                })
                .OrderByDescending(p => p.Item3)
                .Take(count)
                .ToList();
        }
        catch { return new(); }
    }

    public (bool Ok, string Message) KillProcess(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            var name = p.ProcessName;
            p.Kill(entireProcessTree: true);
            p.WaitForExit(2000);
            return (true, $"Proceso '{name}' (PID {pid}) finalizado.");
        }
        catch (ArgumentException)
        {
            return (false, $"El proceso (PID {pid}) ya no existe.");
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo finalizar (PID {pid}): {ex.Message}");
        }
    }

    public NetSample GetNetworkSample()
    {
        try
        {
            long bytesRecv = 0, bytesSent = 0;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var stats = ni.GetIPStatistics();
                bytesRecv += stats.BytesReceived;
                bytesSent += stats.BytesSent;
            }

            var now = DateTime.UtcNow;
            if (_prevNetTime == DateTime.MinValue)
            {
                _prevNetBytesRecv = bytesRecv;
                _prevNetBytesSent = bytesSent;
                _prevNetTime = now;
                return new NetSample(0, 0);
            }

            var seconds = (now - _prevNetTime).TotalSeconds;
            if (seconds <= 0) return new NetSample(0, 0);

            var down = (bytesRecv - _prevNetBytesRecv) * 8.0 / 1000.0 / seconds;
            var up   = (bytesSent - _prevNetBytesSent) * 8.0 / 1000.0 / seconds;

            _prevNetBytesRecv = bytesRecv;
            _prevNetBytesSent = bytesSent;
            _prevNetTime = now;

            return new NetSample(Math.Max(0, down), Math.Max(0, up));
        }
        catch { return new NetSample(0, 0); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int  BatteryLifeTime;
        public int  BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    public BatteryInfo GetBattery()
    {
        try
        {
            if (!GetSystemPowerStatus(out var s))
                return new BatteryInfo(false, 0, false, "—");

            // BatteryFlag 128 = no battery present
            if ((s.BatteryFlag & 128) != 0)
                return new BatteryInfo(false, 0, s.ACLineStatus == 1, "Sin bateria");

            var onAc = s.ACLineStatus == 1;
            var percent = s.BatteryLifePercent == 255 ? 0 : s.BatteryLifePercent;
            string status = onAc ? "Cargando" : "Descargando";
            if ((s.BatteryFlag & 8) != 0) status = "Cargando";
            return new BatteryInfo(true, percent, onAc, status);
        }
        catch
        {
            return new BatteryInfo(false, 0, false, "—");
        }
    }

    public (long FreedMB, string Message) CleanTempFiles()
    {
        long freedBytes = 0;
        var paths = new[]
        {
            Path.GetTempPath(),
            @"C:\Windows\Temp"
        };

        foreach (var dir in paths)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if ((DateTime.UtcNow - info.LastWriteTimeUtc).TotalHours >= 1)
                        {
                            freedBytes += info.Length;
                            File.Delete(file);
                        }
                    }
                    catch { /* archivo en uso o sin permisos */ }
                }
            }
            catch { }
        }

        long freedMB = freedBytes / (1024 * 1024);
        string msg = freedMB > 0
            ? $"Limpieza completada — se liberaron {freedMB} MB."
            : "No habia archivos temporales que limpiar (o estaban en uso).";
        return (freedMB, msg);
    }

    public string ExportReport(string path)
    {
        var cpu = GetCpu();
        var gpu = GetGpu();
        var ram = GetRamStatic();
        var os  = GetOs();
        var disks = GetDisks();
        var (usedMB, totalMB, freeMB) = GetRamRealtime();
        var uptime = GetUptime();
        var bat = GetBattery();

        var sb = new StringBuilder();
        sb.AppendLine("=== SYSTEM MONITOR — INFORME ===");
        sb.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Equipo:   {Environment.MachineName}");
        sb.AppendLine($"Usuario:  {Environment.UserName}");
        sb.AppendLine($"Uptime:   {(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m");
        sb.AppendLine();
        sb.AppendLine("[CPU]");
        sb.AppendLine($"  {cpu.Name}");
        sb.AppendLine($"  {cpu.Cores} nucleos / {cpu.Threads} hilos @ {cpu.MaxMHz} MHz");
        sb.AppendLine();
        sb.AppendLine("[GPU]");
        sb.AppendLine($"  {gpu.Name}");
        sb.AppendLine($"  VRAM: {(gpu.VramMB > 0 ? $"{gpu.VramMB / 1024.0:F0} GB" : "?")}");
        sb.AppendLine();
        sb.AppendLine("[RAM]");
        sb.AppendLine($"  Total:      {ram.TotalMB} MB ({ram.TotalMB / 1024.0:F1} GB)");
        sb.AppendLine($"  Tipo:       {ram.Type} @ {ram.SpeedMHz} MHz");
        sb.AppendLine($"  Modulos:    {ram.Slots}");
        sb.AppendLine($"  En uso:     {usedMB} MB ({(totalMB > 0 ? usedMB * 100.0 / totalMB : 0):F1} %)");
        sb.AppendLine($"  Disponible: {freeMB} MB");
        sb.AppendLine();
        sb.AppendLine("[SO]");
        sb.AppendLine($"  {os.Name}");
        sb.AppendLine($"  Build {os.Build}  —  {os.Architecture}");
        sb.AppendLine();
        sb.AppendLine("[BATERIA]");
        sb.AppendLine(bat.Present
            ? $"  {bat.Percent}%  —  {bat.Status}"
            : "  No presente (equipo de escritorio)");
        sb.AppendLine();
        sb.AppendLine("[DISCOS]");
        foreach (var d in disks)
            sb.AppendLine($"  {d.Letter} {d.Label,-14}  {d.UsedGB,5} / {d.TotalGB,5} GB  ({d.UsedPercent:F1}%)");

        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
