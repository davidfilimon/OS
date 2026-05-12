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
        // mock - MemoryManager
        var memoryMock = new Mock<IMemoryManager>();

        // scheduler cu 2 procesoare
        var scheduler = new Scheduler(
            numProcessors: 2,
            slice: 5,
            sysPeriod: 10,
            allProcesses: new List<Process>(),
            memoryManager: memoryMock.Object
        );

        // verificam ca schedulerul a creat 2 procesoare
        Assert.Equal(2, scheduler.Processors.Count);

        // verificam ca toate procesoarele sunt libere la inceput
        Assert.All(scheduler.Processors, processor => Assert.True(processor.IsFree));
    }

    [Fact]
    public void Tick_WhenProcessIsReleasedAtCurrentTime_ShouldScheduleProcessOnProcessor()
    {
        // mock - memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam ca procesul este incarcat instant in RAM, fara delay
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces care apare la timpul 0
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // cream scheduler cu un proces si un procesor
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // rulam primul tick, procesul trebuie planificat
        scheduler.Ticks();

        // verificam daca:
        // procesorul are un proces pe el
        Assert.NotNull(scheduler.Processors[0].CurrentProcess);

        // procesul de pe CPU este chiar procesul nostru
        Assert.Same(process, scheduler.Processors[0].CurrentProcess);

        // procesul este in starea Running
        Assert.Equal(Status.Running, process.CurrentStatus);

        // activitatea curenta are durata initiala 5
        Assert.Equal(5, process.RemainingTimeInActivity);

        // procesul a fost pus pe procesorul 0
        Assert.Equal(0, process.LastProcessorId);
    }

    [Fact]
    public void Tick_WhenProcessIsScheduled_ShouldCallEnsureInRam()
    {
        // mock - memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces care apare la timpul 0
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // cream scheduler
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // rulam tick-ul de planificare
        scheduler.Ticks();

        // verificam daca schedulerul a cerut incarcarea procesului in RAM exact o data
        memoryMock.Verify(m => m.EnsureInRam(process), Times.Once);
    }

    [Fact]
    public void Tick_WhenProcessIsScheduled_ShouldCreateScheduledEvent()
    {
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare fara delay
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // cream scheduler
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // rulam primul tick
        scheduler.Ticks();

        // verificam daca s-a creat un singur eveniment
        Assert.Single(scheduler.Events);

        // verificam daca evenimentul este de tip SCHEDULED
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
        // mock - memorie
        var memoryMock = new Mock<IMemoryManager>();

        // cream scheduler fara procese
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process>(),
            memoryManager: memoryMock.Object
        );

        // verificam ca simularea este terminata imediat
        Assert.True(scheduler.IsFinished());
    }

    [Fact]
    public void Reset_ShouldClearEventsAndFreeProcessors()
    {
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare fara delay
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // cream scheduler
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 10,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // rulam un tick ca sa apara evenimente si proces pe CPU
        scheduler.Ticks();

        // resetam schedulerul
        scheduler.Reset();

        // verificam daca evenimentele au fost sterse
        Assert.Empty(scheduler.Events);

        // verificam daca procesorul este liber
        Assert.True(scheduler.Processors[0].IsFree);

        // verificam daca lista de procese a fost golita
        Assert.Empty(scheduler.AllProcesses);
    }

    [Fact]
    public void Tick_WhenScheduledProcessRuns_ShouldCreateExecutingEvent()
    {
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare fara delay
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces cu durata 5
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // cream scheduler
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // tick 0: procesul este doar planificat
        scheduler.Ticks();

        // tick 1: procesul executa o unitate de timp
        scheduler.Ticks();

        // verificam ca s-a generat eveniment EXECUTING
        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.ProcessorId == 0 &&
            e.Action == "EXECUTING" &&
            e.Duration == 1
        );

        // durata initiala era 5, dupa o executie ramane 4
        Assert.Equal(4, process.RemainingTimeInActivity);
    }

    [Fact]
    public void Tick_WhenProcessFinishesActivityAndHasNoMoreActivities_ShouldMarkProcessAsFinished()
    {
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare fara delay
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces cu activitate de durata 1
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 1
        );

        // cream scheduler
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // tick 0: procesul este planificat
        scheduler.Ticks();

        // tick 1: procesul executa si termina activitatea
        scheduler.Ticks();

        // verificam ca procesul a ajuns in Finished
        Assert.Equal(Status.Finished, process.CurrentStatus);

        // verificam ca procesorul a fost eliberat
        Assert.True(scheduler.Processors[0].IsFree);

        // verificam ca s-a generat eveniment FINISHED
        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.Action == "FINISHED"
        );
    }

    [Fact]
    public void Tick_WhenTimeSliceExpires_ShouldPreemptProcess()
    {
        // mock - memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces lung, durata 10
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 10
        );

        // cream scheduler cu cuanta 2
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 2,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // tick 0: procesul este planificat
        scheduler.Ticks();

        // tick 1: procesul executa prima unitate din cuanta de timp
        scheduler.Ticks();

        // tick 2: procesul executa a doua unitate si expira cuanta
        scheduler.Ticks();

        // verificam ca s-a generat eveniment PREEMPTED
        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.Action == "PREEMPTED"
        );

        // procesul este pus inapoi imediat pe CPU, deci ramane Running
        Assert.Equal(Status.Running, process.CurrentStatus);

        // verificam ca procesorul are proces dupa replanificare
        Assert.NotNull(scheduler.Processors[0].CurrentProcess);
    }

    [Fact]
    public void Tick_WhenMemoryTransferHasDelay_ShouldCreateWaitingForMemoryEvent()
    {
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam ca incarcarea in RAM dureaza 3 tick-uri
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(3);

        // cream proces
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // cream scheduler
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // tick 0: procesul este planificat, dar ReadyAtTick devine 3
        scheduler.Ticks();

        // tick 1: procesul inca asteapta memoria
        scheduler.Ticks();

        // verificam ca s-a generat eveniment WAITING_FOR_MEMORY
        Assert.Contains(scheduler.Events, e =>
            e.ProcessId == 1 &&
            e.ProcessorId == 0 &&
            e.Action == "WAITING_FOR_MEMORY"
        );

        // timpul ramas nu scade, pentru ca procesul nu a executat
        Assert.Equal(5, process.RemainingTimeInActivity);
    }

    [Fact]
    public void Tick_WhenSystemPeriodIsReached_ShouldCreateSystemCallHandlingEvent()
    {
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // cream scheduler cu proces de sistem la fiecare 2 tick-uri
        var scheduler = new Scheduler(
            numProcessors: 1,
            slice: 10,
            sysPeriod: 2,
            allProcesses: new List<Process>(),
            memoryManager: memoryMock.Object
        );

        // tick 0
        scheduler.Ticks();

        // tick 1
        scheduler.Ticks();

        // tick 2 + lansare proces de sistem
        scheduler.Ticks();

        // verificam evenimentul SYSTEM_CALL_HANDLING
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
        // cream mock pentru memorie
        var memoryMock = new Mock<IMemoryManager>();

        // simulam incarcare fara delay
        memoryMock
            .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
            .Returns(0);

        // cream proces
        var process = CreateProcess(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activityDuration: 5
        );

        // simulam ca procesul a mai rulat pe procesorul 1
        process.LastProcessorId = 1;

        // cream scheduler cu 2 procesoare
        var scheduler = new Scheduler(
            numProcessors: 2,
            slice: 10,
            sysPeriod: 100,
            allProcesses: new List<Process> { process },
            memoryManager: memoryMock.Object
        );

        // rulam primul tick
        scheduler.Ticks();

        // verificam daca procesorul 0 a ramas liber
        Assert.Null(scheduler.Processors[0].CurrentProcess);

        // verificam daca procesul a fost pus pe procesorul 1
        Assert.Same(process, scheduler.Processors[1].CurrentProcess);

        // verificam daca ultimul procesor ramane 1
        Assert.Equal(1, process.LastProcessorId);
    }

    // helper pentru creare de procese cu activitati
    private static Process CreateProcess(int id, int memoryRequired, int releaseTime, int activityDuration)
    {
        // cream coada de activitati
        var activities = new Queue<Activity>();

        // adaugam o activitate de executie cu durata primita
        activities.Enqueue(new Activity
        {
            Type = ActivityType.Execution,
            Duration = activityDuration
        });

        // returnam procesul folosit in teste
        return new Process(
            id: id,
            memoryRequired: memoryRequired,
            releaseTime: releaseTime,
            activities: activities
        );
    }
}