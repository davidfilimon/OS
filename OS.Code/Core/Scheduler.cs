using System.Collections.Generic;
using System.Linq;
using System;

namespace OS.Core;

public class Scheduler
{
    private List<Processor> _processors;
    private IMemoryManager _memory;
    private int _timeSlice;
    private double _diskRate;
    private int _systemPeriod;
    private int _currentTime = 0;

    private List<Process> _allProcesses = new();

    private Queue<Process> _readyQueue = new();

    public List<SimulationEvent> Events { get; } = new();

    public List<Processor> Processors => _processors;
    public List<Process> AllProcesses => _allProcesses;

    public IMemoryManager TotalMemory => _memory;

    public Scheduler(int numProcessors, int ramSize, double diskRate, int slice, int sysPeriod, List<Process> allProcesses)
    {
        _processors = new List<Processor>(numProcessors);

        for (int i = 0; i < numProcessors; i++)
        {
            _processors.Add(new Processor(i));
        }

        _memory = new MemoryManager(ramSize, diskRate);
        _timeSlice = slice;
        _systemPeriod = sysPeriod;

        _diskRate = diskRate;

        _allProcesses = allProcesses;
    }

    public Scheduler(int numProcessors, int slice, int sysPeriod, List<Process> allProcesses, IMemoryManager memoryManager)
    {
        _processors = new List<Processor>(numProcessors);

        for (int i = 0; i < numProcessors; i++)
        {
            _processors.Add(new Processor(i));
        }

        _memory = memoryManager;
        _timeSlice = slice;
        _systemPeriod = sysPeriod;

        _allProcesses = allProcesses;
    }

    public void Ticks()
    {
        // 1. Verificăm procesul de sistem (Prioritate maximă, apare periodic)
        if (_currentTime > 0 && _currentTime % _systemPeriod == 0)
        {
            HandleSystemProcess();
        }

        // 2. Arrival Logic: Procesele care sosesc la acest moment de timp
        for (int i = _allProcesses.Count - 1; i >= 0; i--)
        {
            var p = _allProcesses[i];
            if (p.ReleaseTime == _currentTime)
            {
                p.CurrentStatus = Status.Ready;
                _readyQueue.Enqueue(p);
                _allProcesses.RemoveAt(i);
            }
        }

        // 3. Execution & Round Robin Logic pe fiecare procesor
        foreach (var proc in _processors)
        {
            if (!proc.IsFree)
            {
                var p = proc.CurrentProcess!;

                // VERIFICARE: Procesul lucrează doar dacă s-a terminat transferul de memorie
                if (_currentTime >= p.ReadyAtTick)
                {
                    p.RemainingTimeInActivity--;
                    proc.TimeSpentInSlice++;

                    // Logăm pasul de execuție pentru interfața grafică
                    Events.Add(new SimulationEvent(_currentTime, p.Id, proc.Id, "EXECUTING", 1));
                }
                else
                {
                    // Procesul este pe CPU, dar încă se încarcă din Disk în RAM
                    Events.Add(new SimulationEvent(_currentTime, p.Id, proc.Id, "WAITING_FOR_MEMORY", 1));
                }

                // Verificăm dacă trebuie să elibereze procesorul (Preempțiune sau Finalizare)
                bool finishedWork = p.RemainingTimeInActivity <= 0;
                bool timeSliceExpired = proc.TimeSpentInSlice >= _timeSlice;

                if (finishedWork || timeSliceExpired)
                {
                    // Eliberăm procesorul
                    proc.CurrentProcess = null;
                    proc.TimeSpentInSlice = 0;

                    if (finishedWork)
                    {
                        if (p.Activities.Count > 0)
                        {
                            // Pregătim următoarea activitate (încă nu o scoatem din coadă,
                            // o va scoate Scheduler-ul când îl repune pe un CPU)
                            p.CurrentStatus = Status.Ready;
                            _readyQueue.Enqueue(p);
                            Events.Add(new SimulationEvent(_currentTime, p.Id, null, "SWITCH_ACTIVITY", 0));
                        }
                        else
                        {
                            p.CurrentStatus = Status.Finished;
                            Events.Add(new SimulationEvent(_currentTime, p.Id, null, "FINISHED", 0));
                        }
                    }
                    else if (timeSliceExpired)
                    {
                        // Round Robin: Procesul se întoarce în coada Ready
                        p.CurrentStatus = Status.Ready;
                        _readyQueue.Enqueue(p);
                        Events.Add(new SimulationEvent(_currentTime, p.Id, null, "PREEMPTED", 0));
                    }
                }
            }
        }

        // 4. Scheduling Logic: Încercăm să ocupăm procesoarele libere cu procese din ReadyQueue
        ScheduleReadyProcesses();

        // Avansăm timpul global al simulării
        _currentTime++;
    }

    private bool ExistFreeProcessor() => _processors.Exists(proc => proc.IsFree);

    private Processor FindBestProcessor(Process p)
    {
        // Încercăm Affinity: procesorul pe care a mai fost
        if (p.LastProcessorId != -1 && _processors[p.LastProcessorId].IsFree)
        {
            return _processors[p.LastProcessorId];
        }

        // Altfel, primul procesor liber
        return _processors.Find(proc => proc.IsFree);
    }

    private void ScheduleReadyProcesses()
    {
        while (_readyQueue.Count > 0 && ExistFreeProcessor())
        {
            Process p = _readyQueue.Dequeue();
            Processor targetProc = FindBestProcessor(p);

            if (targetProc != null)
            {
                // Aici se face magia: EnsureInRam trebuie să adauge p în listă
                int transferDelay = _memory.EnsureInRam(p);

                p.ReadyAtTick = _currentTime + transferDelay;
                targetProc.CurrentProcess = p;
                targetProc.TimeSpentInSlice = 0;
                p.CurrentStatus = Status.Running;
                p.LastProcessorId = targetProc.Id;

                if (p.RemainingTimeInActivity <= 0 && p.Activities.Count > 0)
                {
                    var next = p.Activities.Dequeue();
                    p.RemainingTimeInActivity = next.Duration;
                }

                // DEBUG: Vedem dacă procesul a intrat în RAM
                Console.WriteLine($"[DEBUG] PID {p.Id} a fost trimis în RAM. Delay: {transferDelay}");

                Events.Add(new SimulationEvent(_currentTime, p.Id, targetProc.Id, "SCHEDULED", 0));
            }
            else
            {
                _readyQueue.Enqueue(p);
                break;
            }
        }
    }

    private void HandleSystemProcess()
    {
        var freeProc = _processors.Find(proc => proc.IsFree);
        if (freeProc != null)
        {
            Events.Add(new SimulationEvent(_currentTime, null, freeProc.Id, "SYSTEM_CALL_HANDLING", 1));
        }
    }

    public bool IsFinished()
    {
        return _allProcesses.Count == 0 &&
               _readyQueue.Count == 0 &&
               _processors.All(p => p.IsFree);
    }

    public void Reset()
    {
        // 1. Golește cozile interne
        _readyQueue.Clear();
        _allProcesses.Clear();

        // 2. Resetează fiecare procesor
        foreach (var processor in _processors)
        {
            // Setăm procesul la null.
            // Automat, proprietatea IsFree va deveni 'true' dacă este calculată intern.
            processor.CurrentProcess = null;
        }

        // 3. Golește istoricul de evenimente
        Events.Clear();
    }
}
