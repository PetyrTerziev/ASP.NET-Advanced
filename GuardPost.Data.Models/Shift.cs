namespace GuardPost.Data.Models;

/// <summary>
/// A period of duty of one guard at one site.
/// </summary>
public class Shift
{
    public int Id { get; set; }

    public int SiteId { get; set; }

    public virtual Site Site { get; set; } = null!;

    public int GuardId { get; set; }

    public virtual Guard Guard { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? Notes { get; set; }
}
