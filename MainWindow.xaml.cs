using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using SystemMonitorDesktop.Services;

namespace SystemMonitorDesktop;

public partial class MainWindow : Window
{
    private const int SparkCapacity = 60;

    private readonly HardwareService _hw = new();
    private readonly DispatcherTimer _timer = new();
    private readonly Queue<double> _ramHistory = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => _timer.Stop();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await Task.Run(LoadStaticInfo);

        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += OnTick;
        _timer.Start();
        await RefreshRealtime();
    }

    private void LoadStaticInfo()
    {
        var cpu   = _hw.GetCpu();
        var gpu   = _hw.GetGpu();
        var ram   = _hw.GetRamStatic();
        var os    = _hw.GetOs();
        var disks = _hw.GetDisks();

        Dispatcher.Invoke(() =>
        {
            CpuNameText.Text  = cpu.Name;
            CpuCoresText.Text = $"{cpu.Cores} nucleos / {cpu.Threads} hilos";
            CpuSpeedText.Text = cpu.MaxMHz > 0 ? $"{cpu.MaxMHz:N0} MHz" : "—";

            GpuNameText.Text = gpu.Name;
            GpuVramText.Text = gpu.VramMB > 0
                ? $"{gpu.VramMB / 1024.0:F0} GB  ({gpu.VramMB:N0} MB)"
                : "No disponible";

            RamTypeText.Text       = ram.Type;
            RamSpeedText.Text      = ram.SpeedMHz > 0 ? $"{ram.SpeedMHz:N0} MHz" : "—";
            RamTotalDetailText.Text = ram.TotalMB > 0
                ? $"{ram.TotalMB:N0} MB  ({ram.TotalMB / 1024.0:F1} GB)  — {ram.Slots} modulo(s)"
                : "—";

            OsNameText.Text  = os.Name;
            OsBuildText.Text = os.Build;
            OsArchText.Text  = os.Architecture;
            HostnameText.Text = Environment.MachineName;

            BuildDiskPanel(disks);
        });
    }

    private async void OnTick(object? sender, EventArgs e) => await RefreshRealtime();

    private async Task RefreshRealtime()
    {
        var (used, total, available) = await Task.Run(_hw.GetRamRealtime);
        var processes = await Task.Run(() => _hw.GetTopProcesses());
        var cpuUsage  = await Task.Run(_hw.GetCpuUsage);
        var net       = await Task.Run(_hw.GetNetworkSample);
        var battery   = _hw.GetBattery();
        var uptime    = _hw.GetUptime();

        double ramPercent = total > 0 ? Math.Round((double)used / total * 100, 1) : 0;
        var ramBrush = Brush(LoadColor(ramPercent));

        RamUsedText.Text      = $"{used / 1024.0:F1} GB";
        RamTotalText.Text     = $"{total / 1024.0:F1} GB";
        RamPercentText.Text   = $"{ramPercent:F1}%";
        RamPercentText.Foreground = ramBrush;
        RamBar.Value          = ramPercent;
        RamBar.Foreground     = ramBrush;
        RamAvailableText.Text = $"Disponible: {available / 1024.0:F1} GB";
        RamUsedMBText.Text    = $"Usado: {used:N0} MB";
        RamUpdateText.Text    = $"Actualizado: {DateTime.Now:HH:mm:ss}";

        var cpuBrush = Brush(LoadColor(cpuUsage));
        CpuUsageText.Text = $"{cpuUsage:F1}%";
        CpuUsageText.Foreground = cpuBrush;
        CpuBar.Value = cpuUsage;
        CpuBar.Foreground = cpuBrush;

        NetDownText.Text = FormatSpeed(net.DownKbps);
        NetUpText.Text   = FormatSpeed(net.UpKbps);

        if (battery.Present)
        {
            BatteryCard.Visibility = Visibility.Visible;
            var batBrush = Brush(battery.Percent > 40 ? "#3fb950"
                               : battery.Percent > 20 ? "#d29922" : "#f85149");
            BatteryPercentText.Text = $"{battery.Percent}%";
            BatteryPercentText.Foreground = batBrush;
            BatteryBar.Value = battery.Percent;
            BatteryBar.Foreground = batBrush;
            BatteryStatusText.Text = battery.Status;
        }
        else
        {
            BatteryCard.Visibility = Visibility.Collapsed;
        }

        UptimeText.Text = FormatUptime(uptime);
        ClockText.Text = DateTime.Now.ToString("dddd d 'de' MMMM yyyy   HH:mm:ss",
            new CultureInfo("es-ES"));

        UpdateSparkline(ramPercent, LoadColor(ramPercent));
        BuildProcessRows(processes);

        StatusBarText.Text = $"Ultima actualizacion: {DateTime.Now:HH:mm:ss}";
    }

    private static string LoadColor(double pct) =>
        pct < 70 ? "#3fb950" : pct < 85 ? "#d29922" : "#f85149";

    private static string FormatSpeed(double kbps)
    {
        if (kbps < 1000) return $"{kbps:F0} Kbps";
        return $"{kbps / 1000.0:F2} Mbps";
    }

    private static string FormatUptime(TimeSpan t)
    {
        if (t.TotalDays >= 1) return $"Uptime {(int)t.TotalDays}d {t.Hours:D2}h {t.Minutes:D2}m";
        if (t.TotalHours >= 1) return $"Uptime {t.Hours:D2}h {t.Minutes:D2}m";
        return $"Uptime {t.Minutes:D2}m {t.Seconds:D2}s";
    }

    private void UpdateSparkline(double value, string colorHex)
    {
        _ramHistory.Enqueue(value);
        while (_ramHistory.Count > SparkCapacity) _ramHistory.Dequeue();

        RamSparkCanvas.Children.Clear();
        var w = RamSparkCanvas.ActualWidth;
        var h = RamSparkCanvas.ActualHeight;
        if (w <= 0 || h <= 0 || _ramHistory.Count < 2) return;

        var pts = new PointCollection();
        var samples = _ramHistory.ToArray();
        var step = w / (SparkCapacity - 1);
        var startX = w - step * (samples.Length - 1);

        for (int i = 0; i < samples.Length; i++)
        {
            var x = startX + step * i;
            var y = h - (samples[i] / 100.0 * h);
            pts.Add(new Point(x, y));
        }

        var stroke = Brush(colorHex);
        var line = new Polyline
        {
            Points = pts,
            Stroke = stroke,
            StrokeThickness = 1.6,
            StrokeLineJoin = PenLineJoin.Round
        };

        var fillPts = new PointCollection(pts) { new Point(pts[^1].X, h), new Point(pts[0].X, h) };
        var fill = new Polygon
        {
            Points = fillPts,
            Fill = new SolidColorBrush(((SolidColorBrush)stroke).Color) { Opacity = 0.15 }
        };

        RamSparkCanvas.Children.Add(fill);
        RamSparkCanvas.Children.Add(line);
    }

    private void BuildProcessRows(List<(string Name, int Pid, long MemoryMB)> processes)
    {
        ProcessPanel.Children.Clear();
        foreach (var (name, pid, memMB) in processes)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var nameBlock = new TextBlock
            {
                Text = name,
                FontSize = 12,
                Foreground = Brush("#e6edf3"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            var pidBlock = new TextBlock
            {
                Text = pid.ToString(),
                FontSize = 12,
                Foreground = Brush("#8b949e"),
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var memBlock = new TextBlock
            {
                Text = $"{memMB:N0} MB",
                FontSize = 12,
                Foreground = Brush("#58a6ff"),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var killBtn = new Button
            {
                Content = "finalizar",
                Style = (Style)FindResource("KillBtn"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Tag = pid,
                ToolTip = $"Finalizar proceso {name} (PID {pid})",
                Cursor = Cursors.Hand
            };
            killBtn.Click += KillProcess_Click;

            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(pidBlock,  1);
            Grid.SetColumn(memBlock,  2);
            Grid.SetColumn(killBtn,   3);
            row.Children.Add(nameBlock);
            row.Children.Add(pidBlock);
            row.Children.Add(memBlock);
            row.Children.Add(killBtn);
            ProcessPanel.Children.Add(row);
        }
    }

    private void BuildDiskPanel(List<DiskInfo> disks)
    {
        DiskPanel.Children.Clear();

        if (disks.Count == 0)
        {
            DiskPanel.Children.Add(new TextBlock
            {
                Text = "No se encontraron discos.",
                FontSize = 12,
                Foreground = Brush("#8b949e")
            });
            return;
        }

        foreach (var disk in disks)
        {
            var color = LoadColor(disk.UsedPercent);

            var label = string.IsNullOrWhiteSpace(disk.Label)
                ? disk.Letter
                : $"{disk.Letter}  ({disk.Label})";

            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

            var header = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brush("#e6edf3")
            };
            var spaceText = new TextBlock
            {
                Text = $"{disk.UsedGB} GB / {disk.TotalGB} GB  ({disk.UsedPercent:F0}%)",
                FontSize = 12,
                Foreground = Brush(color),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(labelText, 0);
            Grid.SetColumn(spaceText, 1);
            header.Children.Add(labelText);
            header.Children.Add(spaceText);

            var bar = new ProgressBar
            {
                Style = (Style)FindResource("FlatBar"),
                Maximum = 100,
                Value = disk.UsedPercent,
                Foreground = Brush(color)
            };

            container.Children.Add(header);
            container.Children.Add(bar);
            DiskPanel.Children.Add(container);
        }
    }

    private async void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        CleanButton.IsEnabled = false;
        CleanResultText.Text = "Limpiando archivos temporales...";
        CleanResultText.Foreground = Brush("#8b949e");

        var (freedMB, message) = await Task.Run(_hw.CleanTempFiles);

        CleanResultText.Text = message;
        CleanResultText.Foreground = Brush(freedMB > 0 ? "#3fb950" : "#8b949e");
        CleanButton.IsEnabled = true;
    }

    private void GcButton_Click(object sender, RoutedEventArgs e)
    {
        GcButton.IsEnabled = false;
        var before = GC.GetTotalMemory(false);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        var after    = GC.GetTotalMemory(true);
        var freedKB  = (before - after) / 1024;

        CleanResultText.Text = freedKB > 0
            ? $"GC ejecutado — memoria .NET liberada: {freedKB:N0} KB"
            : "GC ejecutado — no habia memoria .NET que liberar.";
        CleanResultText.Foreground = Brush("#3fb950");
        GcButton.IsEnabled = true;
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"system-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            Filter = "Archivo de texto (*.txt)|*.txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };
        if (dlg.ShowDialog() != true) return;

        ExportButton.IsEnabled = false;
        CleanResultText.Foreground = Brush("#8b949e");
        CleanResultText.Text = "Generando informe...";

        try
        {
            var path = await Task.Run(() => _hw.ExportReport(dlg.FileName));
            CleanResultText.Text = $"Informe guardado en: {path}";
            CleanResultText.Foreground = Brush("#3fb950");
        }
        catch (Exception ex)
        {
            CleanResultText.Text = $"Error al guardar el informe: {ex.Message}";
            CleanResultText.Foreground = Brush("#f85149");
        }
        finally { ExportButton.IsEnabled = true; }
    }

    private void KillProcess_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int pid) return;

        var confirm = MessageBox.Show(
            $"¿Finalizar el proceso con PID {pid}?\n\nPuede causar perdida de datos si la aplicacion tenia trabajo sin guardar.",
            "Finalizar proceso",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        var (ok, msg) = _hw.KillProcess(pid);
        CleanResultText.Text = msg;
        CleanResultText.Foreground = Brush(ok ? "#3fb950" : "#f85149");
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex)!);
}
