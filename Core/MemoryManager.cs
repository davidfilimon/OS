using System.Collections.Generic;

namespace OS.Core;

public class MemoryManager
{
    public int TotalMemory { get; }
    public double DiskTransferRate { get; }

    public List<Process> RamProcesses { get; } = new();

    public MemoryManager(int totalMemory, double diskTransferRate)
    {
        TotalMemory = totalMemory;
        DiskTransferRate = diskTransferRate;
    }

    public MemoryManager(int totalMemory)
    {
        TotalMemory = totalMemory;
    }

    public int GetFreeMemory()
    {
        int used = 0;
        foreach (var p in RamProcesses)
            used += p.MemoryRequired;
        return TotalMemory - used;
    }

    public int EnsureInRam(Process p)
    {
        if (RamProcesses.Contains(p)) return 0;

        int transferTime = 0;
        // Scoatem procese până avem loc
        while (GetFreeMemory() < p.MemoryRequired && RamProcesses.Count > 0)
        {
            var victim = RamProcesses[0];
            RamProcesses.RemoveAt(0);
            transferTime += (int)(victim.MemoryRequired * DiskTransferRate);
        }

        // ADĂUGARE GARANTATĂ
        RamProcesses.Add(p);
        transferTime += (int)(p.MemoryRequired * DiskTransferRate);

        return transferTime;
    }
}
