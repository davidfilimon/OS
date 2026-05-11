using Moq;
using OS.Core;
using Xunit;
using System.Collections.Generic;

namespace OS.Tests.Core;

public class SchedulerTests
{
    [Fact]
    public void Constructor_ShouldCreateCorrectNumberOfProcessors()
    {
        var memoryMock = new Mock<IMemoryManager>();

        var scheduler = new Scheduler(
            numProcessors: 2,
            slice: 5,
            sysPeriod: 10,
            allProcesses: new List<Process>(),
            memoryManager: memoryMock.Object
        );

        Assert.Equal(2, scheduler.Processors.Count);
        Assert.All(scheduler.Processors, processor => Assert.True(processor.IsFree));
    }

    [Fact]
    public void Tick_WhenProcessIsReleasedAtCurrentTime_ShouldScheduleProcessOnProcessor()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks();

        Assert.NotNull(scheduler.Processors[0].CurrentProcess);
        Assert.Same(process, scheduler.Processors[0].CurrentProcess);
        Assert.Equal(Status.Running, process.CurrentStatus);
        Assert.Equal(5, process.RemainingTimeInActivity);
        Assert.Equal(0, process.LastProcessorId);
    }

    [Fact]
    public void Tick_WhenProcessIsScheduled_ShouldCallEnsureInRam()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks();

        memoryMock.Verify(m => m.EnsureInRam(process), Times.Once);
    }

    [Fact]
    public void Tick_WhenProcessIsScheduled_ShouldCreateScheduledEvent()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks();

        Assert.Single(scheduler.Events);
        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.ProcessorId == 0 &&
            e.Action == "SCHEDULED" &&
            e.Duration == 0
        );
    }

    [Fact]
    public void Tick_WhenNoProcessesExist_ShouldFinishImmediately()
    {
        var memoryMock = new Mock<IMemoryManager>();

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process>(),
            memoryManager: memoryMock.Object
        );

        Assert.True(scheduler.IsFinished());
    }

    [Fact]
    public void Reset_ShouldClearEventsAndFreeProcessors()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks();

        scheduler.Reset();

        Assert.Empty(scheduler.Events);
        Assert.True(scheduler.Processors[0].IsFree);
        Assert.Empty(scheduler.AllProcesses);
    }

    [Fact]
    public void Tick_WhenScheduledProcessRuns_ShouldCreateExecutingEvent()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks(); // time 0: procesul este programat
        scheduler.Ticks(); // time 1: procesul executa

        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.ProcessorId == 0 &&
            e.Action == "EXECUTING" &&
            e.Duration == 1
        );

        Assert.Equal(4, process.RemainingTimeInActivity);
    }

    [Fact]
    public void Tick_WhenProcessFinishesActivityAndHasNoMoreActivities_ShouldMarkProcessAsFinished()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 1
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks(); // time 0: programare
        scheduler.Ticks(); // time 1: executa si termina

        Assert.Equal(Status.Finished, process.CurrentStatus);
        Assert.True(scheduler.Processors[0].IsFree);

        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.Action == "FINISHED"
        );
    }

    [Fact]
    public void Tick_WhenTimeSliceExpires_ShouldPreemptProcess()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 10
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 2,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks(); // time 0: programare
        scheduler.Ticks(); // time 1: executa, slice = 1
        scheduler.Ticks(); // time 2: executa, slice = 2, preemptie

        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.Action == "PREEMPTED"
        );

        // Dupa preemptie, codul il pune imediat inapoi pe CPU,
        // pentru ca procesorul este liber si ready queue contine procesul.
        Assert.Equal(Status.Running, process.CurrentStatus);
        Assert.NotNull(scheduler.Processors[0].CurrentProcess);
    }

    [Fact]
    public void Tick_WhenMemoryTransferHasDelay_ShouldCreateWaitingForMemoryEvent()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(3);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks(); // time 0: programare, ReadyAtTick = 3
        scheduler.Ticks(); // time 1: asteapta memoria

        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.ProcessorId == 0 &&
            e.Action == "WAITING_FOR_MEMORY"
        );

        Assert.Equal(5, process.RemainingTimeInActivity);
    }

    [Fact]
    public void Tick_WhenSystemPeriodIsReached_ShouldCreateSystemCallHandlingEvent()
    {
        var memoryMock = new Mock<IMemoryManager>();

        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 2,
            allProcesses: new List<Process>(),
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks(); // time 0
        scheduler.Ticks(); // time 1
        scheduler.Ticks(); // time 2: proces sistem

        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == null &&
            e.ProcessorId == 0 &&
            e.Action == "SYSTEM_CALL_HANDLING" &&
            e.Duration == 1
        );
    }

    [Fact]
    public void Tick_WhenProcessHasLastProcessorAndItIsFree_ShouldScheduleOnSameProcessor()
    {
        var memoryMock = new Mock<IMemoryManager>();
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        process.LastProcessorId = 1;

        var scheduler = new Scheduler(
            numProcessors: 2,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        scheduler.Ticks();

        Assert.Null(scheduler.Processors[0].CurrentProcess);
        Assert.Same(process, scheduler.Processors[1].CurrentProcess);
        Assert.Equal(1, process.LastProcessorId);
    }

    private static Process CreateProcess(int id, int memoryRequired, int releaseTime, int activityDuration)
    {
        var activities = new Queue<Activity>();
        activities.Enqueue(new Activity
        {
            Type = ActivityType.Execution,
            Duration = activityDuration
        });

        return new Process(
            id: id,
            memoryRequired: memoryRequired,
            releaseTime: releaseTime,
            activities: activities
        );
    }
}