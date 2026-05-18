using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

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
        // PRECONDITIONS
        Debug.Assert(numProcessors > 0, $"[Scheduler] Numărul de procesoare trebuie să fie > 0, primit: {numProcessors}");
        Debug.Assert(ramSize > 0, $"[Scheduler] Dimensiunea RAM trebuie să fie > 0, primit: {ramSize}");
        Debug.Assert(diskRate > 0, $"[Scheduler] Rata de transfer disk trebuie să fie > 0, primit: {diskRate}");
        Debug.Assert(slice > 0, $"[Scheduler] Time slice trebuie să fie > 0, primit: {slice}");
        Debug.Assert(sysPeriod > 0, $"[Scheduler] Perioada sistemului trebuie să fie > 0, primit: {sysPeriod}");
        Debug.Assert(allProcesses != null, "[Scheduler] Lista de procese nu poate fi null");

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

        // POSTCONDITIONS
        Debug.Assert(_processors.Count == numProcessors,
            "[Scheduler] Numărul de procesoare create nu corespunde cu parametrul");
        Debug.Assert(_processors.All(p => p.IsFree),
            "[Scheduler] Toate procesoarele trebuie să fie libere la inițializare");
        Debug.Assert(_currentTime == 0, "[Scheduler] Timpul curent trebuie să fie 0 la inițializare");
        Debug.Assert(Events.Count == 0, "[Scheduler] Lista de evenimente trebuie să fie goală la inițializare");

        CheckClassInvariant();
    }

    public Scheduler(int numProcessors, int slice, int sysPeriod, List<Process> allProcesses, IMemoryManager memoryManager)
    {
        // PRECONDITIONS
        Debug.Assert(numProcessors > 0, $"[Scheduler] Numărul de procesoare trebuie să fie > 0, primit: {numProcessors}");
        Debug.Assert(slice > 0, $"[Scheduler] Time slice trebuie să fie > 0, primit: {slice}");
        Debug.Assert(sysPeriod > 0, $"[Scheduler] Perioada sistemului trebuie să fie > 0, primit: {sysPeriod}");
        Debug.Assert(allProcesses != null, "[Scheduler] Lista de procese nu poate fi null");
        Debug.Assert(memoryManager != null, "[Scheduler] MemoryManager-ul nu poate fi null");

        _processors = new List<Processor>(numProcessors);

        for (int i = 0; i < numProcessors; i++)
        {
            _processors.Add(new Processor(i));
        }

        _memory = memoryManager;
        _timeSlice = slice;
        _systemPeriod = sysPeriod;
        _allProcesses = allProcesses;

        // POSTCONDITIONS
        Debug.Assert(_processors.Count == numProcessors,
            "[Scheduler] Numărul de procesoare create nu corespunde cu parametrul");
        Debug.Assert(_processors.All(p => p.IsFree),
            "[Scheduler] Toate procesoarele trebuie să fie libere la inițializare");

        CheckClassInvariant();
    }

    public void Ticks()
    {
        // PRECONDITION: simularea nu trebuie să fie deja terminată
        Debug.Assert(!IsFinished(), "[Ticks] Ticks() a fost apelat pe o simulare deja terminată");

        CheckClassInvariant();

        int eventCountBefore = Events.Count;

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
                // FAULT SNIFFER: procesul care soseşte trebuie să fie pe disk
                Debug.Assert(p.CurrentStatus == Status.OnDisk,
                    $"[Ticks] Procesul {p.Id} care soseşte trebuie să aibă starea OnDisk, are: {p.CurrentStatus}");

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

                // INVARIANT: procesul de pe procesor trebuie să fie în starea Running
                Debug.Assert(p.CurrentStatus == Status.Running,
                    $"[Ticks] Procesul {p.Id} de pe procesorul {proc.Id} trebuie să fie Running, are: {p.CurrentStatus}");
                Debug.Assert(proc.TimeSpentInSlice >= 0,
                    "[Ticks] TimeSpentInSlice nu poate fi negativ");
                Debug.Assert(proc.TimeSpentInSlice <= _timeSlice,
                    $"[Ticks] TimeSpentInSlice ({proc.TimeSpentInSlice}) nu poate depăşi time slice-ul ({_timeSlice})");

                if (_currentTime >= p.ReadyAtTick)
                {
                    p.RemainingTimeInActivity--;
                    proc.TimeSpentInSlice++;

                    Events.Add(new SimulationEvent(_currentTime, p.Id, proc.Id, "EXECUTING", 1));
                }
                else
                {
                    Events.Add(new SimulationEvent(_currentTime, p.Id, proc.Id, "WAITING_FOR_MEMORY", 1));
                }

                bool finishedWork = p.RemainingTimeInActivity <= 0;
                bool timeSliceExpired = proc.TimeSpentInSlice >= _timeSlice;

                if (finishedWork || timeSliceExpired)
                {
                    proc.CurrentProcess = null;
                    proc.TimeSpentInSlice = 0;

                    // POSTCONDITION : procesorul trebuie să fie liber acum
                    Debug.Assert(proc.IsFree,
                        $"[Ticks] Procesorul {proc.Id} trebuie să fie liber după eliberare");

                    if (finishedWork)
                    {
                        if (p.Activities.Count > 0)
                        {
                            p.CurrentStatus = Status.Ready;
                            _readyQueue.Enqueue(p);
                            Events.Add(new SimulationEvent(_currentTime, p.Id, null, "SWITCH_ACTIVITY", 0));
                        }
                        else
                        {
                            p.CurrentStatus = Status.Finished;
                            Events.Add(new SimulationEvent(_currentTime, p.Id, null, "FINISHED", 0));

                            // RESULTING CONDITION: procesul finalizat nu mai are activități
                            Debug.Assert(p.Activities.Count == 0,
                                $"[Ticks] Procesul {p.Id} marcat Finished mai are activități în coadă");
                        }
                    }
                    else if (timeSliceExpired)
                    {
                        // RESULTING CONDITION: procesul preemptat trebuie să revină în Ready
                        p.CurrentStatus = Status.Ready;
                        _readyQueue.Enqueue(p);
                        Events.Add(new SimulationEvent(_currentTime, p.Id, null, "PREEMPTED", 0));

                        Debug.Assert(p.CurrentStatus == Status.Ready,
                            $"[Ticks] Procesul {p.Id} preemptat trebuie să fie Ready");
                    }
                }
            }
        }

        // 4. Scheduling Logic: Încercăm să ocupăm procesoarele libere cu procese din ReadyQueue
        ScheduleReadyProcesses();

        // POSTCONDITION : timpul avansează monoton
        int previousTime = _currentTime;
        _currentTime++;
        Debug.Assert(_currentTime == previousTime + 1,
            "[Ticks] Timpul curent trebuie să crească cu exact 1 la fiecare Tick");

        CheckClassInvariant();
    }

    private bool ExistFreeProcessor() => _processors.Exists(proc => proc.IsFree);

    private Processor FindBestProcessor(Process p)
    {
        // PRECONDITION
        Debug.Assert(p != null, "[FindBestProcessor] Procesul nu poate fi null");
        Debug.Assert(ExistFreeProcessor(), "[FindBestProcessor] Trebuie să existe cel puțin un procesor liber");

        Processor result;

        // procesorul pe care a mai fost
        if (p.LastProcessorId != -1 && _processors[p.LastProcessorId].IsFree)
        {
            result = _processors[p.LastProcessorId];
        }
        else
        {
            result = _processors.Find(proc => proc.IsFree);
        }

        // POSTCONDITION: procesorul returnat trebuie să fie liber (sau null dacă nu există)
        Debug.Assert(result == null || result.IsFree,
            "[FindBestProcessor] Procesorul returnat trebuie să fie liber");

        return result;
    }

    private void ScheduleReadyProcesses()
    {
        // PRECONDITION: coada și procesoarele există
        Debug.Assert(_readyQueue != null, "[ScheduleReadyProcesses] ReadyQueue nu poate fi null");
        Debug.Assert(_processors != null, "[ScheduleReadyProcesses] Lista de procesoare nu poate fi null");

        while (_readyQueue.Count > 0 && ExistFreeProcessor())
        {
            // INVARIANT de buclă: numărul de procese din coadă + procesoare ocupate se conservă
            int freeBeforeSchedule = _processors.Count(pr => pr.IsFree);
            Debug.Assert(freeBeforeSchedule > 0, "[ScheduleReadyProcesses] Trebuie să existe un procesor liber în buclă");

            Process p = _readyQueue.Dequeue();

            // FAULT SNIFFER: procesul luat din coadă trebuie să fie în starea Ready
            Debug.Assert(p.CurrentStatus == Status.Ready,
                $"[ScheduleReadyProcesses] Procesul {p.Id} scos din ReadyQueue trebuie să fie Ready, are: {p.CurrentStatus}");

            Processor targetProc = FindBestProcessor(p);

            if (targetProc != null)
            {
                int transferDelay = _memory.EnsureInRam(p);

                // FAULT SNIFFER: delay-ul de transfer nu poate fi negativ
                Debug.Assert(transferDelay >= 0,
                    $"[ScheduleReadyProcesses] transferDelay nu poate fi negativ, primit: {transferDelay}");

                p.ReadyAtTick = _currentTime + transferDelay;
                targetProc.CurrentProcess = p;
                targetProc.TimeSpentInSlice = 0;
                p.CurrentStatus = Status.Running;
                p.LastProcessorId = targetProc.Id;

                if (p.RemainingTimeInActivity <= 0 && p.Activities.Count > 0)
                {
                    var next = p.Activities.Dequeue();
                    p.RemainingTimeInActivity = next.Duration;

                    // POSTCONDITION: durata activității setate trebuie să fie pozitivă
                    Debug.Assert(p.RemainingTimeInActivity > 0,
                        $"[ScheduleReadyProcesses] RemainingTimeInActivity trebuie să fie > 0 după setare, primit: {p.RemainingTimeInActivity}");
                }

                // POSTCONDITION: procesorul nu mai este liber după scheduling
                Debug.Assert(!targetProc.IsFree,
                    $"[ScheduleReadyProcesses] Procesorul {targetProc.Id} trebuie să fie ocupat după scheduling");
                Debug.Assert(p.CurrentStatus == Status.Running,
                    $"[ScheduleReadyProcesses] Procesul {p.Id} trebuie să fie Running după scheduling");

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
        // PRECONDITION: apelul sistemului se face la un moment multiplu de sysPeriod
        Debug.Assert(_currentTime % _systemPeriod == 0,
            $"[HandleSystemProcess] Procesul de sistem rulează la momente nepotrivite: time={_currentTime}, period={_systemPeriod}");

        var freeProc = _processors.Find(proc => proc.IsFree);
        if (freeProc != null)
        {
            Events.Add(new SimulationEvent(_currentTime, null, freeProc.Id, "SYSTEM_CALL_HANDLING", 1));
        }
    }

    public bool IsFinished()
    {
        bool result = _allProcesses.Count == 0 &&
                      _readyQueue.Count == 0 &&
                      _processors.All(p => p.IsFree);

        // POSTCONDITION: dacă IsFinished() e true, toate resursele trebuie să fie libere
        if (result)
        {
            Debug.Assert(_allProcesses.Count == 0,
                "[IsFinished] Mai există procese în allProcesses când IsFinished() == true");
            Debug.Assert(_readyQueue.Count == 0,
                "[IsFinished] Mai există procese în readyQueue când IsFinished() == true");
            Debug.Assert(_processors.All(p => p.IsFree),
                "[IsFinished] Există procesoare ocupate când IsFinished() == true");
        }

        return result;
    }

    public void Reset()
    {
        // 1. Golește cozile interne
        _readyQueue.Clear();
        _allProcesses.Clear();

        // 2. Resetează fiecare procesor
        foreach (var processor in _processors)
        {
            processor.CurrentProcess = null;
        }

        // 3. Golește istoricul de evenimente
        Events.Clear();

        // POSTCONDITION: după reset, totul trebuie să fie curat
        Debug.Assert(_readyQueue.Count == 0, "[Reset] ReadyQueue trebuie să fie goală după Reset");
        Debug.Assert(_allProcesses.Count == 0, "[Reset] AllProcesses trebuie să fie goală după Reset");
        Debug.Assert(_processors.All(p => p.IsFree), "[Reset] Toate procesoarele trebuie să fie libere după Reset");
        Debug.Assert(Events.Count == 0, "[Reset] Lista de evenimente trebuie să fie goală după Reset");

        CheckClassInvariant();
    }

    /// condiții valabile pe toată durata de viată a obiectului Scheduler.

    public void CheckClassInvariant()
    {
        Debug.Assert(_processors != null && _processors.Count > 0,
            "[Scheduler.Invariant] Lista de procesoare nu poate fi null sau goală");
        Debug.Assert(_memory != null,
            "[Scheduler.Invariant] MemoryManager-ul nu poate fi null");
        Debug.Assert(_timeSlice > 0,
            "[Scheduler.Invariant] Time slice trebuie să fie > 0");
        Debug.Assert(_systemPeriod > 0,
            "[Scheduler.Invariant] Perioada sistemului trebuie să fie > 0");
        Debug.Assert(_currentTime >= 0,
            "[Scheduler.Invariant] Timpul curent nu poate fi negativ");
        Debug.Assert(_readyQueue != null,
            "[Scheduler.Invariant] ReadyQueue nu poate fi null");
        Debug.Assert(Events != null,
            "[Scheduler.Invariant] Lista de Events nu poate fi null");

        // niciun proces nu poate fi simultan pe două procesoare
        var runningIds = _processors
            .Where(p => !p.IsFree)
            .Select(p => p.CurrentProcess!.Id)
            .ToList();
        Debug.Assert(runningIds.Distinct().Count() == runningIds.Count,
            "[Scheduler.Invariant] Același proces nu poate rula pe două procesoare simultan");
    }
}