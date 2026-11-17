using GuardPost.Data.Models.Enums;

namespace GuardPost.Data.Models;

/// <summary>
/// An event registered at a site – alarm, intrusion attempt, damage, etc.
/// </summary>
public class Incident
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime OccurredOn { get; set; }

    public IncidentSeverity Severity { get; set; }

    public IncidentStatus Status { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedOn { get; set; }

    public int SiteId { get; set; }

    public virtual Site Site { get; set; } = null!;

    public int? ReportedByGuardId { get; set; }

    public virtual Guard? ReportedByGuard { get; set; }

    /// <summary>The application user who registered the incident (null for seeded/legacy records).</summary>
    public string? CreatedById { get; set; }

    public virtual ApplicationUser? CreatedBy { get; set; }

    public virtual ICollection<IncidentUpdate> Updates { get; set; } = new HashSet<IncidentUpdate>();
}
