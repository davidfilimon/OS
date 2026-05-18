using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using OS.Core;
using Avalonia.Threading;
using ReactiveUI;
using System.Reactive;

namespace OS.ViewModels;

public record GanttSegment(double X, double Y, double Width, string Color, string Label);

public class MainWindowViewModel : ReactiveObject
{
    // Instanța simulatorului nostru
    private Scheduler? _scheduler;

    private double _currentRamUsage;
    public double CurrentRamUsage
    {
        get => _currentRamUsage;
        set => this.RaiseAndSetIfChanged(ref _currentRamUsage, value);
    }

    private double _maxRam = 100;
    public double MaxRam
    {
        get => _maxRam;
        set => this.RaiseAndSetIfChanged(ref _maxRam, value);
    }

    private double _cpuLoad;
    public double CpuLoad
    {
        get => _cpuLoad;
        set => this.RaiseAndSetIfChanged(ref _cpuLoad, value);
    }

    private DispatcherTimer? _timer;
    private bool _isRunning;

    public ObservableCollection<GanttSegment> GanttSegments { get; } = new();

    // Proprietăți pentru UI
    public ObservableCollection<SimulationEvent> DisplayEvents { get; } = new();

    private int _currentTime;
    public int CurrentTime
    {
        get => _currentTime;
        set { _currentTime = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Așteptare încărcare fișier...";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public MainWindowViewModel()
    {
        // Inițializare demonstrativă sau lăsăm utilizatorul să încarce fișierul
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        // Exemplu de date: Procesul 1 vine la t=0, are nevoie de 10 RAM
        var activities = new Queue<Activity>();
        activities.Enqueue(new Activity { Type = ActivityType.Execution, Duration = 5 });
        activities.Enqueue(new Activity { Type = ActivityType.SysCall, Duration = 2 });
        activities.Enqueue(new Activity { Type = ActivityType.Execution, Duration = 3 });

        var p1 = new Process(1, 0, 10, activities);
        var processList = new List<Process> { p1 };

        // Cream Scheduler-ul (2 CPU, 100 RAM, 0.5 disc rate, 5 time slice, 10 system period)
        _scheduler = new Scheduler(2, 100, 0.5, 5, 10, processList);
        StatusMessage = "Simulator pregătit.";
    }

    public void RunNextTick()
    {
        if (_scheduler == null) return;

        _scheduler.Ticks();

        // 1. Accesăm obiectul MemoryManager
        var mem = _scheduler.TotalMemory;

        // 2. Actualizăm MaxRam
        MaxRam = mem.TotalMemory;

        // 3. Calculăm RAM-ul utilizat
        // Verifică dacă p.MemoryRequired din MemoryManager are o valoare mai mare de 0!
        int used = 0;
        foreach (var p in mem.RamProcesses)
        {
            used += p.MemoryRequired;
        }

        // Setăm valoarea pentru UI
        CurrentRamUsage = used;

        // 4. ACTUALIZARE CPU
        double totalCpus = _scheduler.Processors.Count;
        double busyCpus = _scheduler.Processors.Count(p => !p.IsFree);
        CpuLoad = totalCpus > 0 ? (busyCpus / totalCpus) * 100 : 0;

        // 5. GANTT & LOGS
        var currentEvents = _scheduler.Events.Where(e => e.Time == CurrentTime).ToList();
        foreach (var ev in currentEvents)
        {
            if (ev.Action == "EXECUTING" && ev.ProcessorId.HasValue)
            {
                GanttSegments.Add(new GanttSegment(
                    X: CurrentTime * 20,
                    Y: (ev.ProcessorId ?? 0) * 40,
                    Width: 20,
                    Color: GetColorForPid(ev.ProcessId),
                    Label: $"PID: {ev.ProcessId}"
                ));
            }
            DisplayEvents.Insert(0, ev);
        }

        CurrentTime++;
        StatusMessage = $"Simulare în desfășurare. Timp: {CurrentTime}";

        if (_scheduler.IsFinished())
        {
            _timer?.Stop();
            IsRunning = false;
            StatusMessage = "Simulare finalizată!";
            CpuLoad = 0;
        }
    }

    // Resetarea simulării
    public void ResetSimulation()
    {
        _timer?.Stop();
        IsRunning = false;

        // 2. Resetăm logica din Scheduler (dacă ai metoda Reset acolo)
        _scheduler?.Reset();

        // 3. IMPORTANT: Golim colecția de segmente grafice
        GanttSegments.Clear();

        // 4. Golim lista de log-uri (evenimente)
        DisplayEvents.Clear();

        // 5. Resetăm timpul curent
        CurrentTime = 0;

        StatusMessage = "Simulare resetată. Gata pentru un nou start!";
    }

    #region PropertyChanged Setup
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    #endregion

    public void LoadConfiguration(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            // Curățăm spațiile multiple sau tab-urile și transformăm în numere
            int[] data = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse)
                                .ToArray();

            if (data.Length < 5) return;

            int idx = 0;
            int numProcessors = data[idx++];
            int ramSize = data[idx++];
            double diskRate = data[idx++] / 10.0; // Presupunem că rata e dată ca întreg (ex: 5 pentru 0.5)
            int timeSlice = data[idx++];
            int sysPeriod = data[idx++];

            List<Process> processes = new();

            while (idx < data.Length)
            {
                int pid = data[idx++];
                int releaseTime = data[idx++];
                int memReq = data[idx++];
                int numActivities = data[idx++];

                Queue<Activity> activities = new();
                for (int j = 0; j < numActivities; j++)
                {
                    int typeInt = data[idx++];
                    int duration = data[idx++];

                    activities.Enqueue(new Activity
                    {
                        Type = typeInt == 1 ? ActivityType.Execution : ActivityType.SysCall,
                        Duration = duration
                    });
                }

                processes.Add(new Process(pid, memReq, releaseTime, activities));
            }

            // Reinițializăm Scheduler-ul cu datele noi
            _scheduler = new Scheduler(numProcessors, ramSize, diskRate, timeSlice, sysPeriod, processes);

            DisplayEvents.Clear();
            CurrentTime = 0;
            StatusMessage = $"Configurație încărcată: {processes.Count} procese.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Eroare la citire: " + ex.Message;
        }
    }

    public async void OpenFileCommand()
    {
        // Obținem referința către fereastra principală pentru a afișa dialogul
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                       ? Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)
                       : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Selectează fișierul de configurare",
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            LoadConfiguration(files[0].Path.LocalPath);
        }
    }

    public void ExportLog()
    {
        if (_scheduler == null || !_scheduler.Events.Any()) return;

        var lines = _scheduler.Events.Select(e =>
            $"Time: {e.Time} | PID: {e.ProcessId} | CPU: {e.ProcessorId} | Action: {e.Action} | Dur: {e.Duration}");

        File.WriteAllLines("simulation_log.txt", lines);
        StatusMessage = "Log exportat în simulation_log.txt";
    }

    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); }
    }

    public void ToggleSimulation()
    {
        if (_timer == null)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200); // Viteza simulării
            _timer.Tick += (s, e) => RunNextTick();
        }

        if (IsRunning)
        {
            _timer.Stop();
            IsRunning = false;
            StatusMessage = "Simulare întreruptă.";
        }
        else
        {
            _timer.Start();
            IsRunning = true;
            StatusMessage = "Simulare în desfășurare...";
        }
    }

    private string GetColorForPid(int? pid) => pid switch
    {
        1 => "#E74C3C", // Roșu
        2 => "#3498DB", // Albastru
        3 => "#2ECC71", // Verde
        _ => "#95A5A6"  // Gri
    };
}
