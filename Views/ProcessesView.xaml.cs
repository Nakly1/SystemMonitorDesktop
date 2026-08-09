using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SystemMonitorDesktop.Controls;
using SystemMonitorDesktop.Services;

namespace SystemMonitorDesktop.Views;

public partial class ProcessesView : UserControl
{
    /// <summary>Controles vivos de cada fila, indexados por PID.</summary>
    private sealed record Row(TextBlock Memory, ProgressBar Share);

    private readonly Dictionary<int, Row> _rows = new();
    private List<int> _currentPids = new();
    private bool _hooked;

    public ProcessesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hooked) return;
        _hooked = true;

        AppServices.Monitor.Sampled += Apply;
        if (AppServices.Monitor.Latest is { } latest) Apply(latest);

        Unloaded += (_, _) => AppServices.Monitor.Sampled -= Apply;
    }

    private void Apply(RealtimeSnapshot s)
    {
        var processes = s.TopProcesses;

        // Reconstruir el panel entero cada dos segundos haría imposible pulsar
        // «finalizar»: el botón desaparecería bajo el cursor. Sólo se rehace la
        // lista cuando cambia el conjunto de procesos.
        var pids = processes.Select(p => p.Pid).ToList();
        if (!pids.SequenceEqual(_currentPids))
        {
            Rebuild(processes);
            _currentPids = pids;
        }

        var top = processes.Count > 0 ? Math.Max(1, processes[0].MemoryMB) : 1;
        foreach (var p in processes)
        {
            if (!_rows.TryGetValue(p.Pid, out var row)) continue;
            row.Memory.Text = UiKit.FormatBytes(p.MemoryMB);
            row.Share.Value = Math.Min(100, p.MemoryMB * 100.0 / top);
        }
    }

    private void Rebuild(IReadOnlyList<ProcessRow> processes)
    {
        RowsPanel.Children.Clear();
        _rows.Clear();

        if (processes.Count == 0)
        {
            RowsPanel.Children.Add(UiKit.EmptyState("No se pudo leer la lista de procesos."));
            return;
        }

        foreach (var p in processes)
        {
            var container = new Grid { Margin = new Thickness(0, 0, 6, 12) };
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            var name = new TextBlock
            {
                Text = p.Name,
                Style = UiKit.Style("T.Value"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 12, 0),
                ToolTip = p.Name
            };

            var pid = new TextBlock
            {
                Text = p.Pid.ToString(),
                Style = UiKit.Style("T.Mono"),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Memoria: cifra y, debajo, una barra con el peso relativo al mayor.
            var memoryStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            var memory = new TextBlock
            {
                Text = UiKit.FormatBytes(p.MemoryMB),
                Style = UiKit.Style("T.Value"),
                FontSize = 13
            };
            var share = new ProgressBar
            {
                Style = UiKit.Style("Meter"),
                Height = 3,
                Maximum = 100,
                Value = 0,
                Foreground = UiKit.Brush("Br.AccentDeep"),
                Margin = new Thickness(0, 5, 0, 0)
            };
            memoryStack.Children.Add(memory);
            memoryStack.Children.Add(share);

            var kill = new Button
            {
                Content = "Finalizar",
                Style = UiKit.Style("BtnDanger"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = p,
                Cursor = Cursors.Hand,
                ToolTip = $"Cerrar «{p.Name}» y todos sus procesos hijos"
            };
            kill.Click += Kill_Click;

            Grid.SetColumn(name, 0);
            Grid.SetColumn(pid, 1);
            Grid.SetColumn(memoryStack, 2);
            Grid.SetColumn(kill, 3);
            container.Children.Add(name);
            container.Children.Add(pid);
            container.Children.Add(memoryStack);
            container.Children.Add(kill);

            RowsPanel.Children.Add(container);
            _rows[p.Pid] = new Row(memory, share);
        }
    }

    private void Kill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProcessRow process }) return;

        var confirm = MessageBox.Show(
            $"Se cerrará «{process.Name}» (PID {process.Pid}) y sus procesos hijos.\n\n" +
            "Si la aplicación tiene trabajo sin guardar, se perderá.",
            "Finalizar proceso",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (confirm != MessageBoxResult.OK) return;

        var (ok, message) = AppServices.Hardware.KillProcess(process.Pid);
        StatusText.Text = message;
        StatusText.Foreground = UiKit.Brush(ok ? "Br.Positive" : "Br.Critical");
    }
}
