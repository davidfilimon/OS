using System;
using System.Collections.Generic;
using System.Text;

using OS.Core;
using Xunit;

namespace OS.Tests.Core;

// 1. eveniment normal cu toate valorile setate
// 2. eveniment de sistem, unde ProcessId poate fi null
// 3. eveniment fara procesor specific, unde ProcessorId poate fi null
// 4. doua record-uri cu aceleasi valori sunt egale
// 5. doua record-uri cu valori diferite nu sunt egale
// 6. comportamentul la timp negativ
// 7. comportamentul la durata negativa
// 8. comportamentul la Action null

public class SimulationEventTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldSetProperties()
    {
        var simulationEvent = new SimulationEvent(
            Time: 10,
            ProcessId: 1,
            ProcessorId: 0,
            Action: "EXECUTING",
            Duration: 1
        );

        Assert.Equal(10, simulationEvent.Time);
        Assert.Equal(1, simulationEvent.ProcessId);
        Assert.Equal(0, simulationEvent.ProcessorId);
        Assert.Equal("EXECUTING", simulationEvent.Action);
        Assert.Equal(1, simulationEvent.Duration);
    }

    [Fact]
    public void Constructor_WithNullProcessId_ShouldAllowSystemEvent()
    {
        var simulationEvent = new SimulationEvent(
            Time: 5,
            ProcessId: null,
            ProcessorId: 0,
            Action: "SYSTEM_CALL_HANDLING",
            Duration: 1
        );

        Assert.Equal(5, simulationEvent.Time);
        Assert.Null(simulationEvent.ProcessId);
        Assert.Equal(0, simulationEvent.ProcessorId);
        Assert.Equal("SYSTEM_CALL_HANDLING", simulationEvent.Action);
        Assert.Equal(1, simulationEvent.Duration);
    }

    [Fact]
    public void Constructor_WithNullProcessorId_ShouldAllowProcessEventWithoutCpu()
    {
        var simulationEvent = new SimulationEvent(
            Time: 20,
            ProcessId: 2,
            ProcessorId: null,
            Action: "PREEMPTED",
            Duration: 0
        );

        Assert.Equal(20, simulationEvent.Time);
        Assert.Equal(2, simulationEvent.ProcessId);
        Assert.Null(simulationEvent.ProcessorId);
        Assert.Equal("PREEMPTED", simulationEvent.Action);
        Assert.Equal(0, simulationEvent.Duration);
    }

    [Fact]
    public void Record_WithSameValues_ShouldBeEqual()
    {
        var first = new SimulationEvent(10, 1, 0, "EXECUTING", 1);
        var second = new SimulationEvent(10, 1, 0, "EXECUTING", 1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Record_WithDifferentValues_ShouldNotBeEqual()
    {
        var first = new SimulationEvent(10, 1, 0, "EXECUTING", 1);
        var second = new SimulationEvent(11, 1, 0, "EXECUTING", 1);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Constructor_WithNegativeTime_ShouldRevealBehavior()
    {
        var simulationEvent = new SimulationEvent(
            Time: -1,
            ProcessId: 1,
            ProcessorId: 0,
            Action: "EXECUTING",
            Duration: 1
        );

        Assert.Equal(-1, simulationEvent.Time);
    }

    [Fact]
    public void Constructor_WithNegativeDuration_ShouldRevealBehavior()
    {
        var simulationEvent = new SimulationEvent(
            Time: 10,
            ProcessId: 1,
            ProcessorId: 0,
            Action: "EXECUTING",
            Duration: -5
        );

        Assert.Equal(-5, simulationEvent.Duration);
    }

    [Fact]
    public void Constructor_WithNullAction_ShouldRevealBehavior()
    {
        var simulationEvent = new SimulationEvent(
            Time: 10,
            ProcessId: 1,
            ProcessorId: 0,
            Action: null!,
            Duration: 1
        );

        Assert.Null(simulationEvent.Action);
    }
}