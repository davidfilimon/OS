using System;
using System.Collections.Generic;
using System.Text;

using OS.Core;
using Xunit;
using System.Collections.Generic;

namespace OS.Tests.Core;

// 1. verifica daca constructorul seteaza corect proprietatile de baza:
// 2. verifica valorile implicite ale unui proces nou:
// 3. verifica daca statusul procesului poate fi schimbat:
// 4. verifica daca LastProcessorId poate fi modificat
// 5. verifica daca RemainingTimeInActivity poate fi modificat
// 6. verifica daca un proces poate fi creat cu lista de activitati goala
// 7. verifica comportamentul la memorie negativa
// 8. verifica comportamentul la releaseTime negativ
// 9. verifica comportamentul la lista de activitati null

public class ProcessTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldSetBasicProperties()
    {
        var activities = new Queue<Activity>();
        activities.Enqueue(new Activity
        {
            Type = ActivityType.Execution,
            Duration = 5
        });

        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: activities
        );

        Assert.Equal(1, process.Id);
        Assert.Equal(20, process.MemoryRequired);
        Assert.Equal(0, process.ReleaseTime);
        Assert.Same(activities, process.Activities);
    }

    [Fact]
    public void NewProcess_ShouldHaveDefaultStateValues()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        Assert.Equal(-1, process.LastProcessorId);
        Assert.Equal(Status.OnDisk, process.CurrentStatus);
        Assert.Equal(0, process.RemainingTimeInActivity);
        Assert.Equal(0, process.ReadyAtTick);
    }

    [Fact]
    public void Process_ShouldAllowChangingStatus()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        process.CurrentStatus = Status.Ready;
        Assert.Equal(Status.Ready, process.CurrentStatus);

        process.CurrentStatus = Status.Running;
        Assert.Equal(Status.Running, process.CurrentStatus);

        process.CurrentStatus = Status.Blocked;
        Assert.Equal(Status.Blocked, process.CurrentStatus);

        process.CurrentStatus = Status.Finished;
        Assert.Equal(Status.Finished, process.CurrentStatus);
    }

    [Fact]
    public void Process_ShouldAllowChangingLastProcessorId()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        process.LastProcessorId = 2;

        Assert.Equal(2, process.LastProcessorId);
    }

    [Fact]
    public void Process_ShouldAllowChangingRemainingTimeInActivity()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        process.RemainingTimeInActivity = 7;

        Assert.Equal(7, process.RemainingTimeInActivity);
    }

    [Fact]
    public void Constructor_WithEmptyActivities_ShouldCreateProcessWithEmptyQueue()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        Assert.NotNull(process.Activities);
        Assert.Empty(process.Activities);
    }

    [Fact]
    public void Constructor_WithNegativeMemory_ShouldRevealBehavior()
    {
        var process = new Process(
            id: 1,
            memoryRequired: -20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        Assert.Equal(-20, process.MemoryRequired);
    }

    [Fact]
    public void Constructor_WithNegativeReleaseTime_ShouldRevealBehavior()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: -5,
            activities: new Queue<Activity>()
        );

        Assert.Equal(-5, process.ReleaseTime);
    }

    [Fact]
    public void Constructor_WithNullActivities_ShouldRevealBehavior()
    {
        var process = new Process(
            id: 1,
            memoryRequired: 20,
            releaseTime: 0,
            activities: null!
        );

        Assert.Null(process.Activities);
    }
}