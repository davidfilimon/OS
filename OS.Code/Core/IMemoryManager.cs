using System;
using System.Collections.Generic;
using System.Text;

namespace OS.Core;

public interface IMemoryManager
{
    int TotalMemory { get; }
    double DiskTransferRate { get; }
    List<Process> RamProcesses { get; }

    int GetFreeMemory();
    int EnsureInRam(Process p);
}