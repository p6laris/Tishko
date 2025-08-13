namespace Tishko.Data.Models;

public sealed class Session
{
    public long Id { get; set; }
    public long? TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public long StartUtcTicks { get; set; }
    public long? EndUtcTicks { get; set; }
    public int DayKey { get; set; }

    public short Interruptions { get; set; }
}
