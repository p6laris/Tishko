namespace Tishko.Data.Models;

public sealed class TaskItem
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }
    public long? DueAtUtcTicks { get; set; }
    public long CreatedAtUtcTicks { get; set; }
    public long UpdatedAtUtcTicks { get; set; }
    public long? CompletedAtUtcTicks { get; set; }

    public long Version { get; set; } // increment on each update
}

