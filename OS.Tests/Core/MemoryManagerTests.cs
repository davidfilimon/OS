using System;
using System.Collections.Generic;
using System.Text;

using OS.Core;
using Xunit;
using System.Collections.Generic;

namespace OS.Tests.Core;

// 1. initializeaza corect memoria totala si rata de transfer
// 2. seteaza DiskTransferRate implicit la 0 cand nu este dat
// 3. calculeaza memoria libera cand RAM-ul este gol
// 4. calculeaza memoria libera cand exista procese in RAM
// 5. incarca un proces in RAM daca exista memorie disponibila
// 6. nu duplica un proces deja incarcat in RAM
// 7. elimina primul proces din RAM cand nu mai este loc
// 8. elimina procese pana exista destula memorie libera
// 9. returneaza timp de transfer 0 cand DiskTransferRate este 0
// 10. arata comportamentul cand procesul cere mai multa memorie decat RAM-ul total
// 11. arata comportamentul cand procesul are memorie negativa
// 12. arata comportamentul cand memoria totala este negativa

public class MemoryManagerTests
{
    [Fact]
    public void Constructor_WithTotalMemoryAndDiskTransferRate_ShouldSetProperties()
    {
        var memoryManager = new MemoryManager(
            totalMemory: 100,
            diskTransferRate: 0.5
        );

        Assert.Equal(100, memoryManager.TotalMemory);
        Assert.Equal(0.5, memoryManager.DiskTransferRate);
        Assert.Empty(memoryManager.RamProcesses);
    }

    [Fact]
    public void Constructor_WithOnlyTotalMemory_ShouldSetDiskTransferRateToDefaultZero()
    {
        var memoryManager = new MemoryManager(totalMemory: 100);

        Assert.Equal(100, memoryManager.TotalMemory);
        Assert.Equal(0, memoryManager.DiskTransferRate);
    }

    [Fact]
    public void GetFreeMemory_WhenNoProcessesInRam_ShouldReturnTotalMemory()
    {
        var memoryManager = new MemoryManager(100, 0.5);

        int freeMemory = memoryManager.GetFreeMemory();

        Assert.Equal(100, freeMemory);
    }

    [Fact]
    public void GetFreeMemory_WhenProcessesAreInRam_ShouldReturnRemainingMemory()
    {
        var memoryManager = new MemoryManager(100, 0.5);

        var p1 = CreateProcess(id: 1, memoryRequired: 20);
        var p2 = CreateProcess(id: 2, memoryRequired: 30);

        memoryManager.RamProcesses.Add(p1);
        memoryManager.RamProcesses.Add(p2);

        int freeMemory = memoryManager.GetFreeMemory();

        Assert.Equal(50, freeMemory);
    }

    [Fact]
    public void EnsureInRam_WhenProcessIsNotInRamAndMemoryIsAvailable_ShouldAddProcess()
    {
        var memoryManager = new MemoryManager(100, 0.5);
        var process = CreateProcess(id: 1, memoryRequired: 20);

        int transferTime = memoryManager.EnsureInRam(process);

        Assert.Contains(process, memoryManager.RamProcesses);
        Assert.Equal(10, transferTime);
        Assert.Equal(80, memoryManager.GetFreeMemory());
    }

    [Fact]
    public void EnsureInRam_WhenProcessAlreadyExistsInRam_ShouldReturnZeroAndNotDuplicate()
    {
        var memoryManager = new MemoryManager(100, 0.5);
        var process = CreateProcess(id: 1, memoryRequired: 20);

        memoryManager.EnsureInRam(process);
        int secondTransferTime = memoryManager.EnsureInRam(process);

        Assert.Equal(0, secondTransferTime);
        Assert.Single(memoryManager.RamProcesses);
        Assert.Contains(process, memoryManager.RamProcesses);
    }

    [Fact]
    public void EnsureInRam_WhenNotEnoughMemory_ShouldRemoveFirstProcessAndAddNewProcess()
    {
        var memoryManager = new MemoryManager(50, 0.5);

        var p1 = CreateProcess(id: 1, memoryRequired: 30);
        var p2 = CreateProcess(id: 2, memoryRequired: 30);

        memoryManager.EnsureInRam(p1);
        int transferTime = memoryManager.EnsureInRam(p2);

        Assert.DoesNotContain(p1, memoryManager.RamProcesses);
        Assert.Contains(p2, memoryManager.RamProcesses);
        Assert.Single(memoryManager.RamProcesses);

        // scoate p1: 30 * 0.5 = 15
        // adauga p2: 30 * 0.5 = 15
        // total = 30
        Assert.Equal(30, transferTime);
    }

    [Fact]
    public void EnsureInRam_WhenMultipleEvictionsAreNeeded_ShouldRemoveProcessesUntilEnoughMemoryExists()
    {
        var memoryManager = new MemoryManager(100, 0.5);

        var p1 = CreateProcess(id: 1, memoryRequired: 40);
        var p2 = CreateProcess(id: 2, memoryRequired: 40);
        var p3 = CreateProcess(id: 3, memoryRequired: 60);

        memoryManager.EnsureInRam(p1);
        memoryManager.EnsureInRam(p2);

        int transferTime = memoryManager.EnsureInRam(p3);

        Assert.DoesNotContain(p1, memoryManager.RamProcesses);
        Assert.Contains(p2, memoryManager.RamProcesses);
        Assert.Contains(p3, memoryManager.RamProcesses);

        // free initial dupa p1+p2 = 20
        // p3 cere 60, deci se scoate p1
        // transfer p1 afara = 40 * 0.5 = 20
        // transfer p3 inauntru = 60 * 0.5 = 30
        // total = 50
        Assert.Equal(50, transferTime);
    }

    [Fact]
    public void EnsureInRam_WithZeroDiskTransferRate_ShouldReturnZeroTransferTime()
    {
        var memoryManager = new MemoryManager(100, 0);
        var process = CreateProcess(id: 1, memoryRequired: 20);

        int transferTime = memoryManager.EnsureInRam(process);

        Assert.Equal(0, transferTime);
        Assert.Contains(process, memoryManager.RamProcesses);
    }

    [Fact]
    public void EnsureInRam_WhenProcessRequiresMoreMemoryThanTotalMemory_ShouldRevealBehavior()
    {
        var memoryManager = new MemoryManager(100, 0.5);
        var process = CreateProcess(id: 1, memoryRequired: 150);

        int transferTime = memoryManager.EnsureInRam(process);

        Assert.Contains(process, memoryManager.RamProcesses);
        Assert.Equal(-50, memoryManager.GetFreeMemory());
        Assert.Equal(75, transferTime);
    }

    [Fact]
    public void EnsureInRam_WithNegativeProcessMemory_ShouldRevealBehavior()
    {
        var memoryManager = new MemoryManager(100, 0.5);
        var process = CreateProcess(id: 1, memoryRequired: -20);

        int transferTime = memoryManager.EnsureInRam(process);

        Assert.Contains(process, memoryManager.RamProcesses);
        Assert.Equal(120, memoryManager.GetFreeMemory());
        Assert.Equal(-10, transferTime);
    }

    [Fact]
    public void Constructor_WithNegativeTotalMemory_ShouldRevealBehavior()
    {
        var memoryManager = new MemoryManager(-100, 0.5);

        Assert.Equal(-100, memoryManager.TotalMemory);
        Assert.Equal(-100, memoryManager.GetFreeMemory());
    }

    private static Process CreateProcess(int id, int memoryRequired)
    {
        return new Process(
            id: id,
            memoryRequired: memoryRequired,
            releaseTime: 0,
            activities: new Queue<Activity>()
        );
    }
}