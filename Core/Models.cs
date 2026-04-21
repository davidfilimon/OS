namespace OS.Core;

public record SimulationEvent(int Time, int? ProcessId, int? ProcessorId, string Action, int Duration);
