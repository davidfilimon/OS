using System;
using System.Collections.Generic;
using System.Text;
using OS.Core;
using Xunit;

namespace OS.Tests.Core;

// 1. valoarea implicita pentru Type
// 2. valoarea implicita pentru Duration
// 3. setarea unei activitati de tip Execution
// 4. setarea unei activitati de tip SysCall
// 5. comportamentul la durata 0
// 6. comportamentul la durata negativa

public class ActivityTests
{
    [Fact]
    public void NewActivity_ShouldHaveDefaultValues()
    {
        var activity = new Activity();

        Assert.Equal(ActivityType.Execution, activity.Type);
        Assert.Equal(0, activity.Duration);
    }

    [Fact]
    public void Activity_ShouldAllowExecutionType()
    {
        var activity = new Activity
        {
            Type = ActivityType.Execution,
            Duration = 5
        };

        Assert.Equal(ActivityType.Execution, activity.Type);
        Assert.Equal(5, activity.Duration);
    }

    [Fact]
    public void Activity_ShouldAllowSysCallType()
    {
        var activity = new Activity
        {
            Type = ActivityType.SysCall,
            Duration = 3
        };

        Assert.Equal(ActivityType.SysCall, activity.Type);
        Assert.Equal(3, activity.Duration);
    }

    [Fact]
    public void Activity_ShouldAllowZeroDuration()
    {
        var activity = new Activity
        {
            Type = ActivityType.Execution,
            Duration = 0
        };

        Assert.Equal(0, activity.Duration);
    }

    [Fact]
    public void Activity_ShouldRevealBehavior_WhenDurationIsNegative()
    {
        var activity = new Activity
        {
            Type = ActivityType.Execution,
            Duration = -5
        };

        Assert.Equal(-5, activity.Duration);
    }
}