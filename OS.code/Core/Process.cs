using System.Collections.Generic;

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
        Id = id;
        MemoryRequired = memoryRequired;
        ReleaseTime = releaseTime;
        Activities = activities;
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
