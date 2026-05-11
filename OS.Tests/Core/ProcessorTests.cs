using System;
using System.Collections.Generic;
using System.Text;

using OS.Core;
using Xunit;
using System.Collections.Generic;

namespace OS.Tests.Core;

public class ProcessorTests
{
    [Fact]
    public void Constructor_WithValidId_ShouldSetId()
    {
        var processor = new Processor(1);

        Assert.Equal(1, processor.Id);
    }

    [Fact]
    public void NewProcessor_ShouldBeFree()
    {
        var processor = new Processor(1);

        Assert.True(processor.IsFree);
        Assert.Null(processor.CurrentProcess);
    }

    [Fact]
    public void NewProcessor_ShouldHaveZeroTimeSpentInSlice()
    {
        var processor = new Processor(1);

        Assert.Equal(0, processor.TimeSpentInSlice);
    }

    [Fact]
    public void Processor_WithCurrentProcess_ShouldNotBeFree()
    {
        var processor = new Processor(1);

        var process = new Process(
            id: 10,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        processor.CurrentProcess = process;

        Assert.False(processor.IsFree);
        Assert.Same(process, processor.CurrentProcess);
    }

    [Fact]
    public void Processor_ShouldBecomeFree_WhenCurrentProcessIsSetToNull()
    {
        var processor = new Processor(1);

        var process = new Process(
            id: 10,
            memoryRequired: 20,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );

        processor.CurrentProcess = process;
        processor.CurrentProcess = null;

        Assert.True(processor.IsFree);
        Assert.Null(processor.CurrentProcess);
    }

    [Fact]
    public void Processor_ShouldAllowChangingTimeSpentInSlice()
    {
        var processor = new Processor(1);

        processor.TimeSpentInSlice = 5;

        Assert.Equal(5, processor.TimeSpentInSlice);
    }

    [Fact]
    public void Constructor_WithNegativeId_ShouldRevealBehavior()
    {
        var processor = new Processor(-1);

        Assert.Equal(-1, processor.Id);
    }
}