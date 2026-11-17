namespace GuardPost.Data.Models;

/// <summary>One entry in the timeline of an incident (action taken, new information, etc.).</summary>
public class IncidentUpdate
{
    public int Id { get; set; }

    public int IncidentId { get; set; }

    public virtual Incident Incident { get; set; } = null!;

    public string Text { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string AuthorId { get; set; } = null!;

    public virtual ApplicationUser Author { get; set; } = null!;
}
