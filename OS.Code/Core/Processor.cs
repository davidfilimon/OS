namespace OS.Core;

public class Processor
{
    public int Id { get; }
    public Process? CurrentProcess { get; set; }

    public int TimeSpentInSlice { get; set; } = 0;

    public Processor(int id)
    {
        Id = id;
    }

    // O proprietate utilă pentru a verifica rapid dacă putem pune ceva pe el
    public bool IsFree => CurrentProcess == null;
}
