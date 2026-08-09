using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using SystemMonitorDesktop.Controls;
using SystemMonitorDesktop.Services;
using SystemMonitorDesktop.Views;

namespace SystemMonitorDesktop;

public partial class MainWindow : Window
{
    private static readonly CultureInfo Spanish = new("es-ES");

    /// <summary>
    /// Las vistas se crean la primera vez que se visitan y se conservan: así el
    /// arranque es inmediato y el historial de los gráficos no se pierde al
    /// cambiar de sección.
    /// </summary>
    private readonly Dictionary<string, UserControl> _pages = new();

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Navigate(NavOverview);

        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();

        AppServices.Monitor.Sampled += OnSampled;
        AppServices.Monitor.Start();
        await AppServices.Monitor.SampleAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        AppServices.Monitor.Sampled -= OnSampled;
        AppServices.Monitor.Stop();
        _clock.Stop();
    }

    // ────────────────────────── Navegación ──────────────────────────

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        if (sender is RadioButton button) Navigate(button);
    }

    private void Navigate(RadioButton button)
    {
        var key = button.Name;
        if (!_pages.TryGetValue(key, out var page))
        {
            page = CreatePage(key);
            _pages[key] = page;
        }
        PageHost.Content = page;
    }

    /// <summary>La clave es el x:Name del botón de navegación que la abre.</summary>
    private static UserControl CreatePage(string key) => key switch
    {
        "NavMemory" => new MemoryView(),
        "NavStorage" => new StorageView(),
        "NavProcesses" => new ProcessesView(),
        "NavEvidence" => new EvidenceView(),
        "NavTools" => new ToolsView(),
        _ => new OverviewView()
    };

    // ────────────────────────── Barra superior ──────────────────────────

    private void OnSampled(RealtimeSnapshot s)
    {
        CpuPill.Text = $"{s.CpuPercent:0.0} %";
        CpuPill.Foreground = UiKit.LoadBrush(s.CpuPercent);

        RamPill.Text = $"{s.RamPercent:0.0} %";
        RamPill.Foreground = UiKit.LoadBrush(s.RamPercent);

        FooterStatus.Text = $"Actualizado a las {s.TakenAt:HH:mm:ss}";
    }

    private void UpdateClock() =>
        ClockText.Text = DateTime.Now.ToString("ddd d MMM · HH:mm:ss", Spanish);

    // ────────────────────────── Chrome de la ventana ──────────────────────────

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmcpRound = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Al renunciar al marco nativo también se pierden las esquinas
        // redondeadas de Windows 11. Se piden de vuelta a DWM; en Windows 10
        // la llamada devuelve error y la ventana queda cuadrada, sin más.
        try
        {
            var preference = DwmcpRound;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle,
                DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
        catch { }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var maximized = WindowState == WindowState.Maximized;

        // Sin WindowStyle nativo, una ventana maximizada se dibuja unos píxeles
        // por fuera del área de trabajo. El margen lo compensa.
        RootBorder.Margin = maximized ? new Thickness(7) : new Thickness(0);
        RootBorder.BorderThickness = maximized ? new Thickness(0) : new Thickness(1);

        // Segoe MDL2 Assets: E923 restaurar, E922 maximizar.
        MaximizeButton.Content = maximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
    }
}
