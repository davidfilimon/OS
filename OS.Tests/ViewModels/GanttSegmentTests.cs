using System;
using System.Collections.Generic;
using System.Text;

using OS.ViewModels;
using Xunit;

namespace OS.Tests.ViewModels;

public class GanttSegmentTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldSetProperties()
    {
        var segment = new GanttSegment(
            X: 20,
            Y: 40,
            Width: 20,
            Color: "#E74C3C",
            Label: "PID: 1"
        );

        Assert.Equal(20, segment.X);
        Assert.Equal(40, segment.Y);
        Assert.Equal(20, segment.Width);
        Assert.Equal("#E74C3C", segment.Color);
        Assert.Equal("PID: 1", segment.Label);
    }

    [Fact]
    public void Record_WithSameValues_ShouldBeEqual()
    {
        var first = new GanttSegment(20, 40, 20, "#E74C3C", "PID: 1");
        var second = new GanttSegment(20, 40, 20, "#E74C3C", "PID: 1");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Constructor_WithNegativeValues_ShouldRevealBehavior()
    {
        var segment = new GanttSegment(
            X: -20,
            Y: -40,
            Width: -20,
            Color: null!,
            Label: null!
        );

        Assert.Equal(-20, segment.X);
        Assert.Equal(-40, segment.Y);
        Assert.Equal(-20, segment.Width);
        Assert.Null(segment.Color);
        Assert.Null(segment.Label);
    }
}
