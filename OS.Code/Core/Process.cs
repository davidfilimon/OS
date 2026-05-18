using System.Collections.Generic;
using System.Diagnostics;

namespace OS.Core;

public class Process
{
    public int Id { get; }
    public int MemoryRequired { get; }
    public int ReleaseTime { get; }
    public int LastProcessorId { get; set; } = -1;

    public Status CurrentStatus { get; set; } = Status.OnDisk;
    public Queue<Activity> Activities { get; set; } = new();

    public int RemainingTimeInActivity { get; set; }

    public int ReadyAtTick { get; set; } = 0;

    public Process(int id, int memoryRequired, int releaseTime, Queue<Activity> activities)
    {
        // PRECONDITIONS: validăm parametrii înainte de a construi obiectul
        Debug.Assert(id >= 0, $"[Process] ID-ul procesului trebuie să fie >= 0, primit: {id}");
        Debug.Assert(memoryRequired > 0, $"[Process] Memoria necesară trebuie să fie > 0, primit: {memoryRequired}");
        Debug.Assert(releaseTime >= 0, $"[Process] ReleaseTime nu poate fi negativ, primit: {releaseTime}");
        Debug.Assert(activities != null, "[Process] Lista de activități nu poate fi null");

        Id = id;
        MemoryRequired = memoryRequired;
        ReleaseTime = releaseTime;
        Activities = activities;

        // POSTCONDITION: obiectul a fost creat corect
        Debug.Assert(CurrentStatus == Status.OnDisk, "[Process] Starea inițială trebuie să fie OnDisk");
        Debug.Assert(ReadyAtTick == 0, "[Process] ReadyAtTick trebuie să fie 0 la inițializare");
        Debug.Assert(LastProcessorId == -1, "[Process] LastProcessorId trebuie să fie -1 la inițializare (niciun procesor asociat)");

        // INVARIANT de clasă: verificăm că obiectul este într-o stare validă
        CheckClassInvariant();
    }

  
    public void CheckClassInvariant()
    {
        Debug.Assert(Id >= 0, "[Process.Invariant] ID-ul procesului nu poate fi negativ");
        Debug.Assert(MemoryRequired > 0, "[Process.Invariant] Memoria necesară trebuie să fie > 0");
        Debug.Assert(ReleaseTime >= 0, "[Process.Invariant] ReleaseTime nu poate fi negativ");
        Debug.Assert(Activities != null, "[Process.Invariant] Coada de activități nu poate fi null");
        Debug.Assert(RemainingTimeInActivity >= 0, "[Process.Invariant] Timpul rămas în activitate nu poate fi negativ");
        Debug.Assert(ReadyAtTick >= 0, "[Process.Invariant] ReadyAtTick nu poate fi negativ");

        // Invariant de stare:
        // exact una dintre stările valide trebuie să fie activă
        bool isValidStatus = CurrentStatus == Status.OnDisk
                          || CurrentStatus == Status.Ready
                          || CurrentStatus == Status.Running
                          || CurrentStatus == Status.Blocked
                          || CurrentStatus == Status.Finished;
        Debug.Assert(isValidStatus, $"[Process.Invariant] Starea procesului este invalidă: {CurrentStatus}");

        // Accepting condition: un proces Finished nu mai are activități de executat
        if (CurrentStatus == Status.Finished)
        {
            Debug.Assert(Activities.Count == 0,
                "[Process.Invariant] Un proces cu starea Finished nu trebuie să mai aibă activități");
        }
    }
}

public enum Status
{
    OnDisk,
    Ready,
    Running,
    Blocked,
    Finished,
}