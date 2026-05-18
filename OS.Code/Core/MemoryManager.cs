using System.Collections.Generic;
using System.Diagnostics;

namespace OS.Core;

public class MemoryManager : IMemoryManager
{
    public int TotalMemory { get; }
    public double DiskTransferRate { get; }

    public List<Process> RamProcesses { get; } = new();

    public MemoryManager(int totalMemory, double diskTransferRate)
    {
        // PRECONDITIONS
        Debug.Assert(totalMemory > 0, $"[MemoryManager] Memoria totală trebuie să fie > 0, primit: {totalMemory}");
        Debug.Assert(diskTransferRate > 0, $"[MemoryManager] Rata de transfer trebuie să fie > 0, primit: {diskTransferRate}");

        TotalMemory = totalMemory;
        DiskTransferRate = diskTransferRate;

        // POSTCONDITION: managerul a fost creat cu RAM gol
        Debug.Assert(RamProcesses.Count == 0, "[MemoryManager] RAM trebuie să fie gol la inițializare");
        Debug.Assert(GetFreeMemory() == TotalMemory, "[MemoryManager] Toată memoria trebuie să fie liberă la inițializare");

        CheckClassInvariant();
    }

    public MemoryManager(int totalMemory)
    {
        // PRECONDITION
        Debug.Assert(totalMemory > 0, $"[MemoryManager] Memoria totală trebuie să fie > 0, primit: {totalMemory}");

        TotalMemory = totalMemory;

        // POSTCONDITION
        Debug.Assert(RamProcesses.Count == 0, "[MemoryManager] RAM trebuie să fie gol la inițializare");

        CheckClassInvariant();
    }

    public int GetFreeMemory()
    {
        CheckClassInvariant();

        int used = 0;
        foreach (var p in RamProcesses)
        {
            // INVARIANT de buclă: used creşte monoton şi rămâne în limitele memoriei totale
            Debug.Assert(used >= 0, "[GetFreeMemory] Memoria folosită calculată nu poate fi negativă");
            Debug.Assert(used <= TotalMemory, "[GetFreeMemory] Memoria folosită nu poate depăşi memoria totală");

            used += p.MemoryRequired;
        }

        int free = TotalMemory - used;

        // POSTCONDITION: memoria liberă nu poate fi negativă
        Debug.Assert(free >= 0, $"[GetFreeMemory] Memoria liberă nu poate fi negativă, calculat: {free}");
        Debug.Assert(free <= TotalMemory, "[GetFreeMemory] Memoria liberă nu poate depăşi memoria totală");

        CheckClassInvariant();
        return free;
    }

    public int EnsureInRam(Process p)
    {
        // PRECONDITIONS
        Debug.Assert(p != null, "[EnsureInRam] Procesul nu poate fi null");
        Debug.Assert(p.MemoryRequired > 0, $"[EnsureInRam] Procesul trebuie să necesite memorie > 0, primit: {p.MemoryRequired}");
        Debug.Assert(p.MemoryRequired <= TotalMemory,
            $"[EnsureInRam] Procesul (mem={p.MemoryRequired}) nu încape în RAM total ({TotalMemory})");

        CheckClassInvariant();

        // Dacă procesul e deja în RAM, nu facem nimic
        if (RamProcesses.Contains(p))
        {
            // POSTCONDITION pentru early return
            Debug.Assert(RamProcesses.Contains(p), "[EnsureInRam] Procesul trebuie să fie în RAM la ieşire");
            return 0;
        }

        // PRECONDITION suplimentară: memoria liberă nu trebuie să fie negativă înainte de operație
        Debug.Assert(GetFreeMemory() >= 0, "[EnsureInRam] Memoria liberă nu poate fi negativă înainte de operație");

        int transferTime = 0;
        int initialRamCount = RamProcesses.Count;

        // Scoatem procese (LRU = primul din listă) până avem loc
        while (GetFreeMemory() < p.MemoryRequired && RamProcesses.Count > 0)
        {
            var victim = RamProcesses[0];

            // FAULT SNIFFER: victima nu trebuie să fie acelaşi proces pe care vrem să-l adăugăm
            Debug.Assert(victim != p, "[EnsureInRam] Procesul victimă nu poate fi acelaşi cu procesul care se încarcă");
            Debug.Assert(victim.MemoryRequired > 0, "[EnsureInRam] Victima trebuie să aibă memorie > 0");

            RamProcesses.RemoveAt(0);
            transferTime += (int)(victim.MemoryRequired * DiskTransferRate);

            // INVARIANT de buclă: transferTime creşte monoton
            Debug.Assert(transferTime >= 0, "[EnsureInRam] transferTime nu poate fi negativ în timpul buclei");
        }

        // ADĂUGARE GARANTATĂ
        RamProcesses.Add(p);
        transferTime += (int)(p.MemoryRequired * DiskTransferRate);

        // POSTCONDITIONS
        Debug.Assert(RamProcesses.Contains(p),
            "[EnsureInRam] Procesul trebuie să fie în RAM după EnsureInRam");
        Debug.Assert(transferTime > 0,
            "[EnsureInRam] Timpul de transfer trebuie să fie > 0 pentru un proces nou adăugat");
        Debug.Assert(GetFreeMemory() >= 0,
            "[EnsureInRam] Memoria liberă nu poate deveni negativă după adăugare");
        Debug.Assert(RamProcesses.Count >= 1,
            "[EnsureInRam] RAM trebuie să conţină cel puţin procesul nou adăugat");

        CheckClassInvariant();
        return transferTime;
    }


    /// invariant de clasă: condiții valabile pe toată durata de viată a obiectului MemoryManager.
 
    public void CheckClassInvariant()
    {
        Debug.Assert(TotalMemory > 0, "[MemoryManager.Invariant] Memoria totală trebuie să fie > 0");
        Debug.Assert(RamProcesses != null, "[MemoryManager.Invariant] Lista de procese din RAM nu poate fi null");

        // suma memoriei proceselor din RAM nu poate depăşi memoria totală
        int usedMemory = 0;
        foreach (var proc in RamProcesses)
        {
            Debug.Assert(proc != null, "[MemoryManager.Invariant] Nu pot exista procese null în RAM");
            usedMemory += proc.MemoryRequired;
        }
        Debug.Assert(usedMemory <= TotalMemory,
            $"[MemoryManager.Invariant] Memoria folosită ({usedMemory}) depăşeşte memoria totală ({TotalMemory})");

        // Nu pot exista duplicate în RAM (acelaşi proces de două ori)
        var ids = new System.Collections.Generic.HashSet<int>();
        foreach (var proc in RamProcesses)
        {
            Debug.Assert(ids.Add(proc.Id),
                $"[MemoryManager.Invariant] Procesul cu ID={proc.Id} apare de mai multe ori în RAM");
        }
    }
}