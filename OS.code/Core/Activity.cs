namespace OS.Core;

public enum ActivityType
{
    Execution,
    SysCall,
}

public class Activity
{
    public ActivityType Type { get; set; }
    public int Duration { get; set; }
}
