using System;
using System.Collections.Generic;
using System.Text;

using OS.ViewModels;
using Xunit;
using System.IO;
using System.Linq;

namespace OS.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeDemoAndSetDefaultStatus()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Equal("Simulator pregătit.", viewModel.StatusMessage);
        Assert.Equal(0, viewModel.CurrentTime);
        Assert.Empty(viewModel.DisplayEvents);
        Assert.Empty(viewModel.GanttSegments);
    }

    [Fact]
    public void LoadConfiguration_WithValidFile_ShouldLoadProcessesAndResetState()
    {
        string filePath = CreateTempInputFile(
            "1 100 5 10 100 " +
            "1 0 20 1 1 2"
        );

        var viewModel = new MainWindowViewModel();

        viewModel.LoadConfiguration(filePath);

        Assert.Equal("Configurație încărcată: 1 procese.", viewModel.StatusMessage);
        Assert.Equal(0, viewModel.CurrentTime);
        Assert.Empty(viewModel.DisplayEvents);

        File.Delete(filePath);
    }

    [Fact]
    public void LoadConfiguration_WithInvalidFile_ShouldSetErrorStatusMessage()
    {
        string filePath = CreateTempInputFile("invalid data");

        var viewModel = new MainWindowViewModel();

        viewModel.LoadConfiguration(filePath);

        Assert.StartsWith("Eroare la citire:", viewModel.StatusMessage);

        File.Delete(filePath);
    }

    [Fact]
    public void LoadConfiguration_WithTooFewNumbers_ShouldNotChangeStatusAfterDemoInitialization()
    {
        string filePath = CreateTempInputFile("1 100 5");

        var viewModel = new MainWindowViewModel();

        viewModel.LoadConfiguration(filePath);

        Assert.Equal("Simulator pregătit.", viewModel.StatusMessage);

        File.Delete(filePath);
    }

    [Fact]
    public void RunNextTick_AfterLoadingConfiguration_ShouldAdvanceCurrentTime()
    {
        string filePath = CreateTempInputFile(
            "1 100 5 10 100 " +
            "1 0 20 1 1 2"
        );

        var viewModel = new MainWindowViewModel();
        viewModel.LoadConfiguration(filePath);

        viewModel.RunNextTick();

        Assert.Equal(1, viewModel.CurrentTime);
        Assert.Equal(100, viewModel.MaxRam);
        Assert.Equal(20, viewModel.CurrentRamUsage);
        Assert.Equal(100, viewModel.CpuLoad);
        Assert.NotEmpty(viewModel.DisplayEvents);

        File.Delete(filePath);
    }

    [Fact]
    public void RunNextTick_WhenProcessExecutes_ShouldAddGanttSegment()
    {
        string filePath = CreateTempInputFile(
            "1 100 0 10 100 " +
            "1 0 20 1 1 2"
        );

        var viewModel = new MainWindowViewModel();
        viewModel.LoadConfiguration(filePath);

        viewModel.RunNextTick(); // time 0: SCHEDULED
        viewModel.RunNextTick(); // time 1: EXECUTING

        Assert.NotEmpty(viewModel.GanttSegments);

        var segment = viewModel.GanttSegments.First();

        Assert.Equal(20, segment.X);
        Assert.Equal(0, segment.Y);
        Assert.Equal(20, segment.Width);
        Assert.Equal("#E74C3C", segment.Color);
        Assert.Equal("PID: 1", segment.Label);

        File.Delete(filePath);
    }

    [Fact]
    public void ResetSimulation_ShouldClearDisplayedDataAndResetCurrentTime()
    {
        string filePath = CreateTempInputFile(
            "1 100 5 10 100 " +
            "1 0 20 1 1 2"
        );

        var viewModel = new MainWindowViewModel();
        viewModel.LoadConfiguration(filePath);

        viewModel.RunNextTick();
        viewModel.RunNextTick();

        viewModel.ResetSimulation();

        Assert.Equal(0, viewModel.CurrentTime);
        Assert.False(viewModel.IsRunning);
        Assert.Empty(viewModel.DisplayEvents);
        Assert.Empty(viewModel.GanttSegments);
        Assert.Equal("Simulare resetată. Gata pentru un nou start!", viewModel.StatusMessage);

        File.Delete(filePath);
    }

    [Fact]
    public void ExportLog_WhenEventsExist_ShouldCreateSimulationLogFile()
    {
        string filePath = CreateTempInputFile(
            "1 100 5 10 100 " +
            "1 0 20 1 1 2"
        );

        var viewModel = new MainWindowViewModel();
        viewModel.LoadConfiguration(filePath);

        viewModel.RunNextTick();
        viewModel.ExportLog();

        Assert.True(File.Exists("simulation_log.txt"));

        string content = File.ReadAllText("simulation_log.txt");
        Assert.Contains("Action: SCHEDULED", content);

        File.Delete(filePath);
        File.Delete("simulation_log.txt");
    }

    private static string CreateTempInputFile(string content)
    {
        string filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, content);
        return filePath;
    }
}